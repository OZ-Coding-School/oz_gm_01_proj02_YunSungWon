using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BathRoom_DoorControl 에서 상태가 변경되면,
/// 실제 문 경첩부분 피벗을 회전시켜서 문 상태 시각화
/// 
/// -문상태 로직과 표현을 분리,
/// 
/// -절대 여기서 상호작용을 처리하지 말것
/// -변경된 상태에 따른 연출만 처리!!
/// </summary>
public class BathDoorView : MonoBehaviour
{
    [Header("문 상태 컨트롤러")]
    [SerializeField] private BathRoom_DoorControl doorControl;

    [Header("경첩 피벗조절용-경첩달아줘")]
    [SerializeField] private Transform doorHingePivot;

    [Header("닫힌 상태의 Y각도")]
    [SerializeField] private float closedYaw = 0.0f;

    [Header("열린 상태의 Y각도")]
    [SerializeField] private float openYaw = -140.0f;

    [Header("부숴진 상태의 Y각도-임시")] //진짜 부숴버려야함 일단 임시 각도 설정
    [SerializeField] private float brokenYaw = -180.0f;

    [Header("회전 되는 연출 속도")]
    [SerializeField] private float rotateDuration = 0.5f;

    //회전 코루틴 중복 방지용
    private Coroutine rotateRoutine;

    private void OnEnable()
    {
        doorControl.DoorStateChanged += OnDoorStateChanged;
        //Enable 시점에 현재 상태 즉시 반영->상태/표현 싱크 맞추기
        ApplyVisualImmediate();
    }

    private void OnDisable()
    {
        doorControl.DoorStateChanged -= OnDoorStateChanged;
    }

    /// <summary>
    /// 문 상태가 변경되면 호출되는 콜백
    /// </summary>
    private void OnDoorStateChanged(
        BathRoom_DoorControl.BathDoorState oldState,
        BathRoom_DoorControl.BathDoorState newState,
        string reason,
        BathRoom_DoorControl.BathDoorTransitionMode mode)
    {
        if (mode == BathRoom_DoorControl.BathDoorTransitionMode.Instant)
        {
            ApplyVisualImmediate();
            return;
        }

        ApplyVisualByState(newState);
    }

    /// <summary>
    /// 현재 상태를 연출없이 즉시 반영
    /// </summary>
    private void ApplyVisualImmediate()
    {
        ApplyVisualImmediateByState(doorControl.CurState);
    }

    /// <summary>
    /// 특정 상태를 연출없이 즉시 반영
    /// </summary>
    private void ApplyVisualImmediateByState(BathRoom_DoorControl.BathDoorState state)
    {
        float targetYaw = GetYawByState(state);

        Vector3 euler = doorHingePivot.localEulerAngles;
        euler.y = targetYaw;
        doorHingePivot.localEulerAngles = euler;
    }

    /// <summary>
    /// 특정 상태를 연출과 함께 반영
    /// </summary>
    /// <param name="state"></param>
    private void ApplyVisualByState(BathRoom_DoorControl.BathDoorState state)
    {
        float targetYaw = GetYawByState(state);

        //기존 회전 연출 돌고있으면 중단하고 다시 시작
        if (rotateRoutine != null)
        {
            StopCoroutine(rotateRoutine);
            rotateRoutine = null;
        }

        //연출시간 0으로 설정되어 있으면, 즉시 적용
        if (rotateDuration <= 0.0f)
        {
            ApplyVisualImmediateByState(state);
            return;
        }

        rotateRoutine = StartCoroutine(RoTateToYawCo(targetYaw));
    }

    /// <summary>
    /// 상태별 목표 Y각도 반환
    /// -Locked는 닫힌 상태와 같은거로 처리, 각도는 동일하니까
    /// </summary>
    /// <param name="state"></param>
    private float GetYawByState(BathRoom_DoorControl.BathDoorState state)
    {
        if (state == BathRoom_DoorControl.BathDoorState.Open) return openYaw;
        if (state == BathRoom_DoorControl.BathDoorState.Broken) return brokenYaw;

        return closedYaw;
    }

    /// <summary>
    /// 목표 Y각도까지 회전
    /// Mathf.DeltaAngle- 최단 각도로 회전시키기 위함 (ex>-350->10 = 20)
    /// </summary>
    /// <param name="targetYaw"></param>
    private IEnumerator RoTateToYawCo(float targetYaw)
    {
        float time = 0.0f;

        Vector3 startEuler = doorHingePivot.localEulerAngles;
        float startYaw = startEuler.y;

        float deltaYaw = Mathf.DeltaAngle(startYaw, targetYaw);

        while (time < rotateDuration)
        {
            time += Time.deltaTime;
            float t = time / rotateDuration;
            if( t > 1.0f) t = 1.0f;

            float curYaw = startYaw + (deltaYaw * t);

            Vector3 euler = doorHingePivot.localEulerAngles;
            euler.y = curYaw;
            doorHingePivot.localEulerAngles = euler;

            yield return null;
        }

        Vector3 finalEuler = doorHingePivot.localEulerAngles;
        finalEuler.y = targetYaw;
        doorHingePivot.localEulerAngles = finalEuler;

        rotateRoutine = null;
    }
}
