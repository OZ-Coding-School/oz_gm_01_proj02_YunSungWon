using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI안의 버튼 클릭으로 실제 효과를 적용 하는 액션형 UI패널의 공용 베이스
/// 
/// -open시 상호작용 컨텍스트 적용
/// -UI버튼이 눌렸을때, ApllyAction()을 호출할 수 있게
/// 
/// 템플릿 메서드 패턴 -실제 액현은 파생 클래스가 구현할 것
/// </summary>
public abstract class UIActionPanelBase : UIPanelBase
{
    //현재 패널이 열릴때 컨텍스트 저장
    protected InteractContext Context { get; private set; }

    /// <summary>
    /// 패널이 열릴 때 컨텍스트 저장
    /// </summary>
    /// <param name="context"></param>
    protected override void OnOpen(InteractContext context)
    {
        Context = context;
        OnOpenActionPanel(context);
    }

    /// <summary>
    /// 패널이 닫힐 때 컨텍스트 정리
    /// </summary>
    protected override void OnClose()
    {
        OnCloseActionPanel();
        Context = null;
    }

    //파생 패널이 열릴 때 추가로 처리할 내용
    protected abstract void OnOpenActionPanel(InteractContext context);

    //파생 패널이 닫힐 때 추가로 처리할 내용
    protected abstract void OnCloseActionPanel();
}
