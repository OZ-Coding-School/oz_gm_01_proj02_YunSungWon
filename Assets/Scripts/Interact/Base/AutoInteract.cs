using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ClickMove 가 지정한 타겟에 대해
/// 플레이어가 InteractPoint 에 도달하면, 자동으로 Interact() 실행
/// 
/// [추후 연출 고려]
/// 일단 딜레이 직접 넣어서 지연시간 넣어주고, 나중에 애니메이션 트리거 이벤트로 교체 예정
/// </summary>
public class AutoInteract : MonoBehaviour
{
    [Header("타겟 제공자")]
    [SerializeField] private ClickMove clickMove;

    [Header("애니메이터(임시 추후 추가 예정)")]
    [SerializeField] private Animator animator;

    [Header("애니메이션 트리거 이름(추가예정)")]
    [SerializeField] private string interactTriggerName = "intract";

    [Header("애니메이션 없을때 딜레이로 조정")]
    [SerializeField] private float fallbackDelay = 0.1f;

    [Header("도착 판정 여유 값")]
    [SerializeField] private float arriveTolerance = 0.2f;

    //네비메쉬 참조
    private NavMeshAgent agent;

    //과정진행중인지 플래그
    private bool isRunning;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        clickMove = GetComponent<ClickMove>();
    }

    private void Update()
    {
        if (isRunning) return;
        if (clickMove == null) return;

        InteractTarget target = clickMove.CurTarget;
        if (target == null) return;
        if (agent.pathPending) return;

        float arriveDistance = agent.stoppingDistance + arriveTolerance;
        if (agent.remainingDistance > arriveDistance) return;

        float distToPoint = Vector3.Distance(this.transform.position, target.InteractPoint.position);
        if (distToPoint > target.InteractDistance) return;

        StartCoroutine(InteractCO(target));
    }

    private IEnumerator InteractCO(InteractTarget target)
    {
        isRunning = true;
        agent.isStopped = true;

        //타겟 방향 바라보기
        Vector3 dir = target.transform.position - this.transform.position;
        dir.y = 0.0f;
        if (dir.sqrMagnitude > 0.001f)
            this.transform.rotation = Quaternion.LookRotation(dir,Vector3.up);

        //애니메이션 연출 있을시
        if (animator != null && !string.IsNullOrEmpty(interactTriggerName))
        {
            animator.ResetTrigger(interactTriggerName);
            animator.SetTrigger(interactTriggerName);
        }
        //애니메이션 없으면 직접 딜레이
        yield return new WaitForSeconds(fallbackDelay);

        if (target != null && target.Interactable != null)
            target.Interactable.Interact(this.gameObject);

        //1회 실행후 타겟 해제
        if(clickMove != null) clickMove.ClearTarget();

        agent.isStopped = false;
        isRunning = false;
    }
}
