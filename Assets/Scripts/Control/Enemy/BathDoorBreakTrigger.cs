using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


/// <summary>
/// 라스트 페이즈용 괴한 화장실 문 파괴 트리거
/// -변이괴한이 트리거에 닿으면 그냥 문을 터쳐버리기
/// -문이 닫혀있는 경우에만 진행
/// </summary>
public class BathDoorBreakTrigger : MonoBehaviour
{
    [Header("화장실 문 컨트롤러")]
    [SerializeField] private BathRoom_DoorControl doorControl;

    [Header("문 부숴버릴 변이괴한 태그")]
    [SerializeField] private string lastPhaseEnemyTag = "LastPhaseEnemy";

    private void Reset()
    {
        //에디터에서 자동할당 시도
        doorControl = GetComponentInParent<BathRoom_DoorControl>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[BathDoorBreakTrigger] 도어 컨트롤은 null 체크 전");

        if (doorControl == null) return;

        Debug.Log("[BathDoorBreakTrigger] 도어 컨트롤은 null 아님");

        //라스트 페이즈 괴한만 문 부수기 가능
        if (other.CompareTag(lastPhaseEnemyTag))
        {
            Debug.Log("[BathDoorBreakTrigger] 라스트페이즈 태그도 정상적으로 들어옴");

            //닫힘, 잠금 상태에만 파괴가능
            bool isBlocked = doorControl.CurState == BathRoom_DoorControl.BathDoorState.Closed ||
                doorControl.CurState == BathRoom_DoorControl.BathDoorState.Locked;

            if (!isBlocked) return;

            doorControl.EnemyForceBreak();
            Debug.Log("[BathDoorBreakTrigger] 에너미 포스브레이크 발동함");
        }
    }
}
