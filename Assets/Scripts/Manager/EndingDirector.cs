using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 엔딩 진행 감독 (골격만 처리 day7)
/// -112 신고 성공시 엔딩 시작
/// -엔딩중에는 루프리셋 차단(LoopManager.SetResetBlocked)
/// -1인칭 전환할 것
/// -Exit_Trigger 가 엔딩진입상태에서 탈출 성공으로 처리할 것
/// 
/// [day7 설계]
/// -현재 단계에서는 연출없이 플레이 가능한 골격 구조 확립이 목적
/// -시네머신 전환은 추후에 연결할 것,(일단 컨트롤 토글만 확정되게)
/// 
/// </summary>
public class EndingDirector : MonoBehaviour
{
    //전역 접근용 인스턴스
    public static EndingDirector Instance { get; private set; }

    [Header("플레이어 트랜스폼 참조-라스트페이즈 주입용")]
    [SerializeField] private Transform playerTransform;

    [Header("루프 매니저 참조(엔딩중 리셋 차단용)")]
    [SerializeField] private LoopManager loopManager;

    [Header("컨트롤 토글(탑뷰 방식->FPS 방식으로)")]
    [SerializeField] private ControlToggle controlToggle;

    [Header("현관 Exit_Trigger 참조(라스트페이즈때 비활성화)")]
    [SerializeField] private Exit_Trigger exitTrigger;

    [Header("메인 현관문 컨트롤")]
    [SerializeField] private FrontDoorControl frontDoorControl;

    [Header("라스트페이즈 체크포인트")] //일단 신고 지점인 핸드폰 앞으로 설정
    [SerializeField] private Transform lastCheckPoint;

    [Header("화장실 문 롤백")]
    [SerializeField] private BathRoom_DoorControl bathRoomDoorControl;

    [Header("괴한스폰 디렉터(롤백시에 맵에 남아있는 괴한 제거)")]
    [SerializeField] private EnemyDirector enemyDirector;

    [Header("라스트 페이즈 괴한 프리팹")]
    [SerializeField] private GameObject lastPhaseEnemyPrefab;

    [Header("라스트 페이즈 괴한 스폰 위치")]
    [SerializeField] private Transform lastPhaseEnemySpawnPoint;

    [Header("신고 성공 후, 문 파괴까지 대기시간")]
    [SerializeField] private float breakDoorDelay = 3.0f;

    [Header("문 파괴 후, 괴한 스폰까지 추가 대기시간")]
    [SerializeField] private float spawnEnemyDelayAfterBreak = 1.0f;

    [Header("라스트 페이즈 돌입시 Vcam")]
    [SerializeField] private CinemachineVirtualCamera FPScam;

    //엔딩 진행중 여부
    public bool IsEnding { get; private set; }

    //체크 포인트에 저장할 화장실문 상태(체크포인트 도달하기전에 열려있거나 닫혀있는거 그대로 롤백하게)
    private BathRoom_DoorControl.BathDoorState checkPointBathDoorState;
    //현관문 상태
    private FrontDoorControl.FrontDoorState checkPointfrontDoorState;

    private Coroutine lastPhaseRoutine;
    private GameObject curLastPhaseEnemy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 엔딩시작(폰 신고 성공쪽에서 호출 예정)
    /// </summary>
    /// <param name="reason"></param>
    public void BeginEnding(string reason)
    {
        if (IsEnding) return;

        IsEnding = true;

        if (loopManager == null) loopManager = LoopManager.Instance;
        if (loopManager != null) loopManager.SetResetBlocked(true);

        //UI 열려있는 상태면 닫기
        if (UIManager.Instance != null) UIManager.Instance.CloseCurPanel("엔딩 시작 : UI 강제 닫힘");

        //1인칭 전환
        if (controlToggle != null) controlToggle.SetMode(ControlToggle.ControlMode.FPS);

        SaveEndingCheckPoint("BeginEnding 호출");

        //현관 ExitTrigger 비활성화
        if (exitTrigger != null) exitTrigger.enabled = false;

        //엔딩 시작 순간에는 현관문 잠금-바로 탈출 방지
        if (frontDoorControl != null)
        {
            frontDoorControl.ForceSetStateForRollBack(FrontDoorControl.FrontDoorState.Locked, "라스트페이즈 시작 : 현관문 고정");
        }

        //루프 괴한 남아있으면 정리
        if (enemyDirector != null)
        {
            enemyDirector.ForceDespawnEnemy("라스트 페이즈 시작 : 루프 괴한 제거");
        }

        RestartLastPhaseSequence("BeginEnding 호출");

        Debug.Log("[EndingDirector] 엔딩 시작 : " + reason );

        FPScam.Priority = 100;
    }

    /// <summary>
    /// 체크포인트 진입시 저장할 것들-일단 화장실 문,현관문 상태만 넣어둠
    /// </summary>
    /// <param name="reason"></param>
    private void SaveEndingCheckPoint(string reason)
    {
        if (bathRoomDoorControl != null)
        {
            checkPointBathDoorState = bathRoomDoorControl.CurState;
        }
        else
        {
            checkPointBathDoorState = BathRoom_DoorControl.BathDoorState.Closed;
        }

        if (frontDoorControl != null)
        {
            checkPointfrontDoorState = frontDoorControl.CurState;
        }
        else
        {
            checkPointfrontDoorState = FrontDoorControl.FrontDoorState.Closed;
        }

        Debug.Log("[EndingDirector] 체크포인트 저장 : " + reason);
    }

    private void RestartLastPhaseSequence(string reason)
    {
        //기존 시퀀스 중단
        if (lastPhaseRoutine != null)
        {
            StopCoroutine(lastPhaseRoutine);
            lastPhaseRoutine = null;
        }

        DespawnLastPhaseEnemy("시퀀스 재시작 : 기존 변이괴한 제거");

        //체크 포인트 상태로 현관문 복구 
        if (frontDoorControl != null)
        {
            frontDoorControl.ForceSetStateForRollBack(checkPointfrontDoorState, "시퀀스 재시작 : 현관문 복구");
        }

        lastPhaseRoutine = StartCoroutine(LastPhaseSequenceCo(reason));
    }

    private IEnumerator LastPhaseSequenceCo(string reason)
    {
        Debug.Log("[EndingDirector] 라스트페이즈 시퀀스 시작 :"+reason);

        if (breakDoorDelay > 0.0f)
        {
            yield return new WaitForSeconds(breakDoorDelay);
        }

        //현관문 파괴부분
        if (frontDoorControl != null)
        {
            frontDoorControl.EnemyForceBreak("라스트페이즈 : 현관문 파괴됨");
        }

        if (spawnEnemyDelayAfterBreak > 0.0f)
        {
            yield return new WaitForSeconds(spawnEnemyDelayAfterBreak);
        }

        SpawnLastPhaseEnemy();

        lastPhaseRoutine = null;
    }

    /// <summary>
    /// 변이괴한 스폰
    /// </summary>
    private void SpawnLastPhaseEnemy()
    {
        if (lastPhaseEnemyPrefab == null || lastPhaseEnemySpawnPoint == null)
        {
            Debug.Log("[EndingDirector] lastPhaseEnemyPrefab / spawnPoint 둘중 하나 null");
            return;
        }

        if (curLastPhaseEnemy != null) return;

        curLastPhaseEnemy = Instantiate(lastPhaseEnemyPrefab, lastPhaseEnemySpawnPoint.position, lastPhaseEnemySpawnPoint.rotation);

        LastPhaseEnemyControl control = curLastPhaseEnemy.GetComponent<LastPhaseEnemyControl>();

        if (control != null)
        {
            control.Initialize(playerTransform, this);
        }

        Debug.Log("[EndingDirector] 라스트 페이즈 괴한 스폰 완료");
    }

    /// <summary>
    /// 변이괴한 디스폰
    /// </summary>
    /// <param name="reason"></param>
    private void DespawnLastPhaseEnemy(string reason)
    {
        if (curLastPhaseEnemy == null) return;
        Destroy(curLastPhaseEnemy);
        curLastPhaseEnemy = null;

        Debug.Log("[EndingDirector] 라스트 페이즈 괴한 제거됨 : " + reason);
    }

    /// <summary>
    /// 라스트 페이즈 중 괴한에게 잡혔을때 호출될 롤백
    /// </summary>
    /// <param name="reason"></param>
    public void RollbackTocheckPoint(string reason)
    {
        if (!IsEnding) return;

        Debug.Log("[EndingDirector] 엔딩 롤백 시작 : " + reason);

        //남아있는 괴한 제거
        if (enemyDirector != null)
        {
            enemyDirector.ForceDespawnEnemy("엔딩 롤백 : 괴한제거됨");
        }

        //화장실 문상태 복구
        if (bathRoomDoorControl != null)
        {
            bathRoomDoorControl.ForceSetStateForRollBack(checkPointBathDoorState, "엔딩 롤백 : 화장실 문 상태 복구됨");
        }

        //체크포인트로 플레이어 이동
        if (lastCheckPoint != null)
        {
            TeleportPlayerToCheckPoint(lastCheckPoint);
        }

        //시점 FPS로 유지
        if (controlToggle != null) controlToggle.SetMode(ControlToggle.ControlMode.FPS);

        //UI 닫힌상태 유지
        if (UIManager.Instance != null) UIManager.Instance.CloseCurPanel("엔딩 롤백 : UI닫힘 상태로 유지");

        //시퀀스 재시작(현관문 파괴/변이괴한 재등장)
        RestartLastPhaseSequence("라스트페이즈 롤백");

        Debug.Log("[EndingDirector] 라스트 페이즈 진입 시점으로 롤백 완료");
    }

    private void TeleportPlayerToCheckPoint(Transform point)
    {
        if (point == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player"); //캐싱하는게 나을거 같긴한데..

        CharacterController controller = player.GetComponent<CharacterController>();
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();

        //컨트롤러 네비메쉬 둘다 처리
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.Warp(point.position);
            player.transform.rotation = point.rotation;
            agent.isStopped = false;
            return;
        }

        if (controller != null && controller.enabled)
        {
            controller.enabled = false;
            player.transform.position = point.position;
            player.transform.rotation = point.rotation;
            controller.enabled = true;
            return;
        }

        player.transform.position = point.position;
        player.transform.rotation = point.rotation;
    }

    /// <summary>
    /// 엔딩중 플레이어가 Exit_Trigger에 도달시 호출
    /// </summary>
    /// <param name="reason"></param>
    public void OnPlayerReachedExit(string reason)
    {
        if (!IsEnding) return;

        Debug.Log("[EndingDirector] 탈출 성공 : " + reason );

        EndEnding("루프 탈출 성공");
    }

    /// <summary>
    /// 엔딩 종료(성공/실패 공용)
    /// </summary>
    /// <param name="reason"></param>
    private void EndEnding(string reason)
    {
        Debug.Log("[EndingDirector] 엔딩 종료 : " + reason);

        if (loopManager == null) loopManager = LoopManager.Instance;
        if (loopManager != null) loopManager.SetResetBlocked(false);

        IsEnding = false;
    }

    //혹시 루프엔딩 시점에 다시 탑뷰로 복귀해야할 상황이 있을 수도 있으니까,
    //여기서 추가 하거나, 아니면 다음 씬으로 넘어가게
    //일단 탈출 성공 시점에서 게임종료/연출 로 이어지게 구현먼저->추후 수정
}
