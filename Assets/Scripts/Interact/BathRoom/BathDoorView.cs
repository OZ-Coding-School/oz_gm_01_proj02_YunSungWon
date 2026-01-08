using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BathRoom_DoorControl 의 문상태를 받아서,
/// 실제 문 모델을 회전시켜 시각적으로 표현하기 위한 컴포넌트
/// 
/// -로직(문 상태)은 BathRoom_DoorControl에서 관리
/// -문 상태표현을 여기 BathDoorView 에서 관리
/// </summary>
public class BathDoorView : MonoBehaviour
{
    //문 연출 만들고 끝내려고 했는데, 일단 여기까지..
    //힌지 조인트라는거 써볼까 했는데, 절대 안됨.(물리력으로 열리는거라)
    //내 지금 상태기반 다 박살날수도 있음
}
