using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 부엌 이탈시 트리거- 현재 카메라 전환만 있음
/// </summary>
public class Kitchen_Out_Trigger : MonoBehaviour
{
    [Header("부엌 이탈시 Vcam")]
    [SerializeField] private CinemachineVirtualCamera kitchenVcam;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            kitchenVcam.Priority = 0;
        }
    }
}
