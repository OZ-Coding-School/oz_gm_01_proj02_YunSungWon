using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
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
/// ++!!++괴한 AI 시야기반+탐색 루틴으로 완전히 변경
/// -어떤 상태에 있던, 플레이어가 시야안에 있다면, Chasing 으로 인터럽트
/// 
/// -플레이어가 안보인다면 진행되는 시퀀스-
/// 화장실->(잠긴상태면) 부엌열쇠수색->화장실로 복귀후 잠금해제->화장실 안에서 대기->거실 쇼파에서 대기->부엌 진입
/// -FSM 기반, 상태관리
/// -옵저버(여기선 EnemyVisionSensor의 이벤트 구독, 발생시 즉시 추격인터럽트)
/// -SRP원칙 준수 -> 감지는 센서쪽에서, 행동은 컨트롤쪽에서, 대본(주입)은 디렉터쪽에서
/// </summary>
public class EnemyControl : MonoBehaviour
{
    private enum EnemyState
    {
        None = 0,
        
        Entrance = 1,

        GoBathDoor = 2,
        KitchenSearchKey = 3,
        ReturnBathRoom = 4,
        UnlockAndEnterBath = 5,

        WaitInBath = 6,

        GoSofa = 7,
        WatchAtSofa = 8,

        GoKitchenEntry = 9,
        WatchAtKitchen = 10,

        Chasing = 100,
    }

    //===========필수 참조 부분 - 디렉터에서 주입할것들===========//
    [Header("추격대상-플레이어")]
    [SerializeField] private Transform playerTarget;
    [Header("루프매니저 참조-리셋루프 호출용")]
    [SerializeField] private LoopManager loopManager;
    [Header("화장실 문 컨트롤러 참조(문 상태에 따른 괴한 조작위함)")]
    [SerializeField] private BathRoom_DoorControl bathRoomDoor;
    [Header("괴한 시야 담당 센서")]
    [SerializeField] private EnemyVisionSensor visionSensor;
    //==============================================================//

    [Header("네비메쉬 참조")]
    [SerializeField] private NavMeshAgent agent;

    //===========괴한 탐색 포인트===========//
    [Header("화장실 앞")]
    [SerializeField] private Transform bathRoomDoorPoint;
    [Header("부엌 열쇠서랍 앞")]
    [SerializeField] private Transform kitchenKeySearchPoint;
    [Header("소파 위치")]
    [SerializeField] private Transform sofaWatchPoint;
    [Header("부엌 입구")]
    [SerializeField] private Transform kitchenEntryPoint;
    //==============================================================//

    [Header("시퀀스 도착 판정 거리")]
    [SerializeField] private float arriveDistance = 1.0f;

    [Header("연출용 : 스폰직후 멈춰있는 시간")]
    [SerializeField] private float entranceDelay = 2.0f;

    [Header("연출용 : 대사 출력(임시)")]
    [SerializeField] private string entranceText = "이 몸 등장.";

    [Header("부엌에서 열쇠 찾는 시간")]
    [SerializeField] private float keySearchDuration = 60.0f;

    [Header("화장실 안에서 대기 시간")]
    [SerializeField] private float waitInBathDuration = 30.0f;

    [Header("쇼파에서 대기 시간")]
    [SerializeField] private float sofaWatchDuration = 10.0f;

    [Header("부엌 진입후 대기 시간")]
    [SerializeField] private float kitchenWatchDuration = 10.0f;

    [Header("추격 목적지 갱신 간격")]
    [SerializeField] private float chaseUpdateInterval = 0.1f;

    [Header("추격중 시야잃었을때, 탐색으로 복귀시간")]
    [SerializeField] private float chaseLostReturnTime = 1.0f;

    [Header("임시 테스트용 : 근접 시 플레이어 킬 처리")]
    [SerializeField] private bool enableKillTest = true;

    [Header("임시 테스트용 : 킬 처리 판정 거리")]
    [SerializeField] private float killDistance = 1.0f;

    //괴한 상태 저장용
    private EnemyState state;

    //추격위치 갱신용 모래시계
    private float chaseTimer;

    //추격 시작 플래그
    private bool hasStarted;

    //현재 상태 코루틴(상태 전환시 인터럽트)
    private Coroutine stateRoutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        //비전센서 이벤트 구독
        visionSensor.PlayerSpotted += OnPlayerSpottedByVision;
    }

    private void OnDisable()
    {
        visionSensor.PlayerSpotted -= OnPlayerSpottedByVision;
    }

    /// <summary>
    /// 외부 초기화
    /// 스폰 직후 EnemyDirector 가 호출, 참조 주입용
    /// </summary>
    /// <param name="player"></param>
    /// <param name="manager"></param>
    public void Initialize(Transform player, LoopManager manager, BathRoom_DoorControl doorControl, EnemyVisionSensor sensor)
    {
        playerTarget = player;
        loopManager = manager;
        bathRoomDoor = doorControl;
        visionSensor = sensor;

        visionSensor.SetTarget(playerTarget);
    }

    /// <summary>
    /// 씬에 배치된 포인트를 EnemyDirector 가 호출, 참조 주입
    /// </summary>
    /// <param name="bathRoom"></param>
    /// <param name="kitchenKey"></param>
    /// <param name="sofa"></param>
    /// <param name="kitchenEntry"></param>
    public void SetSearchPoints(Transform bathRoom, Transform kitchenKey, Transform sofa, Transform kitchenEntry)
    {
        bathRoomDoorPoint = bathRoom;
        kitchenKeySearchPoint = kitchenKey;
        sofaWatchPoint = sofa;
        kitchenEntryPoint = kitchenEntry;
    }

    /// <summary>
    /// 외부 트리거
    /// 스폰 이후 시퀀스 시작
    /// </summary>
    public void BeginScenario()
    {
        if (hasStarted) return;
        hasStarted = true;
        ChangeState(EnemyState.Entrance, "시나리오 시작");
    }

    /// <summary>
    /// 시야 이벤트
    /// 시야 센서가 플레이어를 보기 시작한 순간 호출
    /// 어떤 상태든 바로 추격상태로 인터럽트
    /// </summary>
    private void OnPlayerSpottedByVision()
    {
        if (state == EnemyState.Chasing) return;

        Debug.Log("[EnemyControl] 플레이어 발견->> 바로 추격함");
        ChangeState(EnemyState.Chasing, "플레이어 시야안에 있음");
    }

    private void Update()
    {
        if (state == EnemyState.Chasing) UpdateChasing();
    }

    /// <summary>
    /// 상태 변경 공통처리
    /// -기존 상태 코루틴 중단(인터럽트)
    /// -새 상태 코루틴 시작
    /// </summary>
    /// <param name="newState"></param>
    /// <param name="reason"></param>
    private void ChangeState(EnemyState newState,string reason)
    {
        //코루틴 인터럽트
        StopStateRoutine();

        EnemyState oldState = state;
        state = newState;

        Debug.Log("[EnemyControl] state변경 : " + oldState + "->" + newState + "/변경이유 = " + reason);

        //상태별 루틴 시작
        if (state == EnemyState.Entrance) stateRoutine = StartCoroutine(StateEntranceCo());
        else if (state == EnemyState.GoBathDoor) stateRoutine = StartCoroutine(StateGoBathDoorCo());
        else if (state == EnemyState.KitchenSearchKey) stateRoutine = StartCoroutine(StateKitchenSearchKeyCo());
        else if (state == EnemyState.ReturnBathRoom) stateRoutine = StartCoroutine(StateReturnBathDoorCo());
        else if (state == EnemyState.UnlockAndEnterBath) stateRoutine = StartCoroutine(StateUnlockAndEnterBathCo());
        else if (state == EnemyState.WaitInBath) stateRoutine = StartCoroutine(StateWaitInBathCo());
        else if (state == EnemyState.GoSofa) stateRoutine = StartCoroutine(StateGoSofaCo());
        else if (state == EnemyState.WatchAtSofa) stateRoutine = StartCoroutine(StateWatchAtSofaCo());
        else if (state == EnemyState.GoKitchenEntry) stateRoutine = StartCoroutine(StateGoKitchenEntryCo());
        else if (state == EnemyState.WatchAtKitchen) stateRoutine = StartCoroutine(StateWatchAtKitchenCo());
        else if (state == EnemyState.Chasing) StartChasingInit(); //추격은 업데이트에서, 여기선 초기화만
    }

    private void StopStateRoutine()
    {
        if (stateRoutine != null)
        {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }
    }

    #region 각 상태 루틴 구현
    /// <summary>
    /// 스폰 직후 연출 후, 시야에 플레이어 없으면 화장실 탐색으로 진행
    /// </summary>
    private IEnumerator StateEntranceCo()
    {
        if (agent != null) agent.isStopped = true;

        Debug.Log("[EnemyControl] (임시) 등장 대사 : " + entranceText);

        yield return new WaitForSeconds(entranceDelay);

        if (agent != null) agent.isStopped = false;

        //연출 종료 시점에 플레이어가 보이면 추격으로 전환
        if (visionSensor.IsTargetVisible)
        {
            ChangeState(EnemyState.Chasing, "입장 시나리오->플레이어 발견");
            yield break;
        }

        //안보이면 다음 시나리오로
        ChangeState(EnemyState.GoBathDoor, "입장 시나리오->화장실 체크 시나리오");
    }

    /// <summary>
    /// 화장실 문 앞으로 이동 후, 문 상태 확인후 다음행동 결정
    /// -잠김이면 부엌 열쇠 탐색 시나리오로
    /// -잠기지 않았으면 문열고 화장실 진입
    /// </summary>
    private IEnumerator StateGoBathDoorCo()
    {
        if (bathRoomDoorPoint == null)
        {
            Debug.Log("[EnemyControl] bathRoomDoorPoint가 null. 화장실 탐색 시나리오 박살남");
            yield break;
        }
        
        yield return StartCoroutine(MoveToPointCo(bathRoomDoorPoint, "화장실 문 앞으로 이동"));

        //도착 중/후 플레이어가 시야안에 있으면 추격으로 전환
        if (visionSensor.IsTargetVisible)
        {
            ChangeState(EnemyState.Chasing, "화장실 탐색->플레이어 발견");
            yield break;
        }

        //문 상태 기반 분기점
        if (bathRoomDoor.CurState == BathRoom_DoorControl.BathDoorState.Locked)
        {
            Debug.Log("[EnemyControl] 화장실 문 잠김상태->부엌 열쇠탐색 이동");
            ChangeState(EnemyState.KitchenSearchKey, "화장실 잠김-> 열쇠탐색 시나리오");
            yield break;
        }

        //잠김 아니면 바로 진입시도
        ChangeState(EnemyState.UnlockAndEnterBath, "화장실 안잠김-> 화장실 진입");
    }

    /// <summary>
    /// 부엌 포인트로 이동해서 열쇠찾는 연출
    /// 시야에만 안걸리면 집 탐색 가능한 시나리오
    /// </summary>
    private IEnumerator StateKitchenSearchKeyCo()
    {
        if (kitchenKeySearchPoint == null)
        {
            Debug.Log("[EnemyControl] kitchenKeySearchPoint가 null. 열쇠 탐색 시나리오 박살남");
            yield break;
        }

        yield return StartCoroutine(MoveToPointCo(kitchenKeySearchPoint, "부엌 열쇠서랍 앞으로 이동"));

        //도착 중/후 플레이어가 시야안에 있으면 추격전환
        if (visionSensor.IsTargetVisible)
        {
            ChangeState(EnemyState.Chasing, "부엌에서 열쇠 탐색->플레이어 발견");
            yield break;
        }

        Debug.Log("[EnemyControl] (연출부분) 부엌에서 열쇠 찾는중-지연시간 = " + keySearchDuration + "s");
        yield return new WaitForSeconds(keySearchDuration);
        Debug.Log("[EnemyControl] (연출부분) 오 열쇠 찾았다!");

        ChangeState(EnemyState.ReturnBathRoom, "화장실 열쇠 찾음-> 화장실 복귀 시나리오");
    }

    /// <summary>
    /// 열쇠 찾은 뒤 화장실 문 앞으로 복귀 시나리오
    /// </summary>
    private IEnumerator StateReturnBathDoorCo()
    {
        if (bathRoomDoorPoint == null)
        {
            Debug.Log("[EnemyControl] bathRoomDoorPoint가 null. 화장실 복귀 시나리오 박살남");
            yield break;
        }

        yield return StartCoroutine(MoveToPointCo(bathRoomDoorPoint, "화장실 문 앞으로 복귀"));

        //도착 중/후 플레이어가 시야안에 있으면 추격전환
        if (visionSensor.IsTargetVisible)
        {
            ChangeState(EnemyState.Chasing, "화장실 복귀->플레이어 발견");
            yield break;
        }

        ChangeState(EnemyState.UnlockAndEnterBath, "화장실 복귀-> 화장실문 잠금해제 시나리오");
    }

    /// <summary>
    /// 화장실 문 잠금해제/열기/진입 시나리오
    /// -잠금이면 EnemyTryUnlock() 호출
    /// -단순 닫힌 상태면 EnemyTryOpenDoor() 호출
    /// -이후 화장실에서 잠깐 대기
    /// </summary>
    private IEnumerator StateUnlockAndEnterBathCo()
    {
        //잠금상태면 잠금해제 시도
        if (bathRoomDoor.CurState == BathRoom_DoorControl.BathDoorState.Locked)
        {
            Debug.Log("[EnemyControl] (연출부분) 잠금해제 시도중");
            bool unlocked = bathRoomDoor.EnemyTryUnlock();
            yield return new WaitForSeconds(1.0f); //잠금해제 시간 (임시-연출이랑 묶어야돼서-진짜 짧아야돼)

            //잠금해제 후 플레이어가 시야안에 있으면 추격전환
            if (visionSensor.IsTargetVisible)
            {
                ChangeState(EnemyState.Chasing, "잠금해제->플레이어 발견");
                yield break;
            }

            if (!unlocked)
                Debug.Log("[EnemyControl] 잠금해제 실패(문상태가 Locked 이 아니었을 가능성 있음). 진행");
        }

        //문 열기 시도 (closed 상태일때)
        Debug.Log("[EnemyControl] (연출부분) 문 여는 중");
        bool opened = bathRoomDoor.EnemyTryOpenDoor();
        yield return new WaitForSeconds(0.5f); //단순 문여는 시간 (임시-마찬가지로 연출이랑 묶어야함)

        //문 여는 후 플레이어가 시야안에 있으면 추격전환
        if (visionSensor.IsTargetVisible)
        {
            ChangeState(EnemyState.Chasing, "문 열기->플레이어 발견");
            yield break;
        }

        if (!opened)
        {
            //괴한이 문 열기 시도하는데 여기까지 와서도 열리지 않은 판정이다?
            //무조건 플레이어가 문고리 연타하고 있을 가능성 농후
            //일단 화장실 문 앞 탐색으로 되돌려 보내고, 이쪽에 문 부숴버리는 상태 추가예정
            Debug.Log("[EnemyControl] 문열기 실패(플레이어 상호작용 연타가능성 있음). 다시 문상태 확인으로");
            ChangeState(EnemyState.GoBathDoor, "문열기 실패->문 상태 다시 체크");
            yield break;
        }

        //화장실 진입 연출 들어갈건데, 일단 화장실 안쪽 포인트는 안넣어둔 상태.
        //진입했다고 가정하고 대기상태로 전환.
        Debug.Log("[EnemyControl] (연출부분) 화장실 진입(가정상황)-일단 문앞에서 대기");
        ChangeState(EnemyState.WaitInBath, "화장실 진입->화장실 대기 시나리오");
    }

    /// <summary>
    /// 화장실 내부에서 대기 시나리오(현재 임시로 문앞에서 대기)
    /// 이후 쇼파 시나리오로
    /// </summary>
    private IEnumerator StateWaitInBathCo()
    {
        if (agent != null) agent.isStopped = true;
        Debug.Log("[EnemyControl] 화장실에서 대기 시작-대기시간 = " + waitInBathDuration + "s");
        yield return new WaitForSeconds(waitInBathDuration);
        if (agent != null) agent.isStopped = false;

        //대기 후 플레이어가 시야안에 있으면 추격전환
        if (visionSensor.IsTargetVisible)
        {
            ChangeState(EnemyState.Chasing, "대기 후->플레이어 발견");
            yield break;
        }

        ChangeState(EnemyState.GoSofa, "화장실 대기 끝->소파 이동 시나리오");
    }

    /// <summary>
    /// 소파 이동 시나리오
    /// 이후 소파에서 대기 시나리오
    /// </summary>
    private IEnumerator StateGoSofaCo()
    {
        if (sofaWatchPoint == null)
        {
            Debug.Log("[EnemyControl] sofaWatchPoint가 null. 소파 이동 시나리오 박살남");
            yield break;
        }

        yield return StartCoroutine(MoveToPointCo(sofaWatchPoint, "소파로 이동"));

        //이동 중/후 플레이어가 시야안에 있으면 추격전환
        if (visionSensor.IsTargetVisible)
        {
            ChangeState(EnemyState.Chasing, "소파로 이동->플레이어 발견");
            yield break;
        }

        ChangeState(EnemyState.WatchAtSofa, "소파 도착->소파 대기 시나리오");
    }

    /// <summary>
    /// 소파 대기 시나리오
    /// 이후 부엌 이동 시나리오 
    /// </summary>
    private IEnumerator StateWatchAtSofaCo()
    {
        if (agent != null) agent.isStopped = true;
        Debug.Log("[EnemyControl] 소파에서 대기 시작-대기시간 = " + sofaWatchDuration + "s");
        yield return new WaitForSeconds(sofaWatchDuration);
        if (agent != null) agent.isStopped = false;

        //대기 후 플레이어가 시야안에 있으면 추격전환
        if (visionSensor.IsTargetVisible)
        {
            ChangeState(EnemyState.Chasing, "소파 대기->플레이어 발견");
            yield break;
        }

        ChangeState(EnemyState.GoKitchenEntry, "소파 대기 끝->부엌 이동 시나리오");
    }

    /// <summary>
    /// 부엌 이동 시나리오
    /// 이후 부엌 대기 시나리오
    /// </summary>
    private IEnumerator StateGoKitchenEntryCo()
    {
        if (kitchenEntryPoint == null)
        {
            Debug.Log("[EnemyControl] kitchenEntryPoint가 null. 부엌 이동 시나리오 박살남");
            yield break;
        }

        yield return StartCoroutine(MoveToPointCo(kitchenEntryPoint, "부엌으로 이동"));

        //이동 중/후 플레이어가 시야안에 있으면 추격전환
        if (visionSensor.IsTargetVisible)
        {
            ChangeState(EnemyState.Chasing, "부엌 이동->플레이어 발견");
            yield break;
        }

        ChangeState(EnemyState.WatchAtKitchen, "부엌 도착->부엌 대기 시나리오");
    }

    /// <summary>
    /// 부엌 대기 시나리오
    /// 혹시 모르니까, 일단 여기서도 플레이어 못찾으면,
    /// 다시 소파로 이동
    /// </summary>
    private IEnumerator StateWatchAtKitchenCo()
    {
        if (agent != null) agent.isStopped = true;
        Debug.Log("[EnemyControl] 부엌에서 대기 시작-대기시간 = " + kitchenWatchDuration + "s");
        yield return new WaitForSeconds(kitchenWatchDuration);
        if (agent != null) agent.isStopped = false;

        //대기 후 플레이어가 시야안에 있으면 추격전환
        if (visionSensor.IsTargetVisible)
        {
            ChangeState(EnemyState.Chasing, "부엌 대기->플레이어 발견");
            yield break;
        }

        ChangeState(EnemyState.GoSofa, "부엌 대기 끝->소파로 다시 복귀");
    }
    #endregion

    #region 추격 로직 관련
    private void StartChasingInit()
    {
        chaseTimer = 0.0f;

        if (agent != null) agent.isStopped = false;
    }

    /// <summary>
    /// 추격 상태 갱신
    /// 화장실 체크/ 플레이어 추격시 목적지 갱신+근접시 킬
    /// </summary>
    private void UpdateChasing()
    {
        //추격중 시야를 오래 잃으면 탐색으로 복귀(무한 추격 방지)
        bool visible = visionSensor.IsTargetVisible;
        if (!visible)
        {
            float lostTime = Time.time - visionSensor.LastSeenTime;
            if (lostTime >= chaseLostReturnTime)
            {
                Debug.Log("[EnemyControl] 추격중 시야 상실 " + lostTime.ToString("F2")+"s");
                ChangeState(EnemyState.GoSofa, "추격대상 잃음->전체가 보이는 소파로 이동");
                return;
            }
        }

        chaseTimer += Time.deltaTime;
        if (chaseTimer >= chaseUpdateInterval)
        {
            chaseTimer = 0.0f;
            agent.SetDestination(playerTarget.position);
        }

        //근접 시 임시 킬 판정(연출 들어가기전 임시)
        if (enableKillTest && loopManager != null)
        {
            float d = Vector3.Distance(this.transform.position, playerTarget.position);
            if (d <= killDistance)
            {
                loopManager.ResetLoop("괴한에게 살해당함(시야기반 탐색중-연출없어서 바로 킬(임시)");
            }
        }
    }
    #endregion

    #region 이동 공통 코루틴
    /// <summary>
    /// 특정 포인트로 이동하고 도착할때까지 대기
    /// </summary>
    /// <param name="point"></param>
    /// <param name="debugLabel"></param>
    private IEnumerator MoveToPointCo(Transform point, string debugLabel)
    {
        agent.isStopped = false;
        agent.SetDestination(point.position);

        Debug.Log("[EnemyControl] 이동 시작 : " + debugLabel);

        while (true)
        {
            //이동 중 플레이어가 보이면 즉시 추격 인터럽트
            if (visionSensor.IsTargetVisible)
            {
                ChangeState(EnemyState.Chasing, "이동중->플레이어 발견");
                yield break;
            }

            if (!agent.pathPending)
            {
                //목적지 대비 남은 거리가 잘 갱신되게, pathPending 끝난후에 체크
                if (agent.remainingDistance <= arriveDistance) break;
            }

            yield return null;
        }

        Debug.Log("[Enemycontrol] 이동 도착 : " + debugLabel);
    }
    #endregion

    /// <summary>
    /// [연출용] 스폰 후 침입시 출력할 대사 외부에서 주입
    /// </summary>
    /// <param name="text"></param>
    public void SetEntranceText(string text)
    {
        entranceText = text;
    }

    ///// <summary>
    ///// 씬에 배치된 화장실 문 체크 포인트를 런타임에 주입
    ///// 프리팹으로 만들어놔서 씬 오브젝트 직접 참조 불가 Director에서 전달해줄 것
    ///// </summary>
    ///// <param name="point"></param>
    //public void SetBathRoomDoorPoint(Transform point)
    //{
    //    bathRoomDoorPoint = point;
    //}
}
