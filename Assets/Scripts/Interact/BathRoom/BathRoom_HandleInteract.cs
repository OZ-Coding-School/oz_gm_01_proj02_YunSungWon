using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화장실 문고리 클릭 상호작용 포인트
/// InteractableBase상속받고, 플레이어 상호작용 시스템에서 호출,
/// -실제 문 잠금 상태 전환 로직은 BathRoom_DoorControl 에서 담당중
/// 
/// [주 역할]
/// -문고리 클릭시, BathRoom_DoorControl.InteractHandle()호출
/// 
/// [구현목적]
/// -상태머신 문 로직과 클릭되는 콜라이더를 분리,
/// -추후 연출관련 변경사항 생길때 로직 유지하기 위함
/// </summary>
public class BathRoom_HandleInteract : InteractableBase
{
    [Header("문 상태 컨트롤러")]
    [SerializeField] private BathRoom_DoorControl doorControl;

    public override void Interact(GameObject interactor)
    {
        doorControl.InteractHandle();
    }
}
