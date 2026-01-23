using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 에너미에게 잡힘 트리거 삽입용
/// -플레이어와 트리거 충돌시 잡힘 이벤트 1회 호출,
/// -연출/리셋/롤백은 캣치디렉터에서 담당하게
/// </summary>
public class EnemyCatchTrigger : MonoBehaviour
{
    [Header("잡힘 처리 담당하는 디렉터")]
    [SerializeField] private CatchDirector catchDirector;

    [Header("캣치인포 붙인 에너미")]
    [SerializeField] private EnemyCatchInfo catchInfo;

    [Header("한번만 발사-중복 방지")]
    [SerializeField] private bool fireOnce = true;

    private bool hasFired;

    private void Awake()
    {
        if (catchDirector == null) catchDirector = FindObjectOfType<CatchDirector>();

        if (catchInfo == null) catchInfo = GetComponentInParent<EnemyCatchInfo>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fireOnce && hasFired) return;
        if (catchDirector == null || catchInfo == null) return;

        //플레이어 아니면 무시
        if (!other.CompareTag("Player")) return;
        hasFired = true;
        catchDirector.BeginCatch(catchInfo, other.gameObject);
    }
}
