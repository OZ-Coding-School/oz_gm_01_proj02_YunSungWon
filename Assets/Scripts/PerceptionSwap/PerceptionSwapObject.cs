using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.Timeline.Actions;
using UnityEngine;

/// <summary>
/// 환각/ 현실 상태에 따라 두 비주얼을 스왑하는 용도
/// 
/// 환각 상태의 규칙은 PerceptionManager에서, 시각표현은 여기서,
/// -상태변경은 옵저버 이벤트 구독상태로 확인
/// </summary>
public class PerceptionSwapObject : MonoBehaviour
{
    [Header("PerceptionManager 참조")]
    [SerializeField] private PerceptionManager perceptionManager;

    [Header("환각 상태에서 보일 비주얼(평소)")]
    [SerializeField] private GameObject hallucinationVisual;

    [Header("현실 상태에서 보일 비주얼")]
    [SerializeField] private GameObject realityVisual;

    private void OnEnable()
    {
        perceptionManager = PerceptionManager.Instance;
        perceptionManager.StateChanged += OnStateChanged;

        //Enable 시 현재상태로 즉시 동기화
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

    //상태에 맞게 비주얼 활성화/비활성화
    private void Apply(PerceptionManager.PerceptionState state)
    {
        bool showReality = (state == PerceptionManager.PerceptionState.Reality);

        if (hallucinationVisual != null) hallucinationVisual.SetActive(!showReality);
        if (realityVisual != null) realityVisual.SetActive(showReality);
    }
}
