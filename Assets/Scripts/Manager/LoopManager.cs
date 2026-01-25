using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;
using System.Runtime.CompilerServices;
using Cinemachine;

/// <summary>
/// 게임의 핵심 시스템 (타이머/침입/리셋) 관리
/// 1.루프 시작후 침입 타이밍 18초+-2초(16~20초)로 랜덤 설정
/// 2.시간이 지나면 침입 이벤트 발생-01/06(day1) 일단 로그만 띄우게 구현
/// 3.특정 조건(현관문 트리거 / 플레이어 사망)에서 루프 리셋 - 01/06(day1) 플레이어 사망은 일단 키 입력으로 테스트
/// 
/// 4.배터리 제거 상호작용시 1차 침입 실패, 비상키 침입(추가35~40초)으로 지연됨
/// 
/// [사용된 기술]
/// -상태관리관련 간단한 플래그 전환
/// -이벤트 연출 포인트
/// </summary>
[DefaultExecutionOrder(-1000)] //루프매니저를 가장 먼저 실행, 상호작용 오브젝트가 먼저 초기화되었던 문제
public class LoopManager : MonoBehaviour
{
    //ResetTable들이 각자의 OnEnable에서 등록할 수 있는 인스턴스 제공
    public static LoopManager Instance { get; private set; }
    //루프 리셋 대상 목록
    private readonly List<IResetTable> resetTables = new List<IResetTable>();

    //침입성공의 방식을 구분하기위한 enum
    public enum BreakInMethod
    {
        DoorLock = 0,
        EmergencyKey =1
    }

    //침입단계 명확하게 관리하기 위해서 상태값 enum
    public enum BreakInPhase
    {
        FirstAttempt = 0, //첫 침입
        WaitingEmergencyKey = 1, // 배터리 제거시 비상키로 침입
        Done = 2 //침입처리 완료시
    }

    //=====이벤트(옵저버 패턴) 훅=====//
    //루프가 시작됐음을 외부에 알릴 이벤트(스폰제거/초기화 등)
    public event Action<int> LoopStarted;

    //침입이 성공했음을 외부에 알릴 이벤트(괴한 스폰 트리거쪽)
    public event Action<BreakInMethod> BreakInSucceeded;

    //침입이 지연됐음을 외부에 알릴 이벤트(괴한 스폰 전 트리거쪽)
    public event Action<float> BreakInFailedByBattery;
    //==============================//

    [Header("플레이어 참조")]
    [SerializeField] private Transform playerTransform;

    [Header("플레이어 네비메쉬 참조")]
    [SerializeField] private NavMeshAgent playerAgent;

    [Header("루프 시작 위치")]
    [SerializeField] private Transform loopStartPoint;

    [Header("1차 침입 시간 설정(최소/최대)")]
    [SerializeField] private float breakInMinSeconds = 16.0f;
    [SerializeField] private float breakInMaxSeconds = 20.0f;

    [Header("2차 침입시간 설정(비상키 진입)")]
    [SerializeField] private float emergencyMinSeconds = 35.0f;
    [SerializeField] private float emergencyMaxSeconds = 40.0f;

    [Header("텔레포트 시 y오프셋 조정")]
    [SerializeField] private float spawnYOffset = 0.5f;

    [Header("루프리셋 중복 방지(쿨타임)")]
    [SerializeField] private float resetCoolTime = 0.5f;

    //v캠관련
    [Header("화장실 Vcam")]
    [SerializeField] private CinemachineVirtualCamera bathVCam;

    [Header("부엌 Vcam")]
    [SerializeField] private CinemachineVirtualCamera kitchenVcam;

    //도어락은 이제 리셋 테이블로 복구하게 만들거야-기존 도어락 필드 제거

    //=====런타임 상태값=====//
    private float elapsedSeconds; //현재 루프 진행 시간
    private float breakInSeconds; //1차침입 시점(루프 시작시 랜덤 결정)
    private float emergencyKeySeconds; //2차침입 시점(배터리 제거시에만 결정)
    
    private int loopCount; //루프횟수
    private bool isResetting; //리셋 중복 방지용 플래그

    private bool batteryRemoved; //배터리 제거 여부(매 루프때마다 초기화할것)
    private BreakInPhase breakInPhase; //침입 진행 단계 저장용

    //플레이어 컨트롤러 보관
    private CharacterController characterController;

    //++엔딩관련 추가-특수 상황에서 루프리셋 차단용
    private bool isResetBlocked;

    //라스트 페이즈 진입시 기존루프 괴한 시나리오 이벤트 원천차단 플래그
    private bool isScenarioBlocked;
    public bool IsScenarioBlocked { get { return isScenarioBlocked; } }

    //오프닝(게임시작전 연출)이후 게임진행 기점 플래그
    private bool isGameRunning;
    public bool IsGameRunning { get { return isGameRunning; } }

    //=====외부 접근용 프로퍼티=====//
    public float ElapsedSeconds { get { return elapsedSeconds; } }
    public float BreakInSeconds { get { return breakInSeconds; } }
    public float EmergencyKeySeconds { get { return emergencyKeySeconds; } }
    public int LoopCount { get { return loopCount; } }
    public bool BatteryRemoved { get { return batteryRemoved; } }
    public BreakInPhase CurBreakInPhase  { get { return breakInPhase; } }
    public bool IsResetBlocked { get { return isResetBlocked; } }

    private void Awake()
    {
        //====Instance세팅=====//일단 간단싱글톤으로,
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        //컨트롤러는 플레이어쪽의 컴포넌트 가져오고
        characterController = playerTransform.GetComponent<CharacterController>();
        //네비메쉬도 자동연결,
        playerAgent = playerTransform.GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        //오프닝연출 중일때 차단
        if (!isGameRunning) return;

        //라스트 페이즈에선 기존루프 시나리오 진행중단
        if (isScenarioBlocked) return;

        elapsedSeconds += Time.deltaTime;

        //침입 타임라인 처리(단계 기반)
        UpdateBreakInTimeLine();

        //사망 트리거 들어갈곳(일단 테스트용)
        if (Input.GetKeyDown(KeyCode.K))
        {
            ResetLoop("테스트 사망처리");
        }
    }

    #region 리셋테이블 관리
    //=================================================================리셋테이블 관리=====//

    /// <summary>
    /// [등록]-ResetTable 컴포넌트가 OnEnable 에서 호출
    /// </summary>
    /// <param name="resetTable"></param>
    public void RegisterResetTable(IResetTable resetTable)
    {
        if (resetTable == null) return;
        //중복 등록 방지
        if (resetTables.Contains(resetTable)) return;

        resetTables.Add(resetTable);
    }

    /// <summary>
    /// [해제]-ResetTable 컴포넌트가 OnDisable 에서 호출
    /// </summary>
    /// <param name="resetTable"></param>
    public void UnRegisterResetTable(IResetTable resetTable)
    {
        if (resetTable == null) return;
    }

    /// <summary>
    /// [루프 시작시 호출]-등록된 resetTables들의 ResetState를 순회 호출
    /// </summary>
    private void ResetAllResetTables()
    {
        int i = 0;
        while (i < resetTables.Count)
        {
            IResetTable resetTable = resetTables[i];
            if (resetTable != null) resetTable.ResetState();
            i++;
        }
    }

    //=================================================================//
    #endregion

    #region 루프컨트롤
    //======================================================================루프 컨트롤=====//
    /// <summary>
    /// 새 루프 시작 : 타이머/침입
    /// </summary>
    private void StartNewLoop()
    {
        if (loopCount == 0) SoundManager.Instance.PlayBgmByName("RoomLoop_BGM");

        loopCount += 1;
        //시간 초기화
        elapsedSeconds = 0.0f;
        //1차 침입 시간 결정 // 유니티 시스템 추가하니까, 랜덤어느걸 써야겠는지 모르겠다고 해서 명시적으로 엔진쪽으로 확정
        breakInSeconds = UnityEngine.Random.Range(breakInMinSeconds, breakInMaxSeconds);
        //비상키 시간은 배터리 제거 시에만 설정
        emergencyKeySeconds = -1.0f;
        //매 루프마다 배터리 제거 상태 리셋
        batteryRemoved = false;
        //1차 침입 대기 상태로 시작
        breakInPhase = BreakInPhase.FirstAttempt;

        //doorLock.RestoreBatteryState(); //변경 및 제거 예정

        ResetAllResetTables();
        TeleportPlayerToStart();

        bathVCam.Priority = 0;
        kitchenVcam.Priority = 0;

        LoopStarted?.Invoke(loopCount);
    }

    /// <summary>
    /// 루프 리셋 후 루프이유표시, 즉시 새 루프 시작
    /// 외부에서 루프 리셋 요청할때 호출
    /// </summary>
    /// <param name="reason"></param>
    public void ResetLoop(string reason)
    {
        if (isResetBlocked)
        {
            return;
        }

        if (isResetting) return;
        StartCoroutine(LoopResetCo(reason));
        SoundManager.Instance.StopSfx();
    }

    /// <summary>
    /// 루프 리셋 요청시 트리거 재진입/연속 호출 방지용 코루틴
    /// </summary>
    /// <param name="reason"></param>
    /// <returns></returns>
    private IEnumerator LoopResetCo(string reason)
    {
        isResetting = true;

        //한프레임 쉬고
        yield return null;

        StartNewLoop();

        //리셋 쿨타임 후 리셋 허용
        yield return new WaitForSeconds(resetCoolTime);
        isResetting = false;
    }

    /// <summary>
    /// 엔딩 등 특수 상황에서 루프리셋을 차단/해제
    /// </summary>
    /// <param name="blocked"></param>
    public void SetResetBlocked(bool blocked)
    {
        isResetBlocked = blocked;
    }

    //라스트페이즈 상황에서 루프 침입 타임라인 차단/해제용
    public void SetScenarioBlocked(bool blocked, string reason)
    {
        isScenarioBlocked = blocked;

        if (blocked)
        {
            //침입 단계 종료 처리-이벤트 추가 발행 방지
            breakInPhase = BreakInPhase.Done;
            emergencyKeySeconds = 1.0f;
        }
    }
    //=================================================================//
    #endregion

    #region 침입 타임라인
    //======================================================================침입 타임라인=====//
    /// <summary>
    /// 배터리 제거 여부 함수
    /// DoorLock 에서 호출,
    /// -루프 내의 행동이니까, StartNewLoop 시에 false로 초기화
    /// </summary>
    /// <param name="value"></param>
    public void SetBatteryRemoved(bool value)
    {
        batteryRemoved = value;
    }

    /// <summary>
    /// 침입 타임라인을 단계별로 처리
    /// -1차 침입 시점 도달시
    /// ->배터리 미제거 = 침입 성공(Done)
    /// ->배터리 제거 = 침입 실패 -> 비상키 시간 설정 ->waitingEmergencyKey
    /// -비상키 대기 상태에서 비상키 시점 도달 시 = 침입성공(Done)
    /// </summary>
    private void UpdateBreakInTimeLine()
    {
        if (breakInPhase == BreakInPhase.FirstAttempt)
        {
            if (elapsedSeconds >= breakInSeconds)
            {
                if (batteryRemoved)
                {
                    //1차 침입 실패-> 비상키 침입 시간으로 재설정
                    float extraDelay = UnityEngine.Random.Range(emergencyMinSeconds, emergencyMaxSeconds);
                    emergencyKeySeconds = elapsedSeconds + extraDelay;
                    //페이즈 변경
                    breakInPhase = BreakInPhase.WaitingEmergencyKey;
                    OnBreakInFailedByBattery();
                }
                else
                {
                    //1차 침입 성공 Done 페이즈로 변경
                    breakInPhase = BreakInPhase.Done;
                    OnBreakInSuccess(BreakInMethod.DoorLock);
                }
            }
        }
        else if (breakInPhase == BreakInPhase.WaitingEmergencyKey)
        {
            if (elapsedSeconds >= emergencyKeySeconds)
            {
                //비상키 침입 성공
                breakInPhase = BreakInPhase.Done;
                OnBreakInSuccess(BreakInMethod.EmergencyKey);
            }
        }
    }
    //=================================================================//
    #endregion

    #region 연출 훅 부분
    //======================================================================연출 훅 부분=====//
    /// <summary>
    /// [연출 훅] 배터리 제거로 인해 1차 침입이 실패했을때 호출
    /// -추후 괴한의" 왜 안열려?" 음성 이나 텍스트, 문 흔들리는 연출 들어갈 곳
    /// </summary>
    private void OnBreakInFailedByBattery()
    {
        //외부로 배터리제거로 인한 침입 지연 타이밍 알림
        BreakInFailedByBattery?.Invoke(emergencyKeySeconds); //관련 제거 예정
    }

    /// <summary>
    /// [연출 훅] 침입 성공 시 호출
    /// -추후 괴한 스폰/도어 오픈/조명 변화/대사 재생 붙일곳-여기가 옵저버패턴 쓰일곳
    /// </summary>
    /// <param name="method"></param>
    private void OnBreakInSuccess(BreakInMethod method)
    {
        BreakInSucceeded?.Invoke(method);
    }
    //=================================================================//
    #endregion

    #region 텔레포트 관련
    //======================================================================텔레포트 관련=====//

    /// <summary>
    /// 트래블 슈팅 시도
    /// 루프가 시작될때, 플레이어 위치가 변하지 않는 상황 간혹 발생
    /// 캐릭터 컨트롤러와 충돌문제인가 싶어(이동이 중복되는 문제),
    /// 위치변경시에는 컨트롤러 비활성화, 변경후에 다시 활성화
    /// </summary>
    private void TeleportPlayerToStart()
    {
        if (playerTransform == null || loopStartPoint == null)
        {
            return;
        }

        Vector3 targetPos = loopStartPoint.position + (Vector3.up * spawnYOffset);
        Quaternion targetRot = loopStartPoint.rotation;

        //루프 리셋후- 네비메쉬가 마지막 목적지로 다시 자동 이동하는 문제
        //->네비메쉬 이동 멈추고, 내장기능 warp사용
        if (playerAgent != null && playerAgent.enabled)
        {
            playerAgent.isStopped = true;
            playerAgent.ResetPath();
            playerAgent.velocity = Vector3.zero;

            //네비메쉬 워프로 안전하게 동기화
            playerAgent.Warp(targetPos);

            playerTransform.rotation = targetRot;

            playerAgent.isStopped = false;
            return;
        }

        //->캐릭터 컨트롤러 기반 텔레포트 처리
        if (characterController != null && characterController.enabled)
        {
            characterController.enabled = false;
            playerTransform.position = targetPos;
            playerTransform.rotation = targetRot;
            characterController.enabled = true;
            return;
        }

        //네비메쉬,컨트롤러 둘다 아니면 직접 이동
        playerTransform.position = targetPos;
        playerTransform.rotation = targetRot;
    }
    //=================================================================//
    #endregion

    /// <summary>
    /// 오프닝 연출때 첫 루프 StartNewLoop 호출할거 퍼블릭으로 래핑
    /// </summary>
    /// <param name="reason"></param>
    public void BeginGameFromOpening(string reason)
    {
        isGameRunning = true;
        isScenarioBlocked = false;
        
        StartNewLoop();
    }

    /// <summary>
    /// 오프닝 타임라인 재생전에 호출 할것
    /// -오픈 연출중에 루프 시스템 완전히 정지시키게
    /// </summary>
    /// <param name="reason"></param>
    public void PrepareForOpening(string reason)
    {
        isGameRunning = false;
        isScenarioBlocked = true;
        breakInPhase = BreakInPhase.Done;
        emergencyKeySeconds = -1.0f;
    }
}
