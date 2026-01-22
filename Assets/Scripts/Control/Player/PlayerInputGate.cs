using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// UI가 열려있는동안 입력조작 잠그고, 닫히면 복구하는 게이트역할
/// 
/// clickMove/AutoInteract/PlayerInteractor/PlayerControl 등의
/// 여러 인풋을 일괄 관리 하기 위함.
/// 퍼사드 패턴식 - 여러 입력 컴포넌트를 한곳에서 통합 제어
/// </summary>
public class PlayerInputGate : MonoBehaviour
{
    [Header("ClickMove 컴포넌트")]
    [SerializeField] private ClickMove clickMove;

    [Header("AutoInteract 컴포넌트")]
    [SerializeField] private AutoInteract autoInteract;

    [Header("PlayerInteractor 컴포넌트")]
    [SerializeField] private PlayerInteractor playerInteractor;

    //[Header("PlayerControl 컴포넌트")]
    //[SerializeField] private PlayerControl playerControl;

    [Header("플레이어 NavMesh 컴포넌트")]
    [SerializeField] private NavMeshAgent navMeshAgent;

    //현재 게임 입력이 활성화 상태인지
    public bool IsGameplayInputEnabled { get; private set; } = true;

    private void Awake()
    {
        clickMove = GetComponent<ClickMove>();
        autoInteract = GetComponent<AutoInteract>();
        playerInteractor = GetComponent<PlayerInteractor>();
        //playerControl = GetComponent<PlayerControl>();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// 게임 입력 활성,비활성 전환
    /// UI열림닫힘 타이밍에 UIManager에서 호출
    /// </summary>
    /// <param name="enabled"></param>
    public void SetGameplayInputEnabeld(bool enabled)
    {
        IsGameplayInputEnabled = enabled;
        if (clickMove != null) clickMove.enabled = enabled;
        if (autoInteract != null) autoInteract.enabled = enabled;
        if (playerInteractor != null) playerInteractor.enabled = enabled;

        //컨트롤 부분은 오히려 충돌문제 나서 빼버리는게 나을지도,
        //어차피 UI상호작용은 탑뷰에서만 가능하게 할거니까
        //if (playerControl != null) playerControl.enabled = enabled;

        //이동중 UI가 열려을때 멈추게 처리
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            if (enabled)
            {
                navMeshAgent.isStopped = false;
            }
            else
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
                navMeshAgent.velocity = Vector3.zero;
            }
        }
    }
}
