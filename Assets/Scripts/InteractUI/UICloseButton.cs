using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 패널의 닫기 버튼에 붙일 컴포넌트
/// </summary>
public class UICloseButton : MonoBehaviour
{
    [Header("디버그/연출용-닫은 사유")]
    [SerializeField] private string reason = "플레이어가 닫기 버튼 클릭";

    /// <summary>
    /// 버튼 OnClick 에서 호출할것
    /// </summary>
    public void Close()
    {
        if (UIManager.Instance == null) return;

        UIManager.Instance.CloseCurPanel(reason);
    }
}
