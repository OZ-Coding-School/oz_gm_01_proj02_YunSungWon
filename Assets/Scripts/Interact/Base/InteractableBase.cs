using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상호작용 오브젝트의 공통 베이스 클래스
/// DisplayName 제공(추후 UI에서도 사용가능하게)
/// </summary>
public abstract class InteractableBase : MonoBehaviour ,IInteractable
{
    [Header("상호작용 표시 이름")]
    [SerializeField] private string displayName = "상호작용 가능한 오브젝트 이름";

    public string DisplayName { get { return displayName; } }

    public abstract void Interact(GameObject interactor);
}
