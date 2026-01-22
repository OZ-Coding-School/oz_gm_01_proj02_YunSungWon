using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 글로벌 볼룸 배치되있는거 에서, 글리치 연출 써먹을거
/// 잡힘 판정에서 특정 오버라이드 요소만 값 변경
/// -평상시에는 비네팅,필름그레인 비활성화 상태로 시작하고?
/// -크로마틱은 값을 0으로 해놓고, 활성화 해놓고?
/// ->연출 시작됐다? ->비활성화 되있는거 활성화로 변경하고
/// ->필름 그레인이랑 크로마틱의 값을 0에서1로 이동시키고,
/// 연출 끝나는 시점엔 그 반대로?
/// </summary>
public class CatchGlitchVolumeFX : MonoBehaviour
{
    [Header("메인 글로벌 볼룸")]
    [SerializeField] private Volume volume;

    [Header("연출 시간 업&다운")]
    [SerializeField] private float rampUpSecond = 1.0f;
    [SerializeField] private float rampDownSecond = 1.0f;

    [Header("비네트 램프 목표값")]
    [SerializeField] private float vignetteRampPeak = 0.5f;

    [Header("필름 그레인 램프 목표값")]
    [SerializeField] private float filmGrainRampPeak = 1.0f;

    [Header("크로마틱 램프 목표값")]
    [SerializeField] private float chromaticRampPeak = 1.0f;

    private Vignette vignette;
    private FilmGrain filmGrain;
    private ChromaticAberration chromatic;

    private Coroutine routine;

    private void Awake()
    {
        if (volume == null) volume = GetComponent<Volume>();

        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out filmGrain);
        volume.profile.TryGet(out chromatic);
    }

    /// <summary>
    /// 잡힘 연출 시작->활성화후 ramp Up 처리
    /// </summary>
    public void PlayStart()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        //각 오버라이드 전부 intensity 0으로 시작
        vignette.intensity.value = 0.0f;
        filmGrain.intensity.value = 0.0f;
        chromatic.intensity.value = 0.0f;

        //실제 값변경 처리
        routine = StartCoroutine(RampUpCo());
    }

    /// <summary>
    /// 잡힘 연출 종료 ramp Down후에 비활성화 처리
    /// </summary>
    public void PlayEnd()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        routine = StartCoroutine(RampDownCo());
    }

    private IEnumerator RampUpCo()
    {
        float time = 0.0f;

        while (time < rampUpSecond)
        {
            time += Time.deltaTime;
            float a = (rampUpSecond <= 0.0f) ? 1.0f : Mathf.Clamp01(time / rampUpSecond);

            //vignette intensity 0에서 목표 값으로
            vignette.intensity.value = Mathf.Lerp(0.0f, vignetteRampPeak, a);
            //FilmGrain intensity 0에서 목표값으로
            filmGrain.intensity.value = Mathf.Lerp(0.0f, filmGrainRampPeak, a);
            // Chromatic intensity 0에서 목표값으로
            chromatic.intensity.value = Mathf.Lerp(0.0f, chromaticRampPeak, a);

            yield return null;
        }

        vignette.intensity.value = vignetteRampPeak;
        filmGrain.intensity.value = filmGrainRampPeak;
        chromatic.intensity.value = chromaticRampPeak;

        routine = null;
    }

    private IEnumerator RampDownCo()
    {
        float time = 0.0f;

        float CurVignetteRamp = vignette.intensity.value; 
        float CurGrainRamp = filmGrain.intensity.value;
        float CurChromaRamp = chromatic.intensity.value;

        while (time < rampDownSecond)
        {
            time += Time.deltaTime;
            float a = (rampDownSecond <= 0.0f) ? 1.0f : Mathf.Clamp01(time / rampDownSecond);

            //각 오버라이드 요소 현재값에서 0으로
            vignette.intensity.value = Mathf.Lerp(CurVignetteRamp, 0.0f, a);
            filmGrain.intensity.value = Mathf.Lerp(CurGrainRamp, 0.0f, a);
            chromatic.intensity.value = Mathf.Lerp(CurChromaRamp, 0.0f, a);

            yield return null;
        }
        routine = null;
    }
}
