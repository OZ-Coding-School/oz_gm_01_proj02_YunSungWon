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
/// [구현목표]
/// -간단 상태 머신 구조로 책임 명확히 분리할것
/// -추후 애니메이션/공격/탐색 로직(화장실/라스트페이즈) 등 상태 확장을 염두할 것
/// </summary>
public class EnemyControl : MonoBehaviour
{
    private enum EnemyState
    {
        None = 0,
        Entrance = 1,
        Chasing = 2
    }

    [Header("추격대상-플레이어")]
    [SerializeField] private Transform playertarget;

    [Header("네비메쉬 참조")]
    [SerializeField] private NavMeshAgent agent;

    [Header("추격 목적지 갱신 간격")]
    [SerializeField] private float chaseUpdateInterval = 0.1f;

    [Header("연출용 : 스폰직후 멈춰있는 시간")]
    [SerializeField] private float entranceDelay = 2.0f;

    [Header("연출용 : 대사 출력(임시)")]
    [SerializeField] private string entranceText = "내집에서 나가!!!";

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
    public void Initialize(Transform player, LoopManager manager)
    {
        playertarget = player;
        loopManager = manager;
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

        //추격 시작
        agent.isStopped = false;

        enemyState = EnemyState.Chasing;
        chaseTimer = 0.0f;

        Debug.Log("[EnemyControl] 추격 시작");
    }

    private void Update()
    {
        if (enemyState != EnemyState.Chasing) return;
        if (playertarget == null || agent == null) return;

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
}
