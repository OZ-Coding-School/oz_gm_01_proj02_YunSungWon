using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 인지 상태에 따라 핸드폰 UI 스왑
/// 환각 상태 : 키패드의 이상한 글씨 또는 기호
/// 현실 상태 : 정상 숫자 키패드 
/// 
/// PerceptionManager 이벤트 구독
/// -현실인지 상태에서만 정상적인 신고 가능
/// </summary>
public class PerceptionUI : MonoBehaviour
{
    [Header("PerceptionManager 참조")]
    [SerializeField] private PerceptionManager perceptionManager;

    [Header("환각상태 UI 패널")]
    [SerializeField] private GameObject hallucinationPanel;

    [Header("현실상태 UI 패널")]
    [SerializeField] private GameObject realityPanel;

    public bool IsRealityReadable { get { return perceptionManager.CurState == PerceptionManager.PerceptionState.Reality; } }

    private void OnEnable()
    {
        perceptionManager = PerceptionManager.Instance;
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
        bool isReality = (state == PerceptionManager.PerceptionState.Reality);

        hallucinationPanel.SetActive(!isReality);
        realityPanel.SetActive(isReality);
    }
}
