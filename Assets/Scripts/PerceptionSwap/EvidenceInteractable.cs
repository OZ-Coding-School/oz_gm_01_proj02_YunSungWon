using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현실 상태 누적치 증가시킬 상호작용 오브젝트 전용
/// -InteractableBase 상속, 기존 상호작용 흐름은 유지
/// 
/// [주 목적]
/// -단서 발견시 PerceptionManager의 현실 인지 누적치가 증가
/// -고유 evidenceId를 이용해서, 같은 단서 중복처리 되는거 방지해야함
/// 
/// </summary>
public class EvidenceInteractable : InteractableBase
{
    [Header("PerceptionManager 참조")]
    [SerializeField] private PerceptionManager perceptionManager;

    [Header("단서 고유 ID 부여")]
    [SerializeField] private string evidenceId = "";

    [Header("단서 발견시 현실인지 누적치 증가량(0~1)")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float meterGain = 0.1f;

    [Header("발견 사유")]
    [SerializeField] private string reason = "";

    public override void Interact(GameObject interactor)
    {
        if (string.IsNullOrWhiteSpace(evidenceId))
        {
            Debug.Log("[EvidenceInteractable] evidenceId가 null, 고유 id 지정필요");
            return;
        }

        if (perceptionManager == null)
        {
            perceptionManager = PerceptionManager.Instance;
        }

        bool applied = perceptionManager.RegisterEvience(evidenceId, meterGain, reason);
        if (applied)
        {
            Debug.Log("[EvidenceInteractable] 단서 반영 완료 : " + evidenceId);
        }
        else
        {
            Debug.Log("[EvidenceInteractable] 단서 반영 실패(중복가능성 있음) : " + evidenceId);
        } 
    }
}
