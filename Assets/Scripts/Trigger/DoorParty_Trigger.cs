using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 트리거에 닿으면,DoorParty_View 에서 ApplyOpenYaw호출
/// 각 트리거는 맞는 DoorParty_View 참조할 것
/// </summary>
public class DoorParty_Trigger : MonoBehaviour
{
    [Header("DoorParty_View 각 문에 있는거 참조할 것")]
    [SerializeField] DoorParty_View doorParty_View;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        doorParty_View.ApplyOpenYaw();
    }
}
