using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 복도 도어파티 연출용 view,
/// 이건 컨트롤러 따로 없이 트리거 형태로 바로 발동되게
/// 
/// 라스트 페이즈에만 작동되는거라서,
/// 복도에서 잡힘판정 났을때, 따로 롤백 시켜야 되는가에 대한 고찰
/// 루프 리셋때는 리셋테이블 따로 만들어서 관리중인데,
/// 롤백때는 급하게 전부다 라스트 페이즈 기점을 롤백시점으로 잡아놔서
/// 이걸 어떻게 처리할지가 고민인데..
/// 어차피 롤백되면, 플레이어 위치 집안으로 들어오니까
/// 복도문 닫기용 공용트리거 1개를 놔두고, 처리? 진짜 지져분해지는데 ㄱㅊ?
/// 지금 시간상 이게 가장 값싼 지불이긴 한데..
/// 일단 레츠고
/// </summary>
public class DoorParty_View : MonoBehaviour
{
    [Header("경첩 피벗조절용-경첩달아줘")]
    [SerializeField] private Transform doorHingePivot;

    [Header("닫힌 상태의 Y각도")]
    [SerializeField] private float closedYaw = 0.0f;

    [Header("열린 상태의 Y각도")]
    [SerializeField] private float openYaw = -90.0f;

    [Header("회전 되는 연출 속도")]
    [SerializeField] private float rotateDuration = 0.5f;

    //회전 코루틴 중복 방지용
    private Coroutine rotateRoutine;

    /// <summary>
    /// 연출없이 문 닫힌 상태 즉시 반영 -공용 문닫기 트리거에서 호출
    /// </summary>
    public void ApplyCloseYawImmediate()
    {
        Vector3 euler = doorHingePivot.localEulerAngles;
        euler.y = closedYaw;
        doorHingePivot.localEulerAngles = euler;
    }

    /// <summary>
    /// 연출과 함께 문열린 상태 반영- 각 복도문 트리거에서 호출
    /// </summary>
    /// <param name="state"></param>
    public void ApplyOpenYaw()
    {
        //기존 회전 연출 돌고있으면 중단하고 다시 시작
        if (rotateRoutine != null)
        {
            StopCoroutine(rotateRoutine);
            rotateRoutine = null;
        }
        SoundManager.Instance.PlaySfxByName("DoorBreak_SFX");
        rotateRoutine = StartCoroutine(RoTateToYawCo(openYaw));
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
            if (t > 1.0f) t = 1.0f;

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
