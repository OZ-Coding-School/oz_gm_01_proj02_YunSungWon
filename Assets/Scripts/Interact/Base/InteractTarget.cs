using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 네비메쉬 자동 상호작용 데이터 제공 컴포넌트
/// -플레이어가 서야 할 위치 = InteractPoint
/// -상호작용 허용 거리 InteractDistance
/// -실제 실행할 InteractableBase참조
/// 
/// 클릭으로 오브젝트를 선택했을때, '어디로 이동해서'/'언제상호작용'의
/// 기준점이 필요함. 따로 별도 컴포넌트로 분리
/// </summary>
public class InteractTarget : MonoBehaviour
{
    [Header("상호작용할 실행 대상")]
    [SerializeField] private InteractableBase interactable;

    [Header("플레이어가 이동, 서야 할 위치")]
    [SerializeField] private Transform interactPoint;

    [Header("상호작용 허용거리")]
    [SerializeField] private float interactDistance = 1.0f;

    //외부 접근용 프로퍼티
    public InteractableBase Interactable { get { return interactable; } }
    public Transform InteractPoint { get { return interactPoint; } }
    public float InteractDistance { get { return interactDistance; } }

    private void Awake()
    {
        if (interactable == null)
            interactable = GetComponent<InteractableBase>();
        if (interactPoint == null)
            interactPoint = this.transform;
    }
}
