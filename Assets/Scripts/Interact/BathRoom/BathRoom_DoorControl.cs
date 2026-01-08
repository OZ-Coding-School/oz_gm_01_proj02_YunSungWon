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
/// </summary>
public class BathRoom_DoorControl : MonoBehaviour, IResetTable
{
    //문 상태 정의
    public enum BathDoorState
    {
        Open = 0,
        Closed =1,
        Locked =2,
        Broken =3
    }

    [Header("현재 문 상태")]
    [SerializeField] private BathDoorState doorState = BathDoorState.Closed;

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
        doorState = BathDoorState.Closed;
        Debug.Log("[BathRoom_DoorControl] ResetState: 문상태 초기화");
    }

    /// <summary>
    /// [문 자체 클릭] 열기/닫기 토글
    /// Locked 상태에선 열리지 않게
    /// </summary>
    public void InteractDoor()
    {
        //부숴진 문은 열린걸로 간주, 및 상호작용 불가
        if (doorState == BathDoorState.Broken)
        {
            Debug.Log("[BathRommDoor] 문 클릭 : Broken 상태(열려있는 취급/상호작용 불가)");
            return;
        }

        //잠긴문은 문클릭으로 열기 불가, 상호작용 불가-연출 추가 예정
        if (doorState == BathDoorState.Locked)
        {
            Debug.Log("[BathRommDoor] 문 클릭 : Locked 상태(잠겨있는 취급/상호작용 불가)");
            return;
        }

        if (doorState == BathDoorState.Open)
        {
            SetState(BathDoorState.Closed, "문 클릭 : Open->Closed");
            return;
        }

        if (doorState == BathDoorState.Closed)
        {
            SetState(BathDoorState.Open, "문 클릭 : Closed->Open");
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
            Debug.Log("[BathRommDoor] 문고리 클릭 : Broken 상태(상호작용 불가)");
            return;
        }

        //잠긴문은 문클릭으로 열기 불가, 상호작용 불가-연출 추가 예정
        if (doorState == BathDoorState.Open)
        {
            Debug.Log("[BathRommDoor] 문고리 클릭 : Open 상태(문 먼저 닫아야함)");
            return;
        }

        if (doorState == BathDoorState.Closed)
        {
            SetState(BathDoorState.Locked, "문고리 클릭 : Closed ->  Locked");
            return;
        }

        if (doorState == BathDoorState.Locked)
        {
            SetState(BathDoorState.Closed, "문고리 클릭 : Locked -> Closed");
            return;
        }
    }

    /// <summary>
    /// 괴한 시점에서 현재 문을 즉시 통과 가능한지 검증
    /// 잠겨있을 경우에만 지연, 문이 단순 닫혀있는 경우, 열고 들어오는 연출로 처리,
    /// </summary>
    public bool CanEnemyEnter()
    {
        if (doorState == BathDoorState.Locked) return false;
        return true;
    }

    /// <summary>
    /// 괴한이 잠금 해제 시도/파괴 마친 후 호출
    /// -Locked 상태면 Broken으로 전환
    /// </summary>
    public void ForceOpenByEnemy()
    {
        if (doorState == BathDoorState.Locked)
        {
            SetState(BathDoorState.Broken, "괴한 강제 개방 : Locked -> Broken");
        }
    }

    /// <summary>
    /// 상태 변경 공통 처리
    /// </summary>
    /// <param name="newState"></param>
    /// <param name="reason"></param>
    private void SetState(BathDoorState newState, string reason)
    {
        doorState = newState;
        Debug.Log("[BathRoomDoor] 상태변경 : " + reason + "/현재 = " + doorState);
    }
}
