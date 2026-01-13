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
/// 
/// ++여기서 인터랙트는 UI 띄우는걸로 대체,
/// 실제 인터랙트는 해당 UI에서 작동하게 변경
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

    [Header("열 UI 패널(약통 패널)")]
    [SerializeField] private MedicineUIPanel panel;

    [Header("이번 루프에 이미 복용했는지")]
    [SerializeField] private bool usedThisLoop;

    //UI에서 읽기 위한 현재 복용 가능 여부
    public bool CanUse
    {
        get 
        { 
            if (!oneUsePerLoop)
            {
                return true;
            }  
            return !usedThisLoop;
        } 
    }

    private void OnEnable()
    {
        if (LoopManager.Instance != null) LoopManager.Instance.RegisterResetTable(this);
    }

    private void OnDisable()
    {
        if (LoopManager.Instance != null) LoopManager.Instance.UnRegisterResetTable(this);
    }

    //원래는 바로 효과적용
    //->관련 컨텐스트 UI만 활성화 
    public override void Interact(GameObject interactor)
    {
        InteractContext context = new InteractContext(interactor, gameObject, "약통 UI 오픈");
        UIManager.Instance.OpenPanel(panel, context);
    }

    /// <summary>
    /// UI버튼에서 호출할 실제 복용처리
    /// </summary>
    /// <returns></returns>
    public bool TryUse()
    {
        if (!CanUse) return false;

        if (perceptionManager == null) perceptionManager = PerceptionManager.Instance;
        perceptionManager.ForceReality(realityDuration, reason);

        if(oneUsePerLoop) usedThisLoop = true;
        return true;
    }

    public void ResetState()
    {
        usedThisLoop = false;
    }
}
