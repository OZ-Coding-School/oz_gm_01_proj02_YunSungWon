using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 상호작용UI 패널의 공통 베이스 역할
/// 
/// 패널은 반드시 UIManager.OpenPanel을 통해서만 열리게,
/// Open시에 InteractContext를 전달받아서, 어떤걸 어떤이유로 열었는지 
/// 
/// 템플릿 메서드 패턴 처럼->open/close 구조는 고정, 내용은 파생해서 구현
/// -얘도 옵저버 써서 이벤트 외부에서 연결가능하게
/// </summary>
public abstract class UIPanelBase : MonoBehaviour
{
    //패널이 열렸는지 여부
    public bool IsOpen { get; private set; }

    //마지막으로 Open 할때 전달받은 컨텍스트
    public InteractContext LastContext { get; private set; }

    //패널 열림 이벤트
    public event Action<UIPanelBase> Opened;

    //패널 닫힘 이벤트
    public event Action<UIPanelBase> Closed;

    /// <summary>
    /// UIManager가 호출하는 패널 열기 진입점
    /// </summary>
    /// <param name="context"></param>
    public void Open(InteractContext context)
    {
        LastContext = context;
        IsOpen = true;

        gameObject.SetActive(true);

        OnOpen(context);

        if (Opened != null) Opened.Invoke(this);
    }

    /// <summary>
    /// UIManager가 호출하는 패널 닫기 진입점
    /// </summary>
    public void Close()
    {
        if (!IsOpen)
        {
            gameObject.SetActive(false);
            return;
        }

        IsOpen = false;

        OnClose();

        gameObject.SetActive(false);

        if(Closed != null) Closed.Invoke(this);
    }

    //파생 패널이 열릴때 구현해야 하는 내용
    protected abstract void OnOpen(InteractContext context);

    //파생 패널이 닫힐때 구현해야 하는 내용
    protected abstract void OnClose();
}
