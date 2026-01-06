using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 도어락 관련 상호작용 (배터리 제거)
/// -1회만 제거 가능
/// -제거시 LoopManager에 BatteryRemoved = true 플래그 전달
/// -일단 임시로 배터리 비주얼 비활성화로 "뽑힘" 표현
/// 
/// [디자인/패턴]
/// -InteractableBase 상속: 상호작용 방식 통일
/// -LoopManager 참조를 통한 상태 전달(지금은 임시로 단순 플래그만 전달)
/// </summary>
public class DoorLock : InteractableBase
{
    [Header("루프 매니저 참조")]
    [SerializeField] private LoopManager loopManager;

    [Header("배터리 제거 후 비활성화 할 오브젝트")]
    [SerializeField] private GameObject batteryVisual;

    private bool isRemoved;

    /// <summary>
    /// 배터리 상호작용 처리
    /// -배터리는 1회만 제거 가능
    /// -루프매니저에 제거상태 전달
    /// </summary>
    /// <param name="interactor"></param>
    public override void Interact(GameObject interactor)
    {
        if (isRemoved)
        {
            Debug.Log("[배터리] 이미 제거됨");
            return;
        }

        isRemoved = true;
        //루프 매니저에 배터리 제거 상태 전달
        loopManager.SetBatteryRemoved(true);
        //배터리 비주얼 비활성화
        batteryVisual.SetActive(false);

        Debug.Log("[배터리] 제거 완료");
    }

    /// <summary>
    /// 배터리 상태 원상복구용(루프리셋될때)
    /// -시각적인 부분, 내부 상태 모두 복구
    /// </summary>
    public void RestoreBatteryState()
    {
        isRemoved = false;
        batteryVisual.SetActive(true);
    }
}
