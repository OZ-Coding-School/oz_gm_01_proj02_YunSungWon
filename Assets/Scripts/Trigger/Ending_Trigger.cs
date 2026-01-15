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
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        EndingDirector.Instance.OnPlayerReachedExit("복도 끝 엔딩트리거 도달");
    }
}
