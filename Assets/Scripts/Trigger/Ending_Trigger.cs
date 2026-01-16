using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [라스트 페이즈에서 엔딩 트리거]
/// 라스트 페이즈 일때만 성공처리
/// 어차피 Exit Trigger에 막혀있는 구조라서
/// 딱히 방어는 안해도 될듯, 그냥 닿으면 트리거 발생
/// </summary>
public class Ending_Trigger : MonoBehaviour
{
    [Header("엔딩 디렉터")]
    [SerializeField] private EndingDirector director;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("[Ending_Trigger]엔딩 트리거 자체는 들어옴");


        if (director == null)
        {
            Debug.Log("[Ending_Trigger] 디렉터 null 났음");
        }

        director.OnPlayerReachedExit("복도 끝 엔딩트리거 도달");

        Debug.Log("[Ending_Trigger]엔딩 트리거 OnPlayerReachedExit 지나침");
    }
}
