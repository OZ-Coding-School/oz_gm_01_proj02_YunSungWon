using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 이동방식 분리 탑뷰<->1인칭
/// 시점모드에 따라 네비메쉬, 캐릭터 컨트롤러 토글식으로 변경
/// 
/// 탑뷰: 클릭이동(네비메쉬on) / 1인칭: WASD로 이동,마우스회전으로 시점변경(캐릭터 컨트롤러on)
/// </summary>
public class ControlToggle : MonoBehaviour
{
    public enum ControlMode
    {
        TopView = 0,
        FPS = 1
    }

    [Header("현재 이동 모드")] //시작시에는 탑뷰로
    [SerializeField] private ControlMode startMode = ControlMode.TopView;

    [Header("네비메쉬,컨트롤러 참조")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private PlayerControl playerControl;

    //외부 접근용 프로퍼티
    public ControlMode CurMode { get; private set; }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (playerControl == null) playerControl = GetComponent<PlayerControl>();
    }

    private void Start()
    {
        SetMode(startMode);
    }

    /// <summary>
    /// 필요시 이동모드 변경 호출-라스트 페이즈때 사용할듯
    /// </summary>
    /// <param name="mode"></param>
    public void SetMode(ControlMode mode)
    {
        CurMode = mode;

        if (mode == ControlMode.TopView) EnableTopView();
        else EnableFPS();
    }

    /// <summary>
    /// 탑뷰 활성화 될땐 컨트롤러 off, 네비메쉬만 on
    /// </summary>
    private void EnableTopView()
    {
        playerControl.enabled = false;
        agent.enabled = true;
        agent.isStopped = false;

        Debug.Log("탑뷰 모드 활성화-컨트롤러 off/ 네비메쉬 on");
    }

    /// <summary>
    /// 1인칭 활성화 될땐 네비메쉬 off, 컨트롤러 on
    /// </summary>
    private void EnableFPS()
    {
        agent.isStopped = true;
        agent.enabled = false;
        playerControl.enabled = true;

        Debug.Log("1인칭 모드 활성화-네비메쉬 off/ 컨트롤러 on");
    }
}
