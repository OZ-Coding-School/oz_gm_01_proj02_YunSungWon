using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// --트러블 슈팅--
/// 씬에 공간자체로 넣어버리니까, 계속 숨은상태로 유지됨(변경불가)
/// 들어오는 공간 트리거, 나가는 공간 트리거 따로 분리 해서 테스트
public class BathRoomHideZoneOut : MonoBehaviour
{
    [Header("스텔스 상태를 적용-playerStealth")]
    [SerializeField] private PlayerStealth playerStealth;

    [Header("화장실 문-거실쪽 콜라이더 활성화")]
    [SerializeField] private GameObject clickColliderOUT;

    [Header("화장실 이탈시 Vcam")]
    [SerializeField] private CinemachineVirtualCamera bathRoomVcam;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerStealth != null) playerStealth.SetHidden(false);
            clickColliderOUT.SetActive(true);
            bathRoomVcam.Priority = 0;
        }
    }
}
