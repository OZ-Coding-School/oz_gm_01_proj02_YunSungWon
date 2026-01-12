using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 정신약 상호작용
/// -InteractableBase 상속, 기존 상호작용 흐름은 유지
/// -복용시 일정시간 강제 현실상태 유지하는 목적
/// 
/// [주 목적]
/// -약을 먹고 30초 이내만 현실상태 핵심 룰-아직 테스트 버전
/// -루프마다 단 1회 사용제한/ 계속 복용가능하면 밸런스 붕괴니까
/// 
/// -약 상호작용은 여기서, 인지상태 규칙은 PerceptionManager가 담당하게 하고,
/// -IResetTable로 루프 시 약의 상태(1회성 소모품) 초기화
/// </summary>
public class MedicineInteractable : InteractableBase, IResetTable
{
    [Header("PerceptionManager 참조")]
    [SerializeField] private PerceptionManager perceptionManager;

    [Header("강제 현실 지속시간")]
    [SerializeField] private float realityDuration = 30.0f;

    [Header("매 루프마다 1회만 복용 가능(루프 시작시 복구용)")]
    [SerializeField] private bool oneUsePerLoop = true;

    [Header("디버그/연출 사유")]
    [SerializeField] private string reason = "";

    private bool usedThisLoop;

    private void OnEnable()
    {
        if (LoopManager.Instance != null) LoopManager.Instance.RegisterResetTable(this);
    }

    private void OnDisable()
    {
        if (LoopManager.Instance != null) LoopManager.Instance.UnRegisterResetTable(this);
    }

    public override void Interact(GameObject interactor)
    {
        if (oneUsePerLoop && usedThisLoop)
        {
            Debug.Log("[MedicineInteractable] 이번 루프에서 이미 복용함");
            return;
        }

        perceptionManager.ForceReality(realityDuration, reason);
        usedThisLoop = true;

        Debug.Log("[MedicineInteractable] 약 복용완료 : 강제현실 진행 " + realityDuration.ToString("F2") + "s");
    }

    public void ResetState()
    {
        usedThisLoop = false;
    }
}
