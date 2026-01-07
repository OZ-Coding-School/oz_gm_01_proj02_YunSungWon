using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화장실 구역에 들어오면 플레이어를 스텔스 상태로 전환하고,
/// 구역에서 나가면 스텔스 상태 해제
/// 
/// [구현목표]
/// -현재 단계에서는 일단, 문 잠금 닫힘 여부 x
/// -구역안에 있으면 스텔스 상태로 인정
/// -추후 문 잠금 퍼즐을 넣을때, 여기서 조건 추가하는식으로 
/// </summary>
public class BathRoomHideZone : MonoBehaviour
{
    [Header("스텔스 상태를 적용-playerStealth")]
    [SerializeField] private PlayerStealth playerStealth;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(playerStealth != null) playerStealth.SetHidden(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerStealth != null) playerStealth.SetHidden(false);
        }
    }
}
