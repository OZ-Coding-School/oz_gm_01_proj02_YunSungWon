using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 네비메쉬 자동 상호작용 데이터 제공 컴포넌트
/// -플레이어가 서야 할 위치 = InteractPoint
/// -상호작용 허용 거리 InteractDistance
/// -실제 실행할 InteractableBase참조
/// 
/// 클릭으로 오브젝트를 선택했을때, '어디로 이동해서'/'언제상호작용'의
/// 기준점이 필요함. 따로 별도 컴포넌트로 분리
/// 
/// ++[해결할 문제]
/// -기존 구조가 InteractableBase를 단일 캐싱해서,
///  동일 타겟에 InteractableBase가 2개 이상 붙으면, 먼저 붙여진 하나만 실행되는 문제
///  ->목록으로 캐싱해서, AutoInteract에서 전부 실행 가능하게 해결할것
///  
/// [컴포지트 패턴 사용]
/// -다중 InteractableBase 제공자의 역할을 하게 될 것
/// </summary>
public class InteractTarget : MonoBehaviour
{
    [Header("플레이어가 이동, 서야 할 위치")]
    [SerializeField] private Transform interactPoint;

    [Header("상호작용 허용거리")]
    [SerializeField] private float interactDistance = 1.0f;

    //캐싱된 상호작용 오브젝트 목록
    private readonly List<InteractableBase> interactables = new List<InteractableBase>();

    //=====외부 접근용 프로퍼티=====
    public Transform InteractPoint { get { return interactPoint; } }
    public float InteractDistance { get { return interactDistance; } }
    public bool HasInteractables { get { return interactables.Count > 0; } }

    private void Awake()
    {
        if (interactPoint == null)
        {
            interactPoint = this.transform;
        }
        RefreshInteractablesCashe();
    }

    /// <summary>
    /// 이 타겟에서 실행할 interactableBase 목록을 반환
    /// </summary>
    public IReadOnlyList<InteractableBase> Getinteractables()
    {
        return interactables;
    }

    /// <summary>
    /// 상호작용 목록 캐시 갱신
    /// -런타임 도중에 구성변경이 있을경우 외부에서 호출
    /// </summary>
    public void RefreshInteractablesCashe()
    {
        interactables.Clear();

        //해당 오브젝트에 붙은 interactableBase 가져오고
        InteractableBase[] found = GetComponents<InteractableBase>();

        //순차적으로 추가
        int i = 0;
        while (i < found.Length)
        {
            InteractableBase item = found[i];
            if (item != null && item.enabled)
            {
                interactables.Add(item);
            }
            i++;
        }
    }
}
