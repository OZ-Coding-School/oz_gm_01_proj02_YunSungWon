using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        //오프닝 중 입력잠금->마우스 커서만 숨기는걸로 변경
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

        //입력잠금 해제->마우스 커서만 해제로 변경
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
        //여기에 클릭무브,플레이어 컨트롤 넣어서 잠궈버리니까,
        //루프매니저에서 처음 시작할때,턉뷰모드로 들어가고, 컨트롤 토글할때,
        //거기서 토글하는거랑 맞부딪혀서 컨트롤 그냥 터져버림
        //연출 중일때는 플레이어 캐릭터 아예 안보일거니까,
        //그냥 여기서 관련 컨트롤 잠그는건 빼기로

        //연출중이니까 마우스 커서는 숨기는쪽으로
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
