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
/// 
/// [연출패스 기간 변경점]
/// -기존 : 도착->애니 트리거->fallbackDelay 후에 상호작용 실행
/// -변경 : 도착->애니 트리거 -> 애니 이벤트 오면 상호작용 실행
/// (애니 이벤트가 없을 경우에만 fallbackdelay 실행)
/// 
/// 여기선, 실행 타이밍만 조절하고,
/// 실제 로직은 인터랙터블에서 담당하게 유지
/// 
/// </summary>
public class AutoInteract : MonoBehaviour
{
    [Header("타겟 제공자")]
    [SerializeField] private ClickMove clickMove;

    [Header("상호작용 AnimView 참조")]
    [SerializeField] private PlayerInteractAnimView playerInteractAnimView;

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
        playerInteractAnimView = GetComponent<PlayerInteractAnimView>();
    }

    private void Update()
    {
        if (isRunning) return;
        if (clickMove == null) return;
        if (agent == null) return;

        InteractTarget target = clickMove.CurTarget;
        if (target == null) return;

        //경로 계산중엔 도착 판정 보류
        if (agent.pathPending) return;

        //에이전트 기준 도착 판정
        float arriveDistance = agent.stoppingDistance + arriveTolerance;
        if (agent.remainingDistance > arriveDistance) return;

        //실제 상호작용 포인트까지 거리 체크
        float distToPoint = Vector3.Distance(this.transform.position, target.InteractPoint.position);
        if (distToPoint > target.InteractDistance) return;

        StartCoroutine(InteractCo(target));
    }

    private IEnumerator InteractCo(InteractTarget target)
    {
        isRunning = true;
        agent.isStopped = true;

        //타겟 방향 바라보기
        Vector3 dir = target.transform.position - transform.position;
        dir.y = 0.0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        //플레이어 애니메이션 재생
        if (playerInteractAnimView != null)
        {
            //애니 아직 끝나지 않았으면 상호작용은 실행하지 않게
            if (playerInteractAnimView.IsBusy)
            {
                agent.isStopped = false;
                isRunning = false;
                yield break;
            }

            bool played = playerInteractAnimView.TryPlay(target.InteractAnimId);

            //TryPlay 실패시에도 상호작용 실행하지 않게
            if (!played)
            {
                agent.isStopped = false;
                isRunning = false;
                yield break;
            }

            //상호작용 이벤트 순간까지 대기
            while (!playerInteractAnimView.IsAnimEventArrived)
            {
                yield return null;
            }
        }
        Debug.Log("[AutoInteract]실제 상호작용 들어갑니다 이제");
        //실제 상호작용 실행
        ExecuteInteract(target);

        //1회 실행후 타겟 해제
        if (clickMove != null) clickMove.ClearTarget();

        agent.isStopped = false;
        isRunning = false;
    }

    /// <summary>
    /// 인터랙트 타겟에 캐싱된 인터랙터블 베이스들 순서대로 실행
    /// -각 상호작용 오브젝트의 연출은 각 인터랙터블 view 훅에서 처리
    /// -여기서는 상호작용 기능Interact만 호출
    /// </summary>
    /// <param name="target"></param>
    private void ExecuteInteract(InteractTarget target)
    {
        if (target == null) return;

        IReadOnlyList<InteractableBase> list = target.Getinteractables();
        
        int i = 0;
        while (i < list.Count)
        {
            InteractableBase interactable = list[i];
            if (interactable != null && interactable.enabled)
            {
                //상호작용 실행만 호출, 연출 부분은 로직쪽에 있음
                interactable.Interact(gameObject);
            }
            i++;
        }
    }
}
