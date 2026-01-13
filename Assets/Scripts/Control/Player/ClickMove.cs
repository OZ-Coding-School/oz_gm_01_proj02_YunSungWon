using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


/// <summary>
/// 마우스 클릭기반 위주
/// -바닥 클릭->위치로 이동
/// -상호작용 오브젝트 클릭->해당 오브젝트의 상호작용포인트로 이동 + 타겟 저장
/// 상호작용 실행 자체는 '도착후'를 기점으로 처리
/// </summary>
public class ClickMove : MonoBehaviour
{
    [Header("메인카메라")]
    [SerializeField] private Camera mainCamera;

    [Header("레이 거리")]
    [SerializeField] private float rayDistance = 100.0f;

    [Header("바닥 레이어")]
    [SerializeField] private LayerMask groundMask;

    [Header("상호작용 레이어")]
    [SerializeField] private LayerMask interactableMask;

    private NavMeshAgent agent;

    //상호작용할 타겟 저장용
    private InteractTarget curTarget;

    //저장된 타겟 외부접근용
    public InteractTarget CurTarget { get { return curTarget; } }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ClickHandle();
        }
    }

    /// <summary>
    /// 마우스 클릭 피드백 관리
    /// </summary>
    private void ClickHandle()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        //상호작용 오브젝트 우선적으로 판별하고 이동
        bool hitInteractable = Physics.Raycast(ray, out hit, rayDistance, interactableMask);
        if (hitInteractable)
        {
            InteractTarget target = hit.collider.GetComponentInParent<InteractTarget>();
            if (target != null && target.HasInteractables)
            {
                SetTargetAndMove(target);
                return;
            }
        }
        //바닥 클릭하면 저장타겟을 클리어, 이동만
        bool hitGround = Physics.Raycast(ray, out hit, rayDistance, groundMask);
        if (hitGround)
        {
            ClearTarget();
            MoveTo(hit.point);
        }
    }

    /// <summary>
    /// 상호작용 타겟지정+이동
    /// </summary>
    /// <param name="target"></param>
    private void SetTargetAndMove(InteractTarget target)
    {
        curTarget = target;
        Vector3 destination = target.InteractPoint.position;
        MoveTo(destination);

        Debug.Log("[ClickMove] 타겟 지정됨,여기로 이동할게?" + target.name);
    }

    /// <summary>
    /// 목적지로 이동기능만
    /// </summary>
    /// <param name="destination"></param>
    private void MoveTo(Vector3 destination)
    {
        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    /// <summary>
    /// 저장 타겟 초기화
    /// </summary>
    public void ClearTarget()
    {
        curTarget = null;
    }
}
