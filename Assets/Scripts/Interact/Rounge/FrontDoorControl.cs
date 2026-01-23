using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BathRoom_DoorControl;

public class FrontDoorControl : MonoBehaviour,IResetTable
{
    //문 상태 정의
    public enum FrontDoorState
    {
        Open = 0,
        Closed = 1,
        Locked = 2,
        Broken = 3
    }

    //문 상태 전환 모드 정의(연출모드/리셋시 제외모드)
    public enum FrontDoorTransitionMode
    {
        Animated = 0,
        Instant = 1
    }

    [Header("초기 문 상태")]
    [SerializeField] private FrontDoorState initialState = FrontDoorState.Closed;

    [Header("현재 문 상태")]
    [SerializeField] private FrontDoorState doorState = FrontDoorState.Closed;

    //문 상태 변경 이벤트(연출 훅)
    //연출 생기면 여기에 구독해서 처리
    //<old->new 변화 방향, 변경된 이유>
    //트랜지션모드가 인스턴트면(리셋용), 연출없이 즉시 회전값 고정
    public event Action<FrontDoorState, FrontDoorState, string, FrontDoorTransitionMode> DoorStateChanged;

    //외부 접근용 프로퍼티
    public FrontDoorState CurState { get { return doorState; } }

    private void Awake()
    {
        doorState = initialState;
    }

    private void OnEnable()
    {
        if (LoopManager.Instance != null) LoopManager.Instance.RegisterResetTable(this);
    }

    private void OnDisable()
    {
        if (LoopManager.Instance != null) LoopManager.Instance.UnRegisterResetTable(this);
    }

    public void ResetState()
    {
        SetState(initialState, "리셋상태: 문상태 = Closed", FrontDoorTransitionMode.Instant);
    }

    #region 플레이어 상호작용
    //=================================================================플레이어 상호작용=====//
    /// <summary>
    /// [문 자체 클릭] 열기/닫기 토글
    /// Locked 상태에선 열리지 않게
    /// </summary>
    public void InteractDoor()
    {
        //부숴진 문은 열린걸로 간주, 및 상호작용 불가
        if (doorState == FrontDoorState.Broken)
        {
            Debug.Log("[BathRommDoor] 문 클릭 : Broken 상태(열려있는 취급/상호작용 불가)");
            return;
        }

        //잠긴문은 문클릭으로 열기 불가, 상호작용 불가-연출 추가 예정
        if (doorState == FrontDoorState.Locked)
        {
            Debug.Log("[BathRommDoor] 문 클릭 : Locked 상태(잠겨있는 취급/상호작용 불가)");
            return;
        }

        if (doorState == FrontDoorState.Open)
        {
            SetState(FrontDoorState.Closed, "문 클릭 : Open->Closed", FrontDoorTransitionMode.Animated);
            return;
        }

        if (doorState == FrontDoorState.Closed)
        {
            SetState(FrontDoorState.Open, "문 클릭 : Closed->Open", FrontDoorTransitionMode.Animated);
            SoundManager.Instance.PlaySfxByName("DoorOpen_SFX");
            return;
        }
    }
    //=================================================================//
    #endregion

    #region 괴한 현관문 컨트롤
    //=================================================================괴한 화장실문 컨트롤=====//

    /// <summary>
    /// 괴한이 문을 여는 시도
    /// -Closed면 Open으로 변경
    /// -Open/Broken 이면 바로 true
    /// -Locked면 fasle
    /// </summary>
    public bool EnemyTryOpenDoor(string reason)
    {
        if (doorState == FrontDoorState.Open) return true;
        if (doorState == FrontDoorState.Broken) return true;

        if (doorState == FrontDoorState.Closed)
        {
            SetState(FrontDoorState.Open, "괴한이 문을 : Close->Open", FrontDoorTransitionMode.Animated);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 괴한이 문을 잠금 해제하는 시도
    /// -Locked면 Closed로 변경(잠금해제 기준)
    /// -그 외는 false
    /// </summary>
    public bool EnemyTryUnlock(string reason)
    {
        if (doorState != FrontDoorState.Locked) return false;

        SetState(FrontDoorState.Closed, "괴한이 문을 : Locked->Closed(Unlock)", FrontDoorTransitionMode.Animated);
        return true;
    }

    /// <summary>
    /// 괴한이 문을 부숴서 강제개방(라스트 페이즈용)
    /// -문이 어떤 상태든 Broken으로 바꿔버리기
    /// </summary>
    public void EnemyForceBreak(string reason)
    {
        if (doorState == FrontDoorState.Broken) return;
        SoundManager.Instance.PlaySfxByName("DoorBreak_SFX");
        SetState(FrontDoorState.Broken, "괴한이 문을 부숴버림", FrontDoorTransitionMode.Animated);
    }
    //=================================================================//
    #endregion

    /// <summary>
    /// 상태 변경 공통 처리
    /// </summary>
    /// <param name="newState"></param>
    /// <param name="reason"></param>
    private void SetState(FrontDoorState newState, string reason, FrontDoorTransitionMode mode)
    {
        FrontDoorState oldState = doorState;

        if (oldState == newState)
        {
            Debug.Log("[BathRoomDoor] 상태변경 무시 => 이미같은 상태임");
            return;
        }

        doorState = newState;
        Debug.Log("[BathRoomDoor] 상태변경 : " + reason + "/" + oldState + "->" + newState + "/트랜지션모드" + mode);

        //구독자에게 알림부분
        DoorStateChanged?.Invoke(oldState, doorState, reason, mode);
    }

    /// <summary>
    /// [라스트 페이즈용 강제 롤백-체크포인트 롤백용]
    /// </summary>
    /// <param name="state"></param>
    /// <param name="reason"></param>
    public void ForceSetStateForRollBack(FrontDoorState state, string reason)
    {
        SetState(state, reason, FrontDoorTransitionMode.Instant);
    }
}
