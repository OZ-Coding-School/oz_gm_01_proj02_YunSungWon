using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 개발 중 시스템 상태(시간/루프/플래그)를 화면에 표시.
/// LoopManager 값을 읽어서 텍스트로 변환
/// 포트폴리오/시연에서 기술적으로 설계된 시스템 보여주기 위함
/// 
/// [표시항목]
/// -루프회차, 경과시간
/// -1차 침입 예정시간(도어락)
/// -배터리 제거 여부
/// -2차 침입(비상키) 예정시간
/// -침입 진행단계(BreakInPhase)
/// </summary>
public class DebugHud : MonoBehaviour
{
    [Header("루프매니저 참조")]
    [SerializeField] private LoopManager loopManager;

    [Header("텍스트 표시 공간")]
    [SerializeField] private TextMeshProUGUI text;

    private void Update()
    {
        //++비상키 시간이 아직 설정되지 않은경우엔, "-"로 임시표시
        string emergencyText = "-";
        if (loopManager.EmergencyKeySeconds >= 0.0f)
        {
            emergencyText = loopManager.EmergencyKeySeconds.ToString("F2");
        }

        if (loopManager == null || text == null) return;

        string message =
            "현재 루프 회차: " + loopManager.LoopCount + "\n" +
            "시간: " + loopManager.ElapsedSeconds.ToString("F2") + "\n" +
            "1번째 침입 시간: " + loopManager.BreakInSeconds.ToString("F2") + "\n" +
            "배터리 제거 여부: " + loopManager.BatteryRemoved + "\n" +
            "비상키 침입 시간: " + emergencyText + "\n" +
            "침입 단계: " + loopManager.CurBreakInPhase;
        text.text = message;
    }
}
