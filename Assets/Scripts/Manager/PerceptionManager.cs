using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 플레이어의 '인지상태(환각/현실)' 을 전역 규칙으로 관리할 핵심 매니저 역할
/// 
/// [주 목적]
/// 1.환각/현실 상태를 여기서 관리
/// 2.현실 단서를 발견하면, realityMeter 현실 인지 누적치 상승
/// 3.realityMeter 가 올라갈수록 랜덤하게 현실순간을 나타내는 빈도 상승 RealityGlimpse
/// 4.약 복용시 일정시간동안 강제 현실인지 ForceReality
/// 5.상태변화는 이벤트로 외부에 알릴 것 (여기선 로직 처리 안하겠다는거 옵저버로 이벤트만)
/// 
/// -데이터 구조는 1회성 처리, 같은 단서 계속 누른다고 리얼리티글립스 폭증하지 않게
/// 
/// [주의할것]
/// -여기선 게임 규칙만 담당
/// -시각적 표현은 다른 컴포넌트가 구독해서 처리하는 방식으로 갈것
/// 
/// +[루프리셋 관련 문제점]
/// -루프리셋시 플레이어의 PerceptionState를 기본 디폴트 상태인 Hallucination으로 복구해야함
/// ->리셋테이블에 등록할 것
/// -누적치와 발견단서는 유지해야됨-인지적 오브젝트니까, 초기화되면 이상하잖아
/// </summary>
public class PerceptionManager : MonoBehaviour ,IResetTable
{
    //인지 상태 정의
    public enum PerceptionState
    {
        Hallucination = 0,
        Reality = 1
    }

    //전역 접근용 인스턴스
    public static PerceptionManager Instance { get; private set; }

    //이벤트관련- 상태변경 외부에 알릴것 (old/new/reason)
    public event Action<PerceptionState, PerceptionState, string> StateChanged;

    [Header("초기 인지 상태")]
    [SerializeField] private PerceptionState initialState = PerceptionState.Hallucination;

    [Header("현실 인지 누적치(0~1)")]
    [Range(0.0f,1.0f)]
    [SerializeField] private float realityMeter = 0.0f;

    [Header("랜덤 현실 검사 간격")]
    [SerializeField] private float randomCheckInterval = 2.0f;

    [Header("기본 현실 발생 확률(0~1)")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float baseRealityChance = 0.05f;

    [Header("누적치에 따른 추가 확률(0~1)")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float meterToChanceMultiplier = 0.1f;

    [Header("랜덤으로 현실이 되었을때, 유지시간")]
    [SerializeField] private float randomRealityDuration = 2.0f;

    [Header("단서 발견시 즉시 현실을 잠깐 보여줄지 여부")]
    [SerializeField] private bool giveInstantGlimpseOnEvidence = true;

    [Header("단서 발견시 현실 지속시간")]
    [SerializeField] private float evidenceGlimpseDuration = 1.0f;

    //현재 상태 저장용
    private PerceptionState curState;

    //강제 현실 종료 시간
    private float forceRealityEndTime;

    //랜덤 현실 종료 시간
    private float tempRealityEndTime;

    //랜덤 검사 타이머
    private float randomTimer;

    //단서 중복 처리 방지 -한번 찾은 단서는 해쉬셋에 등록
    private readonly HashSet<string> discoveredEvidenceIds = new HashSet<string>();

    //루프매니저 등록 여부 타이밍 이슈 방지용
    private bool isRegisteredToLoop;

    //외부 접근용 프로퍼티
    public PerceptionState CurState { get { return curState; } }
    public float RealityMeter { get { return realityMeter; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //DDL은 딱히 없어도 될거 같긴한데.. 어차피 씬 하나에서 다 이루어질거니까.
        //일단 집어넣고 나중에 생각
        //DontDestroyOnLoad(gameObject);

        curState = initialState;
    }

    private void OnEnable()
    {
        //루프 리셋시 인지상태만 초기화
        TryRegisterToLoopManager();
    }

    private void OnDisable()
    {
        TryUnRegisterToLoopManager();
    }

    private void Start()
    {
        //OnEnable 문제때문에 start에도 넣음-라이프 사이클 관련
        TryRegisterToLoopManager();

        //시작 시점에 외부가 동기화 할수 있게 이벤트 발송
        //old,new  상태 동일방지, 내부 SetState에서 처리
        SetState(curState, "게임 시작 초기 상태 적용");
    }

    private void Update()
    {
        UpdateForcedReality();
        UpdateTempReality();
        UpdateRandomRealityTick();
    }

    /// <summary>
    /// 루프매니저-리셋테이블 모두 초기화할때 호출됨
    /// -인지상태만 초기화 하고, 타이머 끊고,
    /// -누적파라미터와 발견단서 목록은 유지
    /// </summary>
    public void ResetState()
    {
        forceRealityEndTime = 0.0f;
        tempRealityEndTime = 0.0f;
        randomTimer = 0.0f;

        SetState(PerceptionState.Hallucination, "루프 리셋 : 인지상태 디폴트값으로 초기화");
        Debug.Log("[PerceptionManager] ResetState : 인지상태만 초기화됨.(누적/단서발견 유지)");
    }

    /// <summary>
    /// 단서 보유 여부 확인(폰 신고 성공/실패 분기용)
    /// </summary>
    /// <param name="evidenceId"></param>
    public bool HasEvidence(string evidenceId)
    {
        if(string.IsNullOrEmpty(evidenceId)) return false;
        return discoveredEvidenceIds.Contains(evidenceId);
    }

    /// <summary>
    /// 단서 발견 처리
    /// -단서Id의 중복 발견 방지
    /// -성공적으로 처리시 realityMeter 증가
    /// </summary>
    /// <param name="evidenceId">단서 고유 Id</param>
    /// <param name="meterGain">0~1 범위로 설정예정</param>
    /// <param name="reason">디버그/연출용으로 사용</param>
    public bool RegisterEvience(string evidenceId, float meterGain, string reason)
    {
        if (discoveredEvidenceIds.Contains(evidenceId))
        {
            Debug.Log("[PerceptionManager] 이미 발견된 단서-> 현실파라미터 누적 중복처리 방지 : " + evidenceId);
            return false;
        }

        discoveredEvidenceIds.Add(evidenceId);

        float oldMeter = realityMeter;
        realityMeter += meterGain;

        if (realityMeter > 1.0f) realityMeter = 1.0f;

        Debug.Log("[PerceptionManager] 단서 발견 :"
            + evidenceId + "/ 변경점:" + oldMeter.ToString("F2") + "->" + realityMeter.ToString("F2")
            + "/이유: " + reason);

        //단서 발견시 짧은 순간동안 현실 반영-플레이어가 인지할 수 있을정도로만,
        if (giveInstantGlimpseOnEvidence)
        {
            EnterTempReality(evidenceGlimpseDuration, "단서 발견 임시 현실");
        }
        return true;
    }

    /// <summary>
    /// 약 복용으로 인한 현실 강제 적용(초기 계획 30초)
    /// -duration 동안 현실상태 유지하게,
    /// -끝나면 기본적으로 환각상태로 복귀
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="reason"></param>
    public void ForceReality(float duration, string reason)
    {
        //시간 설정 0이하면 무시
        if (duration <= 0.0f) return;

        forceRealityEndTime = Time.time + duration;
        //임시 현실 적용시간은 0으로
        tempRealityEndTime = 0.0f;

        SetState(PerceptionState.Reality, "강제 현실 작동: " + reason + "/ 적용시간 : " + duration.ToString("F2") + "s");
    }

    /// <summary>
    /// 강제 현실(약 복용) 이 끝났는지 검사하고 종료 처리
    /// </summary>
    private void UpdateForcedReality()
    {
        if (forceRealityEndTime <= 0.0f) return;

        if (Time.time >= forceRealityEndTime)
        {
            forceRealityEndTime = 0.0f;

            //강제 현실 끝나면 기본적으로 환각상태로 복귀, 추후 랜덤상태유지는 일단 보류
            SetState(PerceptionState.Hallucination, "강제 현실 종료됨");
        }
    }

    /// <summary>
    /// 랜덤 현실, 증거발견으로 인한 임시 현실 끝났는지 검사
    /// 강제 현실중엔 무시.
    /// </summary>
    private void UpdateTempReality()
    {
        if (forceRealityEndTime > 0.0f) return;

        if (tempRealityEndTime <= 0.0f) return;

        if (Time.time >= tempRealityEndTime)
        {
            tempRealityEndTime = 0.0f;
            SetState(PerceptionState.Hallucination, "임시 현실 종료됨");
        }
    }

    /// <summary>
    /// 일정 간격으로 랜덤 현실 발생 검사
    /// -현실 인지 누적치(realityMeter) 높을수록 현실 발생확률 증가하게
    /// -강제현실중엔 검사x
    /// -이미 현실 상태라면 중복방지
    /// </summary>
    private void UpdateRandomRealityTick()
    {
        if (forceRealityEndTime > 0.0f) return;

        //이미 현실 상태면 랜덤 발생 검사x
        if (curState == PerceptionState.Reality) return;

        randomTimer += Time.deltaTime;
        if (randomTimer < randomCheckInterval) return;

        randomTimer = 0.0f;

        float chance = baseRealityChance + (realityMeter * meterToChanceMultiplier);
        if (chance > 1.0f) chance = 1.0f;

        float roll = UnityEngine.Random.value;
        if (roll <= chance)
        {
            EnterTempReality(randomRealityDuration, "랜덤 현실 발생(누적치 기반)");
        }
    }

    /// <summary>
    /// 임시 현실 진입
    /// -강제 현실이 아닐때만 작동
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="reason"></param>
    private void EnterTempReality(float duration, string reason)
    {
        if (duration <= 0.0f) return;

        tempRealityEndTime = Time.time + duration;

        SetState(PerceptionState.Reality, "임시 현실 작동: " + reason + "/ 적용시간 : " + duration.ToString("F2") + "s");
    }

    /// <summary>
    /// 상태 변경 공통처리
    /// -같은 상태로 재적용 도 허용하게
    /// </summary>
    /// <param name="newState"></param>
    /// <param name="reason"></param>
    private void SetState(PerceptionState newState, string reason)
    {
        PerceptionState oldState = curState;
        curState = newState;

        Debug.Log("[PerceptionManager] 상태 변경 : " + oldState + "->" + newState + "/변경사유 : " + reason);

        if (StateChanged != null) StateChanged.Invoke(oldState, newState, reason);
    }

    /// <summary>
    /// 리셋테이블 등록-start에도 넣게 따로 함수로 뺌
    /// </summary>
    private void TryRegisterToLoopManager()
    {
        if (isRegisteredToLoop) return;
        if (LoopManager.Instance == null) return;

        LoopManager.Instance.RegisterResetTable(this);
        isRegisteredToLoop = true;
    }

    /// <summary>
    /// 리셋테이블 해제
    /// </summary>
    private void TryUnRegisterToLoopManager()
    {
        if (!isRegisteredToLoop) return;
        if (LoopManager.Instance == null)
        {
            isRegisteredToLoop = false;
            return;
        }

        LoopManager.Instance.UnRegisterResetTable(this);
        isRegisteredToLoop = false;
    }
}
