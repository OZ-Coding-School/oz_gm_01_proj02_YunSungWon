using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 에너미 잡힘 처리에 필요한 최소 정보 제공 컴포넌트
/// -잡힘 결과 분기처리용 (기존루프에서 잡혔는지, 라스트페이즈에서 잡혔는지)
/// 기존루프, 라스트페이즈 괴한 2개에 쓰일 것
/// </summary>
public class EnemyCatchInfo : MonoBehaviour
{
    public enum CatchType
    {
        Loop,
        LastPhase
    }

    [Header("에너미가 현재 타입")]
    [SerializeField] private CatchType catchType = CatchType.Loop;

    [Header("상호작용중인 괴한 애니메이터")]
    [SerializeField] private Animator enemyAnimator;

    public CatchType Type { get { return catchType; } }

    public Animator EnemyAnimator { get { return enemyAnimator; } }

    private void Awake()
    {
        if (enemyAnimator == null) enemyAnimator = GetComponent<Animator>();
    }
}
