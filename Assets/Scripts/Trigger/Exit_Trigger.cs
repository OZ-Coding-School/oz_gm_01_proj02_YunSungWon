using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현관문을 나가는 순간 루프 리셋 발동 트리거
/// 플레이어가 트리거에 닿으면, LoopManager.ResetLoop 호출
/// 
/// +엔딩 관련, 평소에는 닿으면 리셋루프,
/// 엔딩 진행중 상태에서는 탈출성공처리로 연출 넘어갈수 있게
/// 
/// +- 엔딩관련 ExitTrigger는 그냥 평소대로
/// 엔딩트리거를 복도쪽에 따로 두고, 라스트페이즈 진입하면,
/// 여기 엑시트 트리거는 꺼버릴거임
/// 
/// </summary>
public class Exit_Trigger : MonoBehaviour
{
    [Header("루프 매니저 참조")]
    [SerializeField] private LoopManager loopManager;

    private void Awake()
    {
        //평상시
        if (loopManager == null)
        {
            loopManager = LoopManager.Instance;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        //혹시 실수로 라스트페이즈때 리셋 일어나지 않게
        //여기에 방어 추가
        if (EndingDirector.Instance != null && EndingDirector.Instance.IsEnding)
        {
            return;
        }

        if (loopManager != null)
        {
            loopManager.ResetLoop("엔딩상태 아님-현관문 트리거에 닿음");
        }
    }
}
