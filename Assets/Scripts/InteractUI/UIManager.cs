using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// UI패널 관련 전역 관리자 역할
/// 
/// [규칙]
/// -한번에 하나의 패널만 열기
/// -패널 열리면 게임입력 잠그기
/// -패널이 닫힐때 복구,
/// -루프 리셋시에는 UI강제 닫힘
/// 
/// 퍼사드 느낌으로, UI 열기,닫기,입력잠금,루프리셋 대응을
/// 여기서, 한곳에서 처리
/// 
/// </summary>
public class UIManager : MonoBehaviour, IResetTable
{
    public static UIManager Instance { get; private set; }

    [Header("플레이어 입력 게이트")]
    [SerializeField] private PlayerInputGate PlayerInputGate;

    [Header("현재 열려있는 패널-확인용 어트리뷰트")]
    [SerializeField] private UIPanelBase curPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (PlayerInputGate == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                PlayerInputGate = playerObject.GetComponent<PlayerInputGate>();
            }
        }
    }

    //루프 리셋 연동
    private void OnEnable()
    {
        LoopManager.Instance.RegisterResetTable(this);
    }

    private void OnDisable()
    {
        LoopManager.Instance.UnRegisterResetTable(this);
    }

    /// <summary>
    /// 루프 리셋시 호출->UI강제 종료+입력복구
    /// </summary>
    public void ResetState()
    {
        CloseCurPanel("루프 리셋으로 UI 강제 닫힘");
    }

    /// <summary>
    /// 어떤 패널이던 열기(공용)
    /// </summary>
    /// <param name="panel"></param>
    /// <param name="context"></param>
    public void OpenPanel(UIPanelBase panel, InteractContext context)
    {
        if (panel == null)
        {
            Debug.Log("[UIManager] OpenPanel 실패 - Panel이 null");
            return;
        }
        
        //이미 같은 패널이 열려있으면 무시
        if (curPanel == panel && curPanel.IsOpen) return;

        //기존 패널 닫기
        if (curPanel != null) curPanel.Close();

        curPanel = panel;

        //플레이어 입력 잠금
        if (PlayerInputGate != null)
        {
            PlayerInputGate.SetGameplayInputEnabeld(false);
        }

        //패널 열기
        curPanel.Open(context);

        Debug.Log("[UIManager] 패널 오픈됨" + panel.name + "사유=" + context.Reason);
    }

    /// <summary>
    /// 현재 열린 패널 닫기(공용)
    /// </summary>
    /// <param name="reason"></param>
    public void CloseCurPanel(string reason)
    {
        if (curPanel != null)
        {
            curPanel.Close();
            Debug.Log("[UIManager] 패널 닫힘" + curPanel.name + "사유=" + reason);
        }
        
        curPanel = null;

        //플레이어 입력 복구
        if (PlayerInputGate != null)
        {
            PlayerInputGate.SetGameplayInputEnabeld(true);
        }
    }

    /// <summary>
    /// UI가 열려있는지 여부
    /// </summary>
    /// <returns></returns>
    public bool IsAnyPanelOpen()
    {
        return curPanel != null && curPanel.IsOpen;
    }
}
