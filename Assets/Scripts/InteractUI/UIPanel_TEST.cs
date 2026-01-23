using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테스트용 더미 패널 스크립트
/// 패널 정상적으로 상호작용시에 열리고 닫히는지 확인하려고,
/// 일단 급하게 로그 뽑아내는 용도로만 사용
/// </summary>
public class UIPanel_TEST : UIPanelBase
{
    [Header("패널 이름-디버그 확인용")]
    [SerializeField] private string panelName = "더미패널";

    //사운드 관련
    private void OnEnable()
    {
        if (panelName == "달력") SoundManager.Instance.PlaySfxByName("CalendarInteract_SFX");
        if (panelName == "박스") SoundManager.Instance.PlaySfxByName("BoxInteract_SFX");
        if (panelName == "노트") SoundManager.Instance.PlaySfxByName("Recipt_SFX");
        if (panelName == "그림") SoundManager.Instance.PlaySfxByName("getPhone_SFX");
    }

    protected override void OnOpen(InteractContext context)
    {
        Debug.Log("[UIPanel_TEST] 패널open" + panelName + "열린이유=" +context.Reason);
    }

    protected override void OnClose()
    {
        Debug.Log("[UIPanel_TEST] 패널Close" + panelName);
    }
}
