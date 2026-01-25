using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 액션형 UI 상호작용
/// ->클릭-이동-도착-UI오픈 후
/// 실제 효과는 UI버튼 클릭 시점에 적용되게 변경
/// 
/// 약통, 핸드폰 UI와 관련
/// </summary>
public class UIActionInteractable : InteractableBase
{
    [Header("열 UI 패널(UI액션패널베이스)")]
    [SerializeField] private UIActionPanelBase panel;

    [Header("디버그/연출용 사유")]
    [SerializeField] private string reason = "UIAction 상호작용";

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
