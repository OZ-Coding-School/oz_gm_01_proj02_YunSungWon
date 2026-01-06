using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 개발 중 시스템 상태(시간/루프/플래그)를 화면에 표시.
/// LoopManager 값을 읽어서 텍스트로 변환
/// 포트폴리오/시연에서 기술적으로 설계된 시스템 보여주기 위함
/// </summary>
public class DebugHud : MonoBehaviour
{
    [Header("루프매니저 참조")]
    [SerializeField] private LoopManager loopManager;

    [Header("텍스트 표시 공간")]
    [SerializeField] private TextMeshProUGUI text;

    private void Update()
    {
        if (loopManager == null || text == null) return;

        string message =
            "현재 루프 회차: " + loopManager.LoopCount + "\n" +
            "시간: " + loopManager.ElapsedSeconds.ToString("F2") + " / " + loopManager.BreakInSeconds.ToString("F2") + "\n" +
            "침입 트리거: " + loopManager.BreakInTriggered;
        text.text = message;
    }
}
