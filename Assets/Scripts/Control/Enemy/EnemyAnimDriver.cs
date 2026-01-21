using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimDriver : MonoBehaviour
{
    [Header("애니메이터 참조")]
    [SerializeField] private Animator animator;

    [Header("탑뷰 이동용-네비메쉬")]
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Header("속도 파라미터 갱신 설정")]
    [Header("애니메이션 댐핑")]
    [SerializeField] private float speedDampTime = 0.10f;
    [Header("떨림 제거용")]
    [SerializeField] private float speedEpsilon = 0.01f;

    //Animator 속도 파라미터 해시
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (animator == null) return;

        float speed = navMeshAgent.velocity.magnitude;
        //0근처= 0으로 만들어서 덜렁덜렁한거 방지
        if (speed < speedEpsilon) speed = 0.0f;

        //댐핑넣어서 애니메이션 자연스럽게 변환
        animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);
    }

}
