using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 괴한의 시야만 담당하는 센서 컴포넌트
/// 게임의 몰입감을 위한 필수 요소라고 판단,
/// 실제 괴한의 시야로 플레이어 발견하기 위함(플레이어 스텔스 상태 판단)
/// 
/// Enemycontrol과 분리, SRP 준수하기 위함
/// -일정 간격으로 플레어이가 시야각 안에 들어왔는지 검사
/// -가림 (문/벽) 여부는 레이캐스트로 판정
/// 
/// -업데이트에 바로 때려박는게 아니라,
/// 업데이트쪽에 넣되, 부담안되게 0.1초 간격으로 검사하게 할 것
/// 
/// </summary>
public class EnemyVisionSensor : MonoBehaviour
{
    //플레이어가 보이기 시작한 순간 1회 호출할 이벤트
    public event Action PlayerSpotted;

    [Header("괴한 시야(눈,얼굴쪽)")]
    [SerializeField] private Transform eyeBall;

    [Header("시야 거리")]
    [SerializeField] private float viewDistance = 10.0f;

    [Header("시야 각도")]
    [SerializeField] private float viewAngle = 90.0f;

    [Header("시야에 가려지는 레이어마스크")]
    [SerializeField] private LayerMask obstructionMask;

    [Header("시야 검사 간격")]
    [SerializeField] private float checkInterval = 0.1f;

    [Header("플레이어 머리 위치 오프셋")] //나중에 앉기하면 잘 발견 못하게 일단 임시
    [SerializeField] private float playerAimHeight = 1.0f;

    [Header("테스트용 : 디버그용 기즈모 on/off")]
    [SerializeField] private bool drawGizmo = true;

    //플레이어 위치 저장(컨트롤 에서 주입받을거임)
    private Transform playerTarget;

    //현재 플레이어 시야안에 있는지 저장용
    private bool isTargetVisible;

    //마지막으로 플레이어 봤던 위치, 시간 (추격로직 확장용)
    private Vector3 lastSeenPosition;
    private float lastSeenTime;

    //검사 타이머(업데이트에 무작정 때려넣는거 완화용)
    private float timer;

    //=====외부 접근용 프로퍼티=====
    public bool IsTargetVisible { get { return isTargetVisible; } }
    public Vector3 LastSeenPosition { get { return lastSeenPosition; } }
    public float LastSeenTime { get { return lastSeenTime; } }

    /// <summary>
    /// Enemy 디렉터/컨트롤에서 플레이어 타겟 주입할것
    /// </summary>
    /// <param name="target"></param>
    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < checkInterval) return;

        timer = 0.0f;
        ThickVision();
    }

    /// <summary>
    /// 시야 검사 1회 수행
    /// </summary>
    private void ThickVision()
    {
        if (playerTarget == null)
        {
            Debug.Log("지정타겟(플레이어)가 null 상태임-제대로 주입되고 있는지 확인좀");
            isTargetVisible = false;
            return;
        }

        Transform originTransform = eyeBall;
        Vector3 origin = originTransform.position;

        //머리쪽으로 위치 저장(확장용 - 안 쓰게 될수도 있음)
        Vector3 targetPos = playerTarget.position + (Vector3.up * playerAimHeight);
        Vector3 toTarget = targetPos - origin;

        float distance = toTarget.magnitude;
        if (distance > viewDistance)
        {
            isTargetVisible = false;
            return;
        }

        Vector3 dir = toTarget.normalized;

        //각도 판정 부분-정면기준 1/2 반으로 쪼개서
        float angle = Vector3.Angle(originTransform.forward, dir);
        if (angle > (viewAngle * 0.5f))
        {
            isTargetVisible = false;
            return;
        }

        //가려지는 레이어 있는지 체크
        bool blocked = Physics.Raycast(origin, dir, distance, obstructionMask, QueryTriggerInteraction.Ignore);
        if (blocked)
        {
            isTargetVisible = false;
            return;
        }

        //앞선 검증 모두 통과시 보이는걸로 판단
        bool wasVisible = isTargetVisible;
        isTargetVisible = true;

        lastSeenPosition = playerTarget.position;
        lastSeenTime = Time.time;

        //보였던 순간에 이벤트 1회 발생
        if (!wasVisible) PlayerSpotted?.Invoke();
    }

    //테스트용 기즈모 유니티 에디터 사용
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmo)
        {
            return;
        }

        Transform originTransform = eyeBall != null ? eyeBall : this.transform;
        Vector3 origin = originTransform.position;

        //거리 원
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, viewDistance);

        //시야 각도 라인(좌/우)
        Vector3 leftDir = Quaternion.Euler(0.0f, -viewAngle * 0.5f, 0.0f) * originTransform.forward;
        Vector3 rightDir = Quaternion.Euler(0.0f, viewAngle * 0.5f, 0.0f) * originTransform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + leftDir * viewDistance);
        Gizmos.DrawLine(origin, origin + rightDir * viewDistance);
    }
#endif
}
