using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;

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

    [Header("라스트 페이즈 돌입시 조명 연출")]
    [SerializeField] private LastPhaseLightDirector lastPhaseLightDirector;

    //=====================엔딩 타임라인 관련 필드=====================//
    [Header("엔딩 컷씬 타임라인")]
    [SerializeField] private PlayableDirector endingPlayableDirector;

    [Header("컷씬 중 조작 잠금용")]
    [SerializeField] private ClickMove clickMove;
    [SerializeField] private PlayerControl playerControl;
    [SerializeField] private NavMeshAgent playerNavMeshAgent;
    [SerializeField] private CharacterController playerCharacterController;

    [Header("컷씬 UI")]
    [SerializeField] private CanvasGroup blackOverlay;
    [SerializeField] private TextMeshProUGUI endText;

    //컷씬 플레이중 플래그
    private bool isEndingCutScenePlaying;

    //================================================================//

    //엔딩 진행중 여부
    public bool IsEnding { get; private set; }

    //체크 포인트에 저장할 화장실문 상태(체크포인트 도달하기전에 열려있거나 닫혀있는거 그대로 롤백하게)
    private BathRoom_DoorControl.BathDoorState checkPointBathDoorState;
    //현관문 상태
    private FrontDoorControl.FrontDoorState checkPointfrontDoorState;

    private Coroutine lastPhaseRoutine;
    private GameObject curLastPhaseEnemy;

    //타임라인 종료 콜백 연결-
    private void OnEnable()
    {
        if (endingPlayableDirector != null)
        {
            endingPlayableDirector.stopped += OnFinalEndingTimelineStopped;
        }
    }

    private void OnDisable()
    {
        if (endingPlayableDirector != null)
        {
            endingPlayableDirector.stopped -= OnFinalEndingTimelineStopped;
        }
    }

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
        if (loopManager != null)
        {
            loopManager.SetResetBlocked(true);
            loopManager.SetScenarioBlocked(true, "라스트 페이즈 진입 : 루프시나리오는 완전차단됨");
        }

        if (enemyDirector != null)
        {
            enemyDirector.SetLoopEnemyBlocked(true, "라스트 페이즈 진입 : 루프 괴한 완전차단");
        }

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

        //조명연출 라스트페이즈 라이트디렉터 호출
        lastPhaseLightDirector.ApplyLastPhaseLighting("라스트페이즈 진입 : 조명연출 시작");

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
        SoundManager.Instance.PlayBgmByName("LastPhase_BGM");
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
        SoundManager.Instance.StopBgm();
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
    /// +엔딩 타임라인이랑 엮어야 함
    /// </summary>
    /// <param name="reason"></param>
    public void OnPlayerReachedExit(string reason)
    {
        if (!IsEnding) return;
        if (isEndingCutScenePlaying) return;

        Debug.Log("[EndingDirector] 탈출 성공 : " + reason );

        BeginFinalEndingCutScene("루프 탈출 성공");
    }

    /// <summary>
    /// 최종 엔딩 컷씬 시작
    /// -플레이어 조작 잠그고,
    /// -타임라인 재생할 것
    /// </summary>
    /// <param name="reason"></param>
    private void BeginFinalEndingCutScene(string reason)
    {
        isEndingCutScenePlaying = true;

        Debug.Log("[EndingDirector] 최종 엔딩 컷씬 시작 : " + reason);

        //변이괴한 정리
        DespawnLastPhaseEnemy("최종컷씬 시작 : 변이괴한 제거");

        //플레이어 조작/이동 잠금
        SetCutSceneLock(true);

        //엔딩용 UI 초기화부분
        if (blackOverlay != null) blackOverlay.alpha = 0.0f;
        if (endText != null) endText.gameObject.SetActive(false);

        //타임라인 재생
        endingPlayableDirector.time = 0.0f;
        SoundManager.Instance.StopBgm();
        endingPlayableDirector.Play();
    }

    /// <summary>
    /// 컷씬 중 플레이어 입력,이동 모두 잠그는 용
    /// -라스트페이즈에서 FPS 입력 제한
    /// </summary>
    /// <param name="isLocked"></param>
    private void SetCutSceneLock(bool isLocked)
    {
        //입력값 관련 탑뷰,FPS 둘다 잠그고
        if (clickMove != null) clickMove.enabled = !isLocked;
        if (playerControl != null) playerControl.enabled = !isLocked;

        //네비메쉬 관련
        if (playerNavMeshAgent != null)
        {
            if (isLocked)
            {
                if (playerNavMeshAgent.enabled)
                {
                    playerNavMeshAgent.isStopped = true;
                    playerNavMeshAgent.ResetPath();
                    playerNavMeshAgent.enabled = false;
                }
            }
            else
            {
                //이게 굳이 필요할까.. 안쓸거 같긴 한데
                playerNavMeshAgent.enabled = true;
                playerNavMeshAgent.isStopped = false;
            }
        }

        //캐릭터 컨트롤러 관련
        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = !isLocked;
        }

        //커서도 숨김표시
        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// 타임라인 종료 콜백
    /// -여기서 화이트아웃,관련 텍스트 처리
    /// -
    /// </summary>
    /// <param name="director"></param>
    private void OnFinalEndingTimelineStopped(PlayableDirector director)
    {
        if (!isEndingCutScenePlaying) return;

        Debug.Log("[EndingDirector] 최종엔딩 컷씬 종료됨");

        //암전,텍스트 있으면 보이게 하고,
        if (blackOverlay != null) blackOverlay.alpha = 1.0f;
        if (endText != null)
        {
            endText.gameObject.SetActive(true);
            endText.text = "내집에서 나가";
        }

        //엔딩 시스템 리셋 차단 해제(다시 루프 시스템 되돌릴경우 사용)
        EndEnding("최종 컷씬 종료");

        //근데? 게임 컨셉상? 그냥 종료해버릴거야 컷씬 끝나면-추후수정 가능성있긴함
        Application.Quit();
        Debug.Log("[EndingDirector] 게임 강제종료됨");
    }

    /// <summary>
    /// 엔딩종료-다시 루프매니저 되살리고, 블락 해제(혹시나 게임 되돌릴때 사용)
    /// </summary>
    private void EndEnding(string reason)
    {
        Debug.Log("[EndingDirector] 엔딩 종료됨 : " + reason);

        if (loopManager == null) loopManager = LoopManager.Instance;
        if (loopManager != null) loopManager.SetResetBlocked(false);

        IsEnding = false;
    }
}
