using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 은신(숨기) 상태 관리
/// 화장실 트리거나 특정 상호작용이 이루어지면, 숨은 상태/해제 상태를 변경할 수 있게
/// 
/// [구현목표]
/// 은신 로직인데, 기존설계는 화장실에만 숨을 수 있게(문이 화장실만 있음,부엌x),
/// 관련 은식 로직에 의한 책임을 컨트롤 역할의 스크립트와 분리하기 위함
/// -추후 앉기/숨는 연출,소리 기반 괴한반응(이건 진짜 마지막 연출) 확장 염두할 것
/// </summary>
public class PlayerStealth : MonoBehaviour
{
    [Header("현재 숨은 상태인지")]
    [SerializeField] private bool isHidden;

    //외부 접근용 프로퍼티
    public bool IsHidden { get { return isHidden; } }

    /// <summary>
    /// 숨기 상태는 외부에서 설정
    /// </summary>
    /// <param name="value"></param>
    public void SetHidden(bool value)
    {
        if (isHidden == value) return;
        isHidden = value;
        Debug.Log("[PlayerStealth] isHidden 변경됨 -> " + isHidden);
    }
}
