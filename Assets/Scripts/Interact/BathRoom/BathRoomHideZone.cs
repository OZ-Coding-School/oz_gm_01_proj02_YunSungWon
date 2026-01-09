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
/// 
/// --트러블 슈팅--
/// 씬에 공간자체로 넣어버리니까, 계속 숨은상태로 유지됨(변경불가)
/// 들어오는 공간 트리거, 나가는 공간 트리거 따로 분리 해서 테스트
/// </summary>
public class BathRoomHideZone : MonoBehaviour
{
    [Header("스텔스 상태를 적용-playerStealth")]
    [SerializeField] private PlayerStealth playerStealth;

    [Header("화장실 문-거실쪽 콜라이더 비활성화")]
    [SerializeField] private GameObject clickColliderOUT;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(playerStealth != null) playerStealth.SetHidden(true);
            clickColliderOUT.SetActive(false);
        }
    }
}
