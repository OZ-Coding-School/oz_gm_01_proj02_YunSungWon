using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 괴한의 기본 행동 담당
/// -스폰 직후 ->연출(대사/멈춤) 시간
/// -연출 종료 후->플레이어 추격(NavMeshAgent 목적지 갱신)
/// -(일단 임시로) 플레이어 근접시 루프매니저의 리셋루프 호출 - 나중엔 몸싸움 연출 들어가고 리셋루프 호출할 예정
/// 
/// 괴한을 배우라고 생각 해당 스크립트는 배우의 대본
/// 
/// ++화장실 스텔스 기능 추가
/// -플레이어가 숨지 않았으면 바로 추격
/// -플레이어가 숨었다면 화장실 문을 확인 -> 일정시간 후 문 부숨연출
/// 
/// [구현목표]
/// -간단 상태 머신 구조로 책임 명확히 분리할것
/// -추후 애니메이션/공격/탐색 로직(화장실/라스트페이즈) 등 상태 확장을 염두할 것
/// 
/// +추가 화장실 체크시, 문상태에 따라 행동 분기
/// -문이 Open/Broken -바로 진입
/// -문이 Closed - 문을 열고 진입
/// -문이 Locked - 잠금해제 시도 후, 열고 진입
/// (추후 연출은 DoorStateChanged/ 여기 코루틴의 훅에 붙이면 될듯)
/// 
/// </summary>
public class EnemyControl : MonoBehaviour
{
    private enum EnemyState
    {
        None = 0,
        Entrance = 1,
        Chasing = 2,
        CheckingBathRoom = 3,
        BreakingBathRoom = 4
    }

    [Header("추격대상-플레이어")]
    [SerializeField] private Transform playertarget;

    [Header("플레이어 은신상태")]
    [SerializeField] private PlayerStealth playerStealth;

    [Header("화장실 문 컨트롤러 참조(문 상태에 따른 괴한 조작위함)")]
    [SerializeField] private BathRoom_DoorControl bathRoomDoor;

    [Header("네비메쉬 참조")]
    [SerializeField] private NavMeshAgent agent;

    [Header("추격 목적지 갱신 간격")]
    [SerializeField] private float chaseUpdateInterval = 0.1f;

    [Header("연출용 : 스폰직후 멈춰있는 시간")]
    [SerializeField] private float entranceDelay = 2.0f;

    [Header("연출용 : 대사 출력(임시)")]
    [SerializeField] private string entranceText = "내집에서 나가!!!";

    [Header("화장실 문 앞 체크 위치")]
    [SerializeField] private Transform bathRoomDoorPoint;

    [Header("화장실 문 앞 도착 판정 거리")]
    [SerializeField] private float arriveDistance = 1.0f;

    [Header("화장실 문 여는 시도 시간 설정")]
    [SerializeField] private float bathRoomTryOpenTime = 5.0f;

    [Header("화장실 문 잠금해제까지 걸리는 시간 설정")]
    [SerializeField] private float bathRoomUnlockTime = 10.0f;

    [Header("문이 열렸을때 연출 대기 시간")]
    [SerializeField] private float bathDoorOpenTime = 2.0f;

    [Header("임시 테스트용 : 근접 시 플레이어 킬 처리")]
    [SerializeField] private bool enableKillTest = true;

    [Header("임시 테스트용 : 킬 처리 판정 거리")]
    [SerializeField] private float killDistance = 1.0f;

    [Header("루프매니저 참조-리셋루프 호출용")]
    [SerializeField] private LoopManager loopManager;

    //괴한 상태 저장용
    private EnemyState enemyState;

    //추격위치 갱신용 모래시계
    private float chaseTimer;

    //추격 시작 플래그
    private bool hasStarted;

    private Coroutine bathRoomRoutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// 외부 초기화
    /// 스폰 직후 EnemyDirector 가 호출, 참조 주입용
    /// </summary>
    /// <param name="player"></param>
    /// <param name="manager"></param>
    public void Initialize(Transform player, LoopManager manager, PlayerStealth stealth, BathRoom_DoorControl doorControl)
    {
        playertarget = player;
        loopManager = manager;
        playerStealth = stealth;
        bathRoomDoor = doorControl;
    }

    /// <summary>
    /// 외부 트리거
    /// 스폰 이후 연출->추적 시퀀스 시작
    /// </summary>
    public void BeginEntranceChase()
    {
        if (hasStarted) return;
        hasStarted = true;
        StartCoroutine(EntranceChaseCo());
    }

    /// <summary>
    /// 침입후 추격 코루틴
    /// 연출전 이동 잠그고, 연출후에 이동풀고, 상태전환
    /// </summary>
    /// <returns></returns>
    private IEnumerator EntranceChaseCo()
    {
        enemyState = EnemyState.Entrance;

        //여기가 나중에 연출 들어갈 부분(대사/문 여는 사운드/시선 고정등)**
        agent.isStopped = true;

        Debug.Log("[EnemyControl] (임시)괴한 등장 대사 : " + entranceText);

        yield return new WaitForSeconds(entranceDelay);

        //이동 풀고 행동 결정
        agent.isStopped = false;

        DecideNextAction();
    }

    /// <summary>
    /// 연출 종료 직후에 플레이어의 상태에 따라 괴한 행동결정
    /// -숨은 상태면 ->화장실 문 확인 루틴
    /// -숨지 않은 상태면 -> 바로 추격
    /// </summary>
    private void DecideNextAction()
    {
        bool hidden = false;
        hidden = playerStealth.IsHidden;

        if (hidden) StartBathRoomCheck();
        else StartChasing();
    }

    /// <summary>
    /// 괴한 상태 추적으로 전환
    /// </summary>
    private void StartChasing()
    {
        enemyState = EnemyState.Chasing;
        chaseTimer = 0.0f;
        Debug.Log("[EnemyControl] 괴한 추적 시작");
    }

    /// <summary>
    /// 
    /// </summary>
    private void StartBathRoomCheck()
    {
        if (bathRoomDoorPoint == null)
        {
            Debug.Log("[EnemyControl] bathRoomDoorPoint가 null. 바로 추격함");
            StartChasing();
            return;
        }

        //화장실 체크 상태로 전환
        enemyState = EnemyState.CheckingBathRoom;
        Debug.Log("[EnemyControl] 플레이어가 안보임. 화장실 체크상태 돌입");
        agent.SetDestination(bathRoomDoorPoint.position);

        //이전 코루틴 확인 후 재실행
        if (bathRoomRoutine != null) StopCoroutine(bathRoomRoutine);
        bathRoomRoutine = StartCoroutine(BathRoomCheckCo());
    }

    /// <summary>
    /// 화장실 체크 로직 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator BathRoomCheckCo()
    {
        //화장실 문 앞 도착후 대기
        while (true)
        {
            float distance = Vector3.Distance(this.transform.position, bathRoomDoorPoint.position);
            if (distance <= arriveDistance) break;
            yield return null;
        }
        //문 부수는 상태로 전환
        enemyState = EnemyState.BreakingBathRoom;

        //문 상태 확인
        if (bathRoomDoor == null)
        {
            Debug.Log("[EnemyControl] bathRoomDoor가 null, 문상태 확인 불가->진입으로 처리하겠음");
            OnEnemyEnterBathRoom();
            yield break;
        }
        BathRoom_DoorControl.BathDoorState state = bathRoomDoor.CurState;
        Debug.Log("[EnemyControl] 현재 화장실 문 상태 = "+ state);

        //========================================상태별 분기처리 구간=============================================//
        if (state == BathRoom_DoorControl.BathDoorState.Open || state == BathRoom_DoorControl.BathDoorState.Broken)
        {
            //열려있는 상황->바로 진입
            Debug.Log("[EnemyControl] 문이 이미 열려있음 ->진입으로 처리하겠음");
            yield return new WaitForSeconds(0.5f);
            OnEnemyEnterBathRoom();
            yield break;
        }

        if (state == BathRoom_DoorControl.BathDoorState.Closed)
        {
            //잠긴상태는 아님, 열고 진입
            Debug.Log("[EnemyControl] 문이 닫혀있음 ->열고 진입으로 처리하겠음");
            yield return new WaitForSeconds(bathRoomTryOpenTime);

            bool opened = bathRoomDoor.EnemyTryOpenDoor();
            Debug.Log("[EnemyControl] 문 열기 시도 결과 = " +opened);

            yield return new WaitForSeconds(bathDoorOpenTime);
            OnEnemyEnterBathRoom();
            yield break;
        }

        if (state == BathRoom_DoorControl.BathDoorState.Locked)
        {
            //잠금상태 -> 잠금해제 시도 후, 문열고, 진입
            Debug.Log("[EnemyControl] 문이 잠겨있음 ->잠금해제 하고, 열고 진입으로 처리하겠음");
            yield return new WaitForSeconds(bathRoomTryOpenTime);

            Debug.Log("[EnemyControl] 잠금해제 시도중");
            bool unlocked = bathRoomDoor.EnemyTryUnlock();
            Debug.Log("[EnemyControl] 잠금해제 결과 = " + unlocked);

            yield return new WaitForSeconds(bathRoomUnlockTime);

            //잠금해제 후 문 열기
            bool opened = bathRoomDoor.EnemyTryOpenDoor();
            Debug.Log("[EnemyControl] 문 열기 시도 결과 = " + opened);

            yield return new WaitForSeconds(bathDoorOpenTime);
            OnEnemyEnterBathRoom();
            yield break;
        }
        //===================================================================================//
    }

    /// <summary>
    /// 괴한이 화장실에 진입했다고 가정처리(임시상태->은신해재+추격재개)
    /// -문열림 애니메이션/카메라 뭐 플레이어 발견 연출을 여기 훅에 붙이면 됨
    /// </summary>
    private void OnEnemyEnterBathRoom()
    {
        if (playerStealth != null) playerStealth.SetHidden(false);
        Debug.Log("[EnemyControl] 화장실 진입 완료. 플레이어 추격 재시작");
        StartChasing();
    }

    private void Update()
    {
        if (enemyState == EnemyState.Chasing) UpdateChasing();
    }

    /// <summary>
    /// 추격 상태 갱신
    /// 화장실 체크/ 플레이어 추격시 목적지 갱신+근접시 킬
    /// </summary>
    private void UpdateChasing()
    {
        //숨은 상태면 추격 중단, 화장실 체크로 전환
        if (playerStealth != null && playerStealth.IsHidden)
        {
            StartBathRoomCheck();
            return;
        }

        //목적지 갱신
        chaseTimer += Time.deltaTime;
        if (chaseTimer >= chaseUpdateInterval)
        {
            chaseTimer = 0.0f;
            agent.SetDestination(playertarget.position);
        }

        //플레이어 근접시 루프리셋(임시 테스트용이라 일단 근접하면 바로 리셋)
        if (enableKillTest && loopManager != null)
        {
            float distance = Vector3.Distance(this.transform.position, playertarget.position);
            if (distance <= killDistance) loopManager.ResetLoop("괴한에게 살해당함(임시)");
        }

    }

    /// <summary>
    /// [연출용] 스폰 후 침입시 출력할 대사 외부에서 주입
    /// </summary>
    /// <param name="text"></param>
    public void SetEntranceText(string text)
    {
        entranceText = text;
    }

    /// <summary>
    /// 씬에 배치된 화장실 문 체크 포인트를 런타임에 주입
    /// 프리팹으로 만들어놔서 씬 오브젝트 직접 참조 불가 Director에서 전달해줄 것
    /// </summary>
    /// <param name="point"></param>
    public void SetBathRoomDoorPoint(Transform point)
    {
        bathRoomDoorPoint = point;
    }
}
