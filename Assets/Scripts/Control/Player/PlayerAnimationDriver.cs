using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 플레이어 애니메이션 드라이버
/// 탑뷰, FPS 두개 쓰고 있는데, 공용으로 애니메이션 재생하게 할 용도
/// 탑뷰일때는 네비메쉬의 벨로시티 기반으로 Speed 해쉬값을 갱신하고
/// FPS 일때는 캐릭터 컨트롤러의 벨로시티 기반으로 Speed 해쉬값 갱신
/// </summary>
public class PlayerAnimationDriver : MonoBehaviour
{
    [Header("애니메이터 참조")]
    [SerializeField] private Animator animator;

    [Header("탑뷰 이동용-네비메쉬")]
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Header("FPS 이동용-캐릭터 컨트롤러")]
    [SerializeField] private CharacterController characterController;

    [Header("속도 파라미터 갱신 설정")]
    [Header("애니메이션 댐핑")]
    [SerializeField] private float speedDampTime = 0.10f;
    [Header("떨림 제거용")]
    [SerializeField] private float speedEpsilon = 0.01f;

    //Animator 속도 파라미터 해시
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (animator == null) return;

        float speed = CalculateMoveSpeed();

        //0근처= 0으로 만들어서 덜렁덜렁한거 방지
        if (speed < speedEpsilon) speed = 0.0f;

        //댐핑넣어서 애니메이션 자연스럽게 변환
        animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);
    }

    /// <summary>
    /// 현재 활성/사용 중인 이동 컴포넌트를 기준으로 이동 속도를 계산
    /// -네비메쉬가 enabled, isOnNavMesh 상태면, agent.velocity 사용
    /// -캐릭터 컨트롤러가 enabled 상태면, controller.velocity 사용
    /// </summary>
    private float CalculateMoveSpeed()
    {
        //네비메쉬 우선처리
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            //네비메쉬 위에 올라가 있어야 velocity 정상적으로 읽는듯
            if (navMeshAgent.isOnNavMesh)
            {
                return navMeshAgent.velocity.magnitude;
            }
        }

        //FPS 캐릭터 컨트롤러 처리
        if (characterController != null && characterController.enabled)
        {
            return characterController.velocity.magnitude;
        }

        //둘 다 아니면 0처리
        return 0.0f;
    }
}
