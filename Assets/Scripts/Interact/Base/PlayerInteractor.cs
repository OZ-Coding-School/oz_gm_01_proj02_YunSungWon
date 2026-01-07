using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 상호작용 입력(E키), 레이캐스트 담당
/// -카메라 기준 Raycast로 IInteractable 탐색,
/// -E키 입력시 Interact 호출
/// -Physics.Raycast, 인터페이스 기반의 호출
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("상호작용 최대 거리")]
    [SerializeField] private float interactDistance = 1.0f;

    [Header("상호작용할 레이어 마스크")]
    [SerializeField] private LayerMask interactLayerMask;

    [Header("메인카메라 참조")]
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        //카메라 비어있을경우 방어
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        //디버그용,테스트용 - E키 상호작용은 라스트 페이즈 현관문에만 달것
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        bool isHit = Physics.Raycast(ray, out hit, interactDistance, interactLayerMask);
        if (!isHit) return;
        
        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null) return;

        interactable.Interact(gameObject);
    }
}
