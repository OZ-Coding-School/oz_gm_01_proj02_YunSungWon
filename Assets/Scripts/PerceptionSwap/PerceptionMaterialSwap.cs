using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 환각,현실 상태에 따라 렌더러의 마테리얼을 교체, 표현하기 위한 컴포넌트
/// 
/// 일단 거울에 쓸 용도로 만드는건데, 진짜 반사는 일단 미뤄두고,
/// 재질만 바꿔서 상태 확인 장치 용도로 사용할 수 있게-테스트용
/// </summary>
public class PerceptionMaterialSwap : MonoBehaviour
{
    [Header("PerceptionManager 참조")]
    [SerializeField] private PerceptionManager perceptionManager;

    [Header("재질 바꿀 렌더러")]
    [SerializeField] private Renderer targetRenderer;

    [Header("환각 상태의 재질(평소)")]
    [SerializeField] private Material hallucinationMaterial;

    [Header("현실 상태의 재질")]
    [SerializeField] private Material realityMaterial;

    [Header("공유마테리얼로 변경할지 여부-임시")]
    [SerializeField] private bool useSharedMaterial = false;

    private void OnEnable()
    {
        perceptionManager = PerceptionManager.Instance;
        targetRenderer = GetComponent<Renderer>();
        perceptionManager.StateChanged += OnStateChanged;
        Apply(perceptionManager.CurState);
    }

    private void OnDisable()
    {
        perceptionManager.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged(PerceptionManager.PerceptionState oldState, PerceptionManager.PerceptionState newState, string reason)
    {
        Apply(newState);
    }

    private void Apply(PerceptionManager.PerceptionState state)
    {
        if (targetRenderer == null) return;
        Material material = (state == PerceptionManager.PerceptionState.Reality) ? realityMaterial : hallucinationMaterial;
        if (material == null) return;

        if(useSharedMaterial) targetRenderer.sharedMaterial = material;
        else targetRenderer.material = material;
    }
}
