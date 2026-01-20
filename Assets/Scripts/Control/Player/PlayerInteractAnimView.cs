using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 플레이어 상호작용 애니메이션(트리거)를 한곳에서만 제어하기 위함
/// -진짜 미쳐버릴거 같음 연출훅따로 만들어놨더니, 애먼곳에서 다 터짐
/// 특히 오토인터랙트 부분에 뭔생각으로 애니메이션 넣었는지
/// 
/// 어떤 상호작용 로직이던 플레이어의 상호작용 관련 애니메이션이 필요하면
/// 이 view 만 호출하면 되게 정리할 것
/// 지금 각 상호작용 오브젝트에 연출훅이 있긴한데,
/// 그건 플레이어의 연출을 담당할게 아니라서, 쓰기에 애매함
/// 플레이어의 연출을 떠맡을 허브역할이 필요
/// 그게 이거
/// 
/// 애니메이터 파라미터를 2개 지정
/// -Trigger : Interact(상호작용 공용)
/// -int : InteractType(분기점 설정1,2,3,4 전부 다른 상호작용으로 분기되게)
/// 상호작용 애니메이션 클립에 이벤트 걸고,
/// -각 클립별 실제로 상호작용이 일어나는 순간에 이벤트 추가
/// </summary>
public class PlayerInteractAnimView : MonoBehaviour
{
    [Header("플레이어 애니메이터 참조")]
    [SerializeField] private Animator animator;

    [Header("상호작용 파라미터 이름")]
    [SerializeField] private string interactTriggerName = "Interact";
    [SerializeField] private string interactTypeName = "InteractType";

    [Header("애니 이벤트 사용할지 여부")]
    [SerializeField] private bool useAnimationEvent = true;

    [Header("애니 이벤트 최대 대기 시간")]
    [SerializeField] private float maxWaitForAnimationEvent = 0.5f;

    [Header("애니 이벤트가 없을때 경우 단순 딜레이")]
    [SerializeField] private float FallbackDelay = 0.5f;

    private int interactTriggerHash;
    private int interactTypeHash;

    //현재 상호작용 애님 진행 중인지
    private bool isBusy;

    //애니 이벤트 타이밍 도착 여부
    private bool isAnimEventArrived;

    //애니 타임아웃 처리 코루틴
    private Coroutine waitCoroutin;

    //AutoInteract 에서 애니 이벤트 도착 여부 확인용
    public bool IsAnimEventArrived { get { return isAnimEventArrived; } }

    public bool IsBusy { get { return isBusy; } }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();

        interactTriggerHash = Animator.StringToHash(interactTriggerName);
        interactTypeHash = Animator.StringToHash(interactTypeName);
    }

    /// <summary>
    /// typeId 에 맞는 상호작용 애니메이션 재생
    /// </summary>
    /// <param name="typeId"></param>
    /// <param name="onMoment"></param>
    public bool TryPlay(int typeId)
    {
        Debug.Log("[AnimView]TryPlay 호출됨 IsBusy상태" + IsBusy);

        if (isBusy) return false;

        isBusy = true;
        isAnimEventArrived = false;

        //애니메이터 없으면, 애니 이벤트 즉시 도착 처리로 알림
        if (animator == null)
        {
            isAnimEventArrived = true;
            isBusy = false;
            return true;
        }

        animator.SetInteger(interactTypeHash, typeId);
        animator.ResetTrigger(interactTriggerHash);
        animator.SetTrigger(interactTriggerHash);

        //기존 코루틴 정리
        if (waitCoroutin != null)
        {
            StopCoroutine(waitCoroutin);
            waitCoroutin = null;
        }

        waitCoroutin = StartCoroutine(WaitForMomentCo());
        return true;
    }

    /// <summary>
    /// 이벤트 호출용-실제로 상호작용 일어나는! 이벤트 프레임에서 호출할 것
    /// </summary>
    public void OnAutoInteractAnimationMoment()
    {
        Debug.Log("[AnimView] 이벤트 모먼트 호출됨");
        isAnimEventArrived = true;
    }

    /// <summary>
    /// 이벤트 호출용- 실제로 상호작용이 끝나는! 프레임에서 호출할 것
    /// </summary>
    public void OnAutoInteractAnimationEnd()
    {
        Debug.Log("[AnimView] 이벤트 엔드 호출됨");
        Release();
    }

    /// <summary>
    /// 애니 이벤트 기다리고, 누락이면 폴백딜레이로
    /// </summary>
    private IEnumerator WaitForMomentCo()
    {
        if (useAnimationEvent)
        {
            float elapsed = 0.0f;

            //이벤트 들어오는거 먼저 대기
            while (!isAnimEventArrived && elapsed < maxWaitForAnimationEvent)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            //이벤트 대기시간 이후에도 안오면 폴백딜레이로 보험처리
            if (!isAnimEventArrived && FallbackDelay > 0.0f)
            {
                yield return new WaitForSeconds(FallbackDelay);
            }

            //이벤트 없으면 강제 진행
            isAnimEventArrived = true;
        }
        
        //애니메이션이 아예 없는 경우-폴백 딜레이로 타이밍 맞추기
        else
        {
            if (FallbackDelay > 0.0f)
            {
                yield return new WaitForSeconds(FallbackDelay);
            }
            isAnimEventArrived = true;
        }
    }

    /// <summary>
    /// AutoInteract가 상호작용 실행 끝낸뒤 호출-Busy 플래그 해제
    /// </summary>
    public void Release()
    {
        isBusy = false;

        if (waitCoroutin != null)
        {
            StopCoroutine(waitCoroutin);
            waitCoroutin = null;
        }
    }
}
