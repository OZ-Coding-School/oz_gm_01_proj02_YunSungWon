using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엔딩문만 사운드 하나 더 추가 하고 싶은데,
/// 도어파티쪽이랑 같은 컴포넌트 사용중이라,
/// 급하게 하나 트리거 따로 팜
/// </summary>
public class EndDoorSoundTrigger : MonoBehaviour
{ 
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        SoundManager.Instance.PlaySfxByName("EndDoorOpen");
    }
}
