using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 루프 리셋시,관련 오브젝트가 각자의 상태를 초기화 하도록 강제하는 인터페이스
/// 
/// -초기화 로직이 이곳저곳으로 흩어지고 있어서,
/// 인터페이스로 각자의 책임 범위를 복구할 수 있도록 하기위함
/// 
/// [구현목표]
/// -단일 책임원칙, 각 컴포넌트가 자기상태만 복구
/// -메인인 루프매니저에선 ResetState()만 호출하면 되는식으로,
/// </summary>
public interface IResetTable
{
    //매 루프 시작시(리셋후 시작 전/후 타이밍에) 호출
    //오브젝트가 루프 시작 상태로 되돌아가도록 내부 상태 초기화
    void ResetState();
}
