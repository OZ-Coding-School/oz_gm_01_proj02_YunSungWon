using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


/// <summary>
/// 네비메쉬 클릭이동 테스트 급하게 일단 움직이는거만,
/// </summary>
public class ClickMove : MonoBehaviour
{
    [Header("메인카메라")]
    [SerializeField] private Camera mainCamera;

    [Header("레이 거리")]
    [SerializeField] private float rayDistance = 100.0f;

    private NavMeshAgent agent;

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
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            bool isHit = Physics.Raycast(ray, out hit, rayDistance);

            if (isHit)
            {
                agent.SetDestination(hit.point);
            }
        }
    }
}
