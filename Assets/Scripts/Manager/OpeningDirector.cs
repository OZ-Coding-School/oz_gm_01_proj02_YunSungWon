using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;


/// <summary>
/// 게임 시작 오프닝 타임라인 재생
/// -오프닝 타임라인 컷씬 종료시 루프매니저에게 게임시작 타이밍을 넘겨줄 수 있게
/// </summary>
public class OpeningDirector : MonoBehaviour
{
    [Header("루프 매니저 참조")]
    [SerializeField] private LoopManager loopManager;

    [Header("오프닝 타임라인")]
    [SerializeField] private PlayableDirector openingPlayableDirector;

    [Header("플레이어 입력 잠금용 인풋게이트 참조")]
    [SerializeField] PlayerInputGate inputGate;

    private bool isOpeningPlaying;

    private void OnEnable()
    {
        if (openingPlayableDirector != null)
        {
            openingPlayableDirector.stopped += OnOpeningStopped;
        }
    }

    private void OnDisable()
    {
        if (openingPlayableDirector != null)
        {
            openingPlayableDirector.stopped -= OnOpeningStopped;
        }
    }

    private void Start()
    {
        if (loopManager == null) loopManager = LoopManager.Instance;

        if (loopManager != null)
        {
            loopManager.PrepareForOpening("오프닝 연출중에 루프시스템 정지");
        }
        PlayOpening("게임 시작됨");
    }

    /// <summary>
    /// 오프닝 타임라인 재생 시작
    /// </summary>
    /// <param name="reason"></param>
    private void PlayOpening(string reason)
    {
        if (openingPlayableDirector == null)
        {
            Debug.Log("[OpeningDirector] openingPlayableDirector 가 null 상태-체크요망");
            StartLoopImmediately("오프닝 타임라인 없이 진행");
            return;
        }

        isOpeningPlaying = true;

        Debug.Log("[OpeningDirector] 오프닝 타임라인 시작됨 " + reason);

        //오프닝 중 입력잠금
        SetControlLocked(true);

        //플레이어블 디렉터 인스펙터에서 어웨이크 자동 재생 하거나, 여기서 코드로 제어
        openingPlayableDirector.time = 0.0f;
        openingPlayableDirector.Play();
    }

    private void OnOpeningStopped(PlayableDirector director)
    {
        if (!isOpeningPlaying) return;

        Debug.Log("[OpeningDirector] 오프닝 종료됨");

        isOpeningPlaying = false;

        //입력잠금 해제
        SetControlLocked(false);

        StartLoopImmediately("오프닝 종료 게임루프 시작");
    }

    private void StartLoopImmediately(string reason)
    {
        if (loopManager == null) loopManager = LoopManager.Instance;
        if (loopManager != null)
        {
            loopManager.BeginGameFromOpening(reason);
        }
        else
        {
            Debug.Log("[OpeningDirector] loopManager가 null 상태, 루프시작 실패");
        }
    }

    private void SetControlLocked(bool locked)
    {
        if (locked)
        {
            inputGate.SetGameplayInputEnabeld(false);
        }
        else
        {
            inputGate.SetGameplayInputEnabeld(true);
        }
    }
}
