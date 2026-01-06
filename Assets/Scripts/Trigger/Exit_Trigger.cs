using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현관문을 나가는 순간 루프 리셋 발동 트리거
/// 플레이어가 트리거에 닿으면, LoopManager.ResetLoop 호출
/// </summary>
public class Exit_Trigger : MonoBehaviour
{
    [Header("루프 매니저 참조")]
    [SerializeField] private LoopManager loopManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            loopManager.ResetLoop("현관문 트리거에 닿음");
        }
    }
}
