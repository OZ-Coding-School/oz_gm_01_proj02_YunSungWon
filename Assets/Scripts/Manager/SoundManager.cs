using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 사운드 매니저
/// 클립이름으로 호출 가능하게,
/// 씬 하나여서 굳이 DDL은 필요없을거 같긴함
/// 시간상 급해서 믹서까진 안만들고 오디오소스로 관리
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("BGM 소스")]
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX 소스")]
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM 클립 목록")]
    [SerializeField] private AudioClip[] bgmClips;

    [Header("SFX 클립 목록")]
    [SerializeField] private AudioClip[] sfxClips;

    [Header("기본 볼륨")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float bgmVolume = 1.0f;

    [Range(0.0f, 1.0f)]
    [SerializeField] private float sfxVolume = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// 클립이름으로 BGM 재생->기존 BGM 교체식
    /// </summary>
    /// <param name="clipName"></param>
    public void PlayBgmByName(string clipName)
    {
        AudioClip clip = FindClipByName(bgmClips, clipName);
        if (clip == null)
        {
            return;
        }

        PlayBgm(clip);
    }

    /// <summary>
    /// BGM 재생처리
    /// </summary>
    /// <param name="clip"></param>
    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        //같은 곡이 이미 재생 중이면 재시작하지 않게 - 일단 테스트후 제거
        if (bgmSource.isPlaying && bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>
    /// BGM 정지처리
    /// </summary>
    public void StopBgm()
    {
        if (bgmSource == null) return;

        bgmSource.Stop();
        bgmSource.clip = null;
    }

    /// <summary>
    /// SFX 정지처리
    /// </summary>
    public void StopSfx()
    {
        if (sfxSource == null) return;

        sfxSource.Stop();
        sfxSource.clip = null;
    }

    /// <summary>
    /// 클립이름으로 SFX 1회 재생-겹쳐서 재생 가능한 식으로
    /// </summary>
    /// <param name="clipName"></param>
    public void PlaySfxByName(string clipName)
    {
        AudioClip clip = FindClipByName(sfxClips, clipName);
        if (clip == null)
        {
            return;
        }

        PlaySfx(clip);
    }

    /// <summary>
    /// SFX 1회 재생처리
    /// </summary>
    /// <param name="clip"></param>
    public void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>
    /// 배열에서 이름 일치하는 클립을 찾을 용도
    /// </summary>
    /// <param name="list"></param>
    /// <param name="clipName"></param>
    /// <returns></returns>
    private AudioClip FindClipByName(AudioClip[] list, string clipName)
    {
        if (list == null) return null;

        if (string.IsNullOrEmpty(clipName)) return null;

        int i = 0;
        while (i < list.Length)
        {
            AudioClip clip = list[i];
            if (clip != null && clip.name == clipName)
            {
                return clip;
            }
            i++;
        }

        return null;
    }
}
