using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 라스트 페이즈 전용 변이괴한 컨트롤러
/// -무조건 플레이어 추격(일단 1차 초안 임시)
/// -플레이어에 근접시 롤백(킬처리)
/// -화장실 문 막고 있으면 화장실문에 근접시 문 강제 파괴
/// </summary>
public class LastPhaseEnemyControl : MonoBehaviour
{
    [Header("추격 갱신 간격")]
    [SerializeField] private float chaseUpdateInterval = 0.5f;

    //NavMesh 이동 담당
    private NavMeshAgent agent;

    //추격 대상(플레이어)
    private Transform playerTransform;

    //잡힘 처리 시 롤백 호출용(EndingDirector)
    private EndingDirector endingDirector;

    //SetDestination 갱신 타이머
    private float timer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// EndingDirector가 스폰 직후 1회 호출, 필요한 참조 주입
    /// -화장실 문 부수는거 같이 넣으려고 했다가 머리 터질거 같아서,
    /// -그냥 문 트리거에 변이괴한 닿으면 터지게 구현하려고 함
    /// </summary>
    public void Initialize(Transform playertransform, EndingDirector director)
    {
        this.playerTransform = playertransform;
        endingDirector = director;
    }

    private void Update()
    {
        if (agent == null || playerTransform == null || endingDirector == null) return;

        //목적지 갱신
        timer += Time.deltaTime;
        if (timer >= chaseUpdateInterval)
        {
            timer = 0.0f;
            agent.SetDestination(playerTransform.position);
        }
    }
}
