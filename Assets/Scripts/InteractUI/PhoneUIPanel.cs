using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 핸드폰 관련 액션 UI 패널
/// 작동 흐름>
/// -비밀번호 입력(잠금해제)->112 버튼 입력
/// -현실 상태에서만 정상신고 가능하게,
/// -주소 단서를 발견해야 최종신고로 인정
/// 
/// -UI를 열고,
/// -플레이어 특정 상호작용으로만 규칙 판정/상태 전이
/// 
/// [사용기술]
/// -패널 내부의 진행 상태는 enum으로 상태머신관리
/// -템플릿 메서드로 UI 오픈/클로즈 흐름은 고정 상세는 파생
///
/// </summary>
public class PhoneUIPanel : UIActionPanelBase
{
    //패널 내부 진행 상태
    private enum PhoneState
    {
        Locked = 0,
        Dialing = 1,
        Calling = 2,
        Result = 3
    }

    [Header("상단 타이틀(현재 폰 상태)")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("입력되는 숫자텍스트")]
    [SerializeField] private TextMeshProUGUI inputText;

    [Header("상호작용 피드백 텍스트")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("잠금 해제 버튼")] // 원래 비밀번호 입력은 따로 버튼 없지않나 싶은데..
    [SerializeField] private Button unlockButton;

    [Header("통화 버튼")]
    [SerializeField] private Button callButton;

    [Header("입력 지우기 버튼")]
    [SerializeField] private Button backSpaceButton;

    [Header("숫자 키패드 그룹(활성화/비활성화)")]
    [SerializeField] private GameObject keypadRoot;

    [Header("비밀번호 정답(일단 고정)")]
    [SerializeField] private string corrcetPassword = "";

    [Header("신고번호 고정")] //나중에 다른 고정번호 관련 분기 넣을지는 고민좀
    [SerializeField] private string emergencyNumber = "";

    [Header("신고 성공 판정 주소 단서 ID")]
    [SerializeField] private string addressEvidenceId = "";

    [Header("통화 연출 시간")]
    [SerializeField] private float callDelay = 2.0f;

    //현실 여부/단서 여부 확인용 퍼셉션매니저 참조
    private PerceptionManager perceptionManager;

    //현재 폰 패널 상태 저장용
    private PhoneState curState;

    //비밀번호 입력 버퍼
    private string passwordBuffer;

    //신고번호 입력 버퍼
    private string dialBuffer;

    //중복방지용 통화 진행 코루틴
    private Coroutine callRoutine;

    private void OnEnable()
    {
        SoundManager.Instance.PlaySfxByName("getPhone_SFX");
        messageText.text = null;
    }

    protected override void OnOpenActionPanel(InteractContext context)
    {
        if (PerceptionManager.Instance != null)
        {
            perceptionManager = PerceptionManager.Instance;
        }

        //입력 버퍼들 초기화
        passwordBuffer = string.Empty;
        dialBuffer = string.Empty;

        //초기상태는 잠금상태
        SetState(PhoneState.Locked, "패널 오픈 : 초기 잠금상태");

        //버튼 리스너 연결(제거후 등록-중복방지)
        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveListener(OnClickUnlock);
            unlockButton.onClick.AddListener(OnClickUnlock);
        }

        if (callButton != null)
        {
            callButton.onClick.RemoveListener(OnClickCall);
            callButton.onClick.AddListener(OnClickCall);
        }

        if (backSpaceButton != null)
        {
            backSpaceButton.onClick.RemoveListener(OnClickBackSpace);
            backSpaceButton.onClick.AddListener(OnClickBackSpace);
        }

        RefreshUI();
    }

    protected override void OnCloseActionPanel()
    {
        //코루틴 중단
        if (callRoutine != null)
        {
            StopCoroutine(callRoutine);
            callRoutine = null;
        }

        //버튼 리스너 해제
        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveListener(OnClickUnlock);
        }

        if (callButton != null)
        {
            callButton.onClick.RemoveListener(OnClickCall);
        }

        if (backSpaceButton != null)
        {
            backSpaceButton.onClick.RemoveListener(OnClickBackSpace);
        }
    }

    /// <summary>
    /// 숫자 버튼 온클릭에서 호출-유니티 이벤트로 string 인자 전달
    /// </summary>
    /// <param name="digit"></param>
    public void OnClickDigit(string digit)
    {
        SoundManager.Instance.PlaySfxByName("PhoneKeypad_SFX");

        if (string.IsNullOrEmpty(digit)) return;

        if (curState == PhoneState.Locked) passwordBuffer += digit;
        else if (curState == PhoneState.Dialing) dialBuffer += digit;
        else return;

        RefreshUI();
    }

    /// <summary>
    /// 지우기 버튼 클릭 처리
    /// </summary>
    private void OnClickBackSpace()
    {
        SoundManager.Instance.PlaySfxByName("PhoneKeypad_SFX");

        if (curState == PhoneState.Locked)
        {
            passwordBuffer = RemoveLastChar(passwordBuffer);
        }
        else if (curState == PhoneState.Dialing)
        {
            dialBuffer = RemoveLastChar(dialBuffer);
        }
        else return;
        RefreshUI();
    }

    /// <summary>
    /// 잠금 해제 버튼 클릭 처리
    /// </summary>
    private void OnClickUnlock()
    {
        if (curState != PhoneState.Locked) return;

        if (passwordBuffer == corrcetPassword)
        {
            SoundManager.Instance.PlaySfxByName("PhoneKeypad_SFX");

            dialBuffer = string.Empty;
            SetState(PhoneState.Dialing, "비밀번호 정답 : 다이얼화면으로 진입");
            SoundManager.Instance.PlaySfxByName("SuccessAlert_SFX");
            RefreshUI();
            return;
        }

        messageText.text = "비밀번호가 틀린 것 같다.";
        SoundManager.Instance.PlaySfxByName("ErrorAlert_SFX");
        passwordBuffer = string.Empty;
        RefreshUI();
    }

    /// <summary>
    /// 통화 버튼 클릭처리(112+현실상태+주소 단서 여부 체크 구간)
    /// </summary>
    private void OnClickCall()
    {
        if (curState != PhoneState.Dialing) return;

        SoundManager.Instance.PlaySfxByName("PhoneKeypad_SFX");

        if (dialBuffer != emergencyNumber)
        {
            messageText.text = "이 번호가 아닌거 같은데..";
            SoundManager.Instance.PlaySfxByName("PhoneFail_SFX");
            RefreshUI();
            return;
        }

        if (perceptionManager == null)
        {
            messageText.text = "기억나는게 없다."; //퍼셉션 널 났을때 대비
            RefreshUI();
            return;
        }

        if (perceptionManager.CurState != PerceptionManager.PerceptionState.Reality)
        {
            messageText.text = "이게 대체 무슨..제대로 보이지 않잖아..!";
            RefreshUI();
            return;
        }

        //통화 연출 시작
        SetState(PhoneState.Calling, "112 통화 시작");
        SoundManager.Instance.PlaySfxByName("PhoneLing_SFX");
        RefreshUI();

        //자꾸 여기서 null터짐 이유 분명히 해야함
        if (callRoutine != null)
        {
            StopCoroutine(callRoutine);
            callRoutine = null;
        }

        callRoutine = StartCoroutine(CallFlowCo());
    }

    /// <summary>
    /// 통화 흐름 코루틴(연출 대기 후 성공/실패 판정)
    /// </summary>
    /// <returns></returns>
    private IEnumerator CallFlowCo()
    {
        yield return new WaitForSeconds(callDelay);

        bool hasAddress = perceptionManager.HasEvidence(addressEvidenceId);

        if (hasAddress)
        {
            messageText.text = "내집에서 나가"; //신고성공 기준 엔딩들어갈거라서
            SetState(PhoneState.Result, "신고 성공 : 주소 단서 보유");
            SoundManager.Instance.StopSfx();
            SoundManager.Instance.StopBgm();
            SoundManager.Instance.PlaySfxByName("CinematicSlow_SFX");

            //엔딩 진입 트리거
            if (EndingDirector.Instance != null)
            {
                EndingDirector.Instance.BeginEnding("112 신고 성공");
            }
        }
        else
        {
            SoundManager.Instance.PlaySfxByName("ErrorAlert_SFX");
            messageText.text = "우리집 주소가 뭐였지..?";
            SetState(PhoneState.Result, "신고 실패 : 주소 단서 없음");
        }

        RefreshUI();

        callRoutine = null;
    }

    /// <summary>
    /// 상태 변경 공통 처리
    /// </summary>
    /// <param name="newState"></param>
    /// <param name="reason"></param>
    private void SetState(PhoneState newState, string reason)
    {
        curState = newState;
    }

    /// <summary>
    /// UI갱신 (버튼/텍스트/키패드 노출)
    /// </summary>
    private void RefreshUI()
    {
        if (titleText != null)
        {
            titleText.text = GetTitleByState(curState);
        }

        if (inputText != null)
        {
            if (curState == PhoneState.Locked)
            {
                inputText.text = MaskText(passwordBuffer);
            }
            else
            {
                inputText.text = dialBuffer;
            }
        }
        
        if (messageText != null)
        {
            if (curState == PhoneState.Locked)
            {
                if (string.IsNullOrEmpty(messageText.text))
                {
                    messageText.text = "비밀번호를 입력해야 한다.";
                }
            }
            else if (curState == PhoneState.Dialing)
            {
                //기본 텍스트
                messageText.text = "경찰에 신고해야돼..!";
            }
            else if (curState == PhoneState.Calling)
            {
                messageText.text = "연결중...";
            }

            //result 상태관련 메세지는 CallFlowCo에서 세팅
        }

        if (unlockButton != null)
        {
            unlockButton.gameObject.SetActive(curState == PhoneState.Locked);
            unlockButton.interactable = (curState == PhoneState.Locked && passwordBuffer.Length > 0);
        }

        if (callButton != null)
        {
            callButton.gameObject.SetActive(curState == PhoneState.Dialing);
            callButton.interactable = (curState == PhoneState.Dialing && dialBuffer.Length > 0);
        }

        if (backSpaceButton != null)
        {
            bool allowBack = (curState == PhoneState.Locked || curState == PhoneState.Dialing);
            backSpaceButton.gameObject.SetActive(allowBack);
            backSpaceButton.interactable = allowBack;
        }

        if (keypadRoot != null)
        {
            keypadRoot.SetActive(curState == PhoneState.Locked || curState == PhoneState.Dialing);
        }
    }

    /// <summary>
    /// 상태별 타이틀 문자열 반환
    /// </summary>
    /// <param name="state"></param>
    private string GetTitleByState(PhoneState state)
    {
        if (state == PhoneState.Locked)
        {
            return "비밀번호 입력";
        }
        if (state == PhoneState.Dialing)
        {
            return "전화번호 입력";
        }
        if (state == PhoneState.Calling)
        {
            return "통화중";
        }

        return "---";
    }

    /// <summary>
    /// 비밀번호 표시용 마스킹 (****)
    /// </summary>
    /// <param name="raw"></param>
    private string MaskText(string raw)
    { 
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        int length = raw.Length;
        string masked = string.Empty;

        int i = 0;
        while (i < length)
        {
            masked += "*";
            i++;
        }

        return masked;
    }

    /// <summary>
    /// 문자열 마지막 글자 제거-백스페이스 클릭처리에서 사용
    /// </summary>
    /// <param name="raw"></param>
    private string RemoveLastChar(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }
        if (raw.Length <= 1)
        {
            return string.Empty;
        } 
        return raw.Substring(0, raw.Length - 1);
    }
}
