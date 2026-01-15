using Cinemachine;
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
/// 
/// +해당 화장실 진입시 트리거에 거실방향 문 콜라이더 비활성화 넣어놨는데,
/// 문제는 진입상태에서 루프되면, 문 콜라이더를 다시 복구할 방안이 없음.
/// 스텔스 상태도 마찬가지긴 한데, 이건 문 상태에 의한 상태로 바꿀거니까-일단 보류
/// 일단 이것도 리셋테이블로 지정해놓고, 리셋시 문 콜라이더 복구하는걸로
/// </summary>
public class BathRoomHideZone : MonoBehaviour , IResetTable
{
    [Header("스텔스 상태를 적용-playerStealth")]
    [SerializeField] private PlayerStealth playerStealth;

    [Header("화장실 문-거실쪽 콜라이더 비활성화")]
    [SerializeField] private GameObject clickColliderOUT;

    [Header("화장실 진입시 Vcam")]
    [SerializeField] private CinemachineVirtualCamera bathRoomVcam;

    private void OnEnable()
    {
        LoopManager.Instance.RegisterResetTable(this);
    }

    private void OnDisable()
    {
        LoopManager.Instance.UnRegisterResetTable(this);
    }

    public void ResetState()
    {
        clickColliderOUT.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(playerStealth != null) playerStealth.SetHidden(true);
            clickColliderOUT.SetActive(false);
            bathRoomVcam.Priority = 20;
        }
    }
}
