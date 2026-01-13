using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 열때, 상호작용 대상, 어떤 UI, 이유를 전달하기 위한 컨텍스트 데이터
/// -단순 UI 활성화/ 상호작용 UI 활성화 든 동일 구조로 전달하기 위함
/// </summary>
/// 재정의 불가하게,상속불가 sealed 클래스로,
public sealed class InteractContext
{
    //상호작용을 실행한 주체-플레이어
    public GameObject Interactor { get; private set; }

    //상호작용 대상(상호작용 오브젝트)
    public GameObject Source { get; private set; }

    //디버그,연출용 문자열(이유)
    public string Reason { get; private set; }

    //컨텍스트 생성자
    public InteractContext(GameObject interactor, GameObject source, string reason)
    {
        Interactor = interactor;
        Source = source;
        Reason = reason;
    }
}
