using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 애니메이션 이벤트로 사운드 호출하려고 하는데,
/// 사운드 매니저 쪽 바로 호출 불가하고,
/// 해당 오브젝트 컴포넌트로 들어가 있는거만 호출 가능해서
/// 사운드 매니저를 중계해서 가져올 컴포넌트
/// </summary>
public class AnimSfxBridge : MonoBehaviour
{
    public void PlaySfxByName(string clipName)
    {
        if (SoundManager.Instance == null) return;
        SoundManager.Instance.PlaySfxByName(clipName);
    }
}
