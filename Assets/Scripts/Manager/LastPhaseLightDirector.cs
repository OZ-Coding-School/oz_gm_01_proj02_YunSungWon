using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


/// <summary>
/// 라스트 페이즈 진입시, 라이트 전환
/// -각 구역 스팟라이트 꺼버리고, 레드라이트는 키고,
/// 글로벌 볼룸의 color Adjustments(post Exposure)을 -5로 설정
/// -라스트 페이즈 시작 기점은 엔딩디렉터에서 알려줄거고,
/// 실제 조명 바꾸는건 여기서
/// </summary>
public class LastPhaseLightDirector : MonoBehaviour
{
    [Header("라스트 페이즈 진입시 off 할 스팟 라이트들")]
    [SerializeField] private List<Light> offTheSpotLight = new List<Light>();

    [Header("라스트 페이즈 진입시 on 할 레드 라이트")] //일단 한개만 쓸건데, 혹시몰라서 리스트로
    [SerializeField] private Light OnTheRedLight;

    [Header("Global Volume 참조")]
    [SerializeField] private Volume globalVolume;

    [Header("라스트 페이즈 Exposure노출값 설정")]
    [SerializeField] private float lastPhasePostExposure = -5.0f;

    //Volume에서 가져올 ColorAdjustments 오버라이드
    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        CacheVolumeOverride();
    }

    /// <summary>
    /// Volume.profile에서 ColorAdjustments 오버라이드를 찾아서 캐싱
    /// </summary>
    private void CacheVolumeOverride()
    {
        //ColorAdjustments 오버라이드 꼭 해당 프로필에 있어야 함
        bool found = globalVolume.profile.TryGet(out colorAdjustments);
    }

    /// <summary>
    /// 라스트 페이즈 진입시 조명 연출 적용-엔딩디렉터에서 호출할 것
    /// </summary>
    public void ApplyLastPhaseLighting(string reason)
    {
        //스팟 라이트 Off
        SetLightsEnabled(offTheSpotLight, false);

        //레드 라이트 On
        OnTheRedLight.gameObject.SetActive(true);

        //Post Exposure 변경
        SetPostExposure(lastPhasePostExposure);
    }

    private void SetLightsEnabled(List<Light> lights, bool enabled)
    {
        if (lights == null) return;

        int i = 0;
        while (i < lights.Count)
        {
            Light lightItem = lights[i];
            if (lightItem != null)
            {
                lightItem.enabled = enabled;
            }
            i++;
        }
    }

    /// <summary>
    /// Global Volume의 Color Adjustments(Post Exposure) 값 설정
    /// </summary>
    private void SetPostExposure(float value)
    {
        //Override 활성화
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = value;
    }
}
