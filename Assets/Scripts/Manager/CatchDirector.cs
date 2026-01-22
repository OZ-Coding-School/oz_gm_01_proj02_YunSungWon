using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 잡혔을때 몸싸움 연출용 디렉터
/// -기존 루프에서는 플레이어 클릭무브 잠그고,
/// -라스트 페이즈에서는 플레이어 컨트롤 잠근 상태로
/// -컷씬 연출 나오는 조건(잡힘결과) 반환용
/// </summary>
public class CatchDirector : MonoBehaviour
{
    [Header("플레이어 Animator")]
    [SerializeField] private Animator playerAnimator;

    [Header("플레이어 애니 트리거 이름")]
    [SerializeField] private string playerCaughtTriggerName = "Caught";

    [Header("에너미 애니 트리거 이름")]
    [SerializeField] private string enemyCatchTriggerName = "Catch";

    [Header("탑뷰 입력 컨트롤")]
    [SerializeField] private ClickMove clickMove;

    [Header("FPS 입력 컨트롤")]
    [SerializeField] private PlayerControl playerControl;

    //잡힘 처리 시 롤백 호출용 참조
    //이거 지금 라스트 페이즈에너미 컨트롤쪽에서 임시로 킬 처리 하고 있는거
    //제발 제발 잊지마 -리셋 루프도 마찬가지로 루프매니저->에너미 컨트롤쪽 리셋루프
    [Header("루프리셋, 롤백 담당 참조")]
    [SerializeField] private LoopManager loopManager;
    [SerializeField] private EndingDirector endingDirector;

    [Header("연출 파라미터")]
    [Header("잡히는 거리")]
    [SerializeField] private float snapDistance = 0.9f;
    [Header("잡힘 애니 보여줄 시간")]
    [SerializeField] private float holdSeconds = 1.2f;

    [Header("중복 실행 방지")]
    [SerializeField] private bool blockIfBusy = true;

    [Header("잡혔을때 연출용 볼룸FX 참조")]
    [SerializeField] private CatchGlitchVolumeFX volumeFX;

    private bool isRunning;
    private int playerCaughtHash;
    private int enemyCatchHash;

    private void Awake()
    {
        playerCaughtHash = Animator.StringToHash(playerCaughtTriggerName);
        enemyCatchHash = Animator.StringToHash(enemyCatchTriggerName);
    }

    /// <summary>
    /// EnemyCatchTrigger에서 호출되는 진입점
    /// </summary>
    public void BeginCatch(EnemyCatchInfo enemyInfo, GameObject player)
    {
        if (enemyInfo == null || player == null) return;
        if (blockIfBusy && isRunning) return;

        StartCoroutine(CatchSequence(enemyInfo, player));
    }

    private IEnumerator CatchSequence(EnemyCatchInfo enemyInfo, GameObject player)
    {
        isRunning = true;

        //현재 괴한 타입에 따라 플레이어 입력 잠금
        LockInput(enemyInfo.Type);

        //연출용 위치 스냅
        SnapEnemyToPlayer(enemyInfo.transform, player.transform);

        //볼룸FX 연출 시작
        volumeFX.PlayStart();

        //애니메이션 재생
        PlayCatchAnimations(enemyInfo);

        //잠시 유지 (카메라/사운드 연출 구간) ====!!====
        yield return new WaitForSeconds(holdSeconds);

        //결과 분기점
        if (enemyInfo.Type == EnemyCatchInfo.CatchType.Loop)
        {
            loopManager.ResetLoop("[CatchDirector] 기존루프 괴한에게 잡힘 -> 리셋 루프");
        }
        else
        {
            endingDirector.RollbackTocheckPoint("[CatchDirector] 라스트페이즈 괴한에게 잡힙 -> 롤백 루프");
        }
        
        //볼룸 FX 연출 끝
        volumeFX.PlayEnd();

        //플레이어 입력 복구
        UnlockInput(enemyInfo.Type);

        isRunning = false;
    }

    /// <summary>
    /// 괴한타입별 플레이어 입력 실제 잠금 처리
    /// </summary>
    private void LockInput(EnemyCatchInfo.CatchType type)
    {
        if (type == EnemyCatchInfo.CatchType.Loop)
        {
            if (clickMove != null) clickMove.enabled = false;
        }
        else
        {
            if (playerControl != null) playerControl.enabled = false;
        }
    }


    /// <summary>
    /// 괴한타입별 플레이어 입력 실제 복구 처리
    /// </summary>
    /// <param name="type"></param>
    private void UnlockInput(EnemyCatchInfo.CatchType type)
    {
        if (type == EnemyCatchInfo.CatchType.Loop)
        {
            if (clickMove != null) clickMove.enabled = true;
        }
        else
        {
            if (playerControl != null) playerControl.enabled = true;
        }
    }

    /// <summary>
    /// 괴한을 플레이어 정면으로 이동시켜서 컷씬 연출 포즈 연출
    /// </summary>
    private void SnapEnemyToPlayer(Transform enemy, Transform player)
    {
        Vector3 forward = player.forward;
        Vector3 targetPosition = player.position + forward * snapDistance;
        targetPosition.y = enemy.position.y;

        enemy.position = targetPosition;

        Vector3 lookPosition = player.position;
        lookPosition.y = enemy.position.y;
        enemy.LookAt(lookPosition);
    }

    /// <summary>
    /// 잡힘 애니메이션 트리거 실행
    /// </summary>
    private void PlayCatchAnimations(EnemyCatchInfo enemyInfo)
    {
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(playerCaughtHash);
            playerAnimator.SetTrigger(playerCaughtHash);
        }

        Animator enemyAnimator = enemyInfo.EnemyAnimator;

        if (enemyAnimator != null)
        {
            enemyAnimator.ResetTrigger(enemyCatchHash);
            enemyAnimator.SetTrigger(enemyCatchHash);
        }
    }
}
