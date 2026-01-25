using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 약통 액션형UI패널
/// 
/// UI가 열렸을때, 이번 루프에 복용가능 여부 표시,
/// [복용시] 버튼 클릭시, PerceptionManager.ForceReality() 실행
/// -루프당 1회 제한
/// -루프 리셋시 복구
/// 
/// 기존 약관련 상호작용 컴포넌트 따로 있었는데,
/// UI방식으로 교체, 성공적이면,
/// 기존 인터랙터블 컴포넌트는 제거하는 방향으로,
/// 
/// 가려고 했는데, 리셋테이블관련 상태를 여기다가 저장해버리니까,
/// 패널 껐다 켜졌다 할때마다 테이블에서 등록 해제가 반복되어버림.
/// 결국 상태를 온전하게 저장 못함-> 월드에 배치된 오브젝트에 상태저장
/// -> 즉 기존 MedicineInteractable 활용방법 찾아야됨
/// 
/// -루프 리셋/상태 복구는 MedicineInteractable에서 여기선 UI표시만
/// </summary>
public class MedicineUIPanel : UIActionPanelBase
{
    [Header("복용 버튼")]
    [SerializeField] private Button useButton;

    [Header("약 남아있는 개수 텍스트 안내")]
    [SerializeField] private TextMeshProUGUI remainText;

    [Header("MedicineInteractable 참조")]
    [SerializeField] private MedicineInteractable medicine;

    //사운드 관련
    private void OnEnable()
    {
        SoundManager.Instance.PlaySfxByName("PillInteract_SFX");
    }

    protected override void OnOpenActionPanel(InteractContext context)
    {
        medicine = null;
        if (context != null && context.Source != null)
        {
            medicine = context.Source.GetComponent<MedicineInteractable>();
        }
        useButton.onClick.RemoveListener(OnClickUse);
        useButton.onClick.AddListener(OnClickUse);
        RefreshUI();
    }

    protected override void OnCloseActionPanel()
    {
        useButton.onClick.RemoveListener(OnClickUse);
        medicine = null;
    }

    /// <summary>
    /// UI갱신
    /// -사용상태에 따라 버튼 활성화/비활성화
    /// -남아있는 갯수 텍스트 표현 위함(1개 남았다. 약이 하나도 없다)
    /// </summary>
    private void RefreshUI()
    {
        bool canUse = false;

        if (medicine != null)
        {
            canUse = medicine.CanUse;
        }

        useButton.interactable = canUse;
        useButton.gameObject.SetActive(canUse);

        if (canUse)
        {
            remainText.text = "통 안에 1개의 알약이 남아있는 것 같다.";
        }
        else
        {
            remainText.text = "통 안에는 아무것도 들어있지 않다.";
        }
    }

    /// <summary>
    /// 복용버튼 클릭시 호출-여기서 실제효과 적용
    /// </summary>
    private void OnClickUse()
    {
        SoundManager.Instance.PlaySfxByName("PillSwallow_SFX");

        if (medicine == null)
        {
            RefreshUI();
            return;
        }

        bool success = medicine.TryUse();

        if (!success)
        {
            RefreshUI();
            return;
        }

        RefreshUI();
    }
}
