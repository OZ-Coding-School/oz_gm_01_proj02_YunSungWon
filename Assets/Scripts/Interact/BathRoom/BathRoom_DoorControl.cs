using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화장실 문 상태를 관리하는 컨트롤러
/// -현재는 트리거 기반으로 플레이어 은신상태를 조절중인데(01/08)
/// 추후 애니메이션/잠금/부숨 연출을 붙일때 확장하기 위해 뼈대만 구성
/// 
/// [구현목표]
/// -EnemyControl은 문상태 를 보고 행동하게 만들기
/// -PlayerStealth는 플레이어의 은신상태만 관리
/// -문의 상태와 전환 타이밍은 여기서 정의
/// 
/// [문 클릭 규칙]
/// -열림->닫힘 / 닫힘->열림 /잠긴상태에선 문 클릭으로 열기 불가
/// [문고리 클릭 규칙]
/// -잠금<->잠금해제 토글식, 문이 열린 상태에선 잠금/해제 불가
/// 
/// [루프 리셋 연동]
/// -IResetTable로 등록, 루프 시작시 문 상태 초기화 할것
/// 
/// +괴한 컨트롤 API 추가
/// 괴한이 문을 열거나 잠금을 해제/파괴 할 수 있는 API 제공
/// 추후 연출 이벤트로 연결할 수 있도록 DoorStateChanged 이벤트 제공
/// 
/// 01/09(day4) 정리- 작업후 위 주석 지울것
/// [역할 : 컨트롤 로직만 담당]
/// 화장실 문 상태를 명확하게 enum으로 관리
/// 
/// -해당 클래스는 상태 정의/상태 전이 규칙만 담당할것
/// -문이 실제로 어떻게 움직이게 보일지는 view쪽에서 담당
/// 
/// [사용된 것들]
/// -상태기반 설계 : 물리 결과에 의존하지 않고, 게임규칙을 유지
/// -SRP : 문 상태 로직과 시각 연출 분리
/// -옵저버 : DoorStateChanged 이벤트로 연출/사운드/AI 느슨한 결합 가능(강결합 회피용)
/// -IResetTable : 루프 리셋시 각 오브젝트 상태를 정확하게 복구하기 위함
/// </summary>
public class BathRoom_DoorControl : MonoBehaviour, IResetTable
{
    //문 상태 정의
    public enum BathDoorState
    {
        Open = 0,
        Closed = 1,
        Locked = 2,
        Broken = 3
    }

    //문 상태 전환 모드 정의(연출모드/리셋시 제외모드)
    public enum BathDoorTransitionMode
    {
        Animated = 0,
        Instant = 1
    }

    [Header("현재 문 상태")]
    [SerializeField] private BathDoorState doorState = BathDoorState.Closed;

    //문 상태 변경 이벤트(연출 훅)
    //연출 생기면 여기에 구독해서 처리
    //<old->new 변화 방향, 변경된 이유>
    //트랜지션모드가 인스턴트면(리셋용), 연출없이 즉시 회전값 고정
    public event Action<BathDoorState, BathDoorState, string, BathDoorTransitionMode> DoorStateChanged;

    //외부 접근용 프로퍼티
    public BathDoorState CurState { get { return doorState; } }

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
        SetState(BathDoorState.Closed, "리셋상태: 문상태 = Closed", BathDoorTransitionMode.Instant);
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
        if (doorState == BathDoorState.Broken)
        {
            return;
        }

        //잠긴문은 문클릭으로 열기 불가, 상호작용 불가-연출 추가 예정
        if (doorState == BathDoorState.Locked)
        {
            SoundManager.Instance.PlaySfxByName("ErrorAlert_SFX");
            return;
        }

        if (doorState == BathDoorState.Open)
        {
            SetState(BathDoorState.Closed, "문 클릭 : Open->Closed", BathDoorTransitionMode.Animated);
            SoundManager.Instance.PlaySfxByName("DoorClose_SFX");
            return;
        }

        if (doorState == BathDoorState.Closed)
        {
            SetState(BathDoorState.Open, "문 클릭 : Closed->Open", BathDoorTransitionMode.Animated);
            SoundManager.Instance.PlaySfxByName("DoorOpen_SFX");
            return;
        }
    }

    /// <summary>
    /// [문고리 클릭] 잠금/해제 토글
    /// -문 상태가 Closed 일때만 잠금 사능, 
    /// Open 상태일땐, 문클릭 후, 닫히고? 닫힌상태에서 문고리 클릭해야 잠글 수 있는 방향으로
    /// </summary>
    public void InteractHandle()
    {
        //부숴진 문은 상호작용 불가
        if (doorState == BathDoorState.Broken)
        {
            return;
        }

        //잠긴문은 문클릭으로 열기 불가, 상호작용 불가-연출 추가 예정
        if (doorState == BathDoorState.Open)
        {
            SoundManager.Instance.PlaySfxByName("ErrorAlert_SFX");
            return;
        }

        if (doorState == BathDoorState.Closed)
        {
            SetState(BathDoorState.Locked, "문고리 클릭 : Closed ->  Locked", BathDoorTransitionMode.Animated);
            SoundManager.Instance.PlaySfxByName("DoorTryLock_SFX");
            return;
        }

        if (doorState == BathDoorState.Locked)
        {
            SetState(BathDoorState.Closed, "문고리 클릭 : Locked -> Closed", BathDoorTransitionMode.Animated);
            SoundManager.Instance.PlaySfxByName("DoorTryLock_SFX");
            return;
        }
    }
    //=================================================================//
    #endregion

    #region 괴한 화장실문 컨트롤
    //=================================================================괴한 화장실문 컨트롤=====//
    /// <summary>
    /// 괴한이 문을 여는 시도
    /// -Closed면 Open으로 변경
    /// -Open/Broken 이면 바로 true
    /// -Locked면 fasle
    /// </summary>
    public bool EnemyTryOpenDoor()
    {
        if (doorState == BathDoorState.Open) return true;
        if (doorState == BathDoorState.Broken) return true;

        if (doorState == BathDoorState.Closed)
        {
            SetState(BathDoorState.Open, "괴한이 문을 : Close->Open", BathDoorTransitionMode.Animated);
            SoundManager.Instance.PlaySfxByName("DoorOpen_SFX");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 괴한이 문을 잠금 해제하는 시도
    /// -Locked면 Closed로 변경(잠금해제 기준)
    /// -그 외는 false
    /// </summary>
    public bool EnemyTryUnlock()
    {
        if (doorState != BathDoorState.Locked) return false;

        SetState(BathDoorState.Closed, "괴한이 문을 : Locked->Closed(Unlock)", BathDoorTransitionMode.Animated);
        SoundManager.Instance.PlaySfxByName("DoorTryLock_SFX");
        return true;
    }

    /// <summary>
    /// 괴한이 문을 부숴서 강제개방(라스트 페이즈용)
    /// -문이 어떤 상태든 Broken으로 바꿔버리기
    /// </summary>
    public void EnemyForceBreak()
    {
        if (doorState == BathDoorState.Broken) return;
        SoundManager.Instance.PlaySfxByName("DoorBreak_SFX");
        SetState(BathDoorState.Broken, "괴한이 문을 부숴버림", BathDoorTransitionMode.Animated);
    }
    //=================================================================//
    #endregion

    /// <summary>
    /// 상태 변경 공통 처리
    /// </summary>
    /// <param name="newState"></param>
    /// <param name="reason"></param>
    private void SetState(BathDoorState newState, string reason,BathDoorTransitionMode mode)
    {
        BathDoorState oldState = doorState;

        if (oldState == newState)
        {
            return;
        }

        doorState = newState;
        
        //구독자에게 알림부분
        DoorStateChanged?.Invoke(oldState, doorState, reason, mode);
    }

    /// <summary>
    /// [라스트 페이즈용 강제 롤백-체크포인트 롤백용]
    /// </summary>
    /// <param name="state"></param>
    /// <param name="reason"></param>
    public void ForceSetStateForRollBack(BathDoorState state, string reason)
    {
        SetState(state, reason, BathDoorTransitionMode.Instant);
    }
}
