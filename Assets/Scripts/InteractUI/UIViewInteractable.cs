using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UIView 형 (단순 보여지기 형) 상호작용
/// 클릭->이동->도착시 UI를 띄우기만 할 용도
/// 
/// -기존 인터랙터블베이스 상호작용 흐름대로
/// </summary>
public class UIViewInteractable : InteractableBase
{
    [Header("열게될 UI패널-UIPanelBase 포함한")]
    [SerializeField] private UIPanelBase panel;

    [Header("디버그/연출용 reason")]
    [SerializeField] private string reason = "UIView 상호작용 내용";

    public override void Interact(GameObject interactor)
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        InteractContext context = new InteractContext(interactor, gameObject, reason);
        UIManager.Instance.OpenPanel(panel, context);
    }
}
