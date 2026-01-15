using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 이동 전용 스크립트
/// 유니티 내장기능 캐릭터 컨트롤러 사용(물리기반x),탑뷰 WASD 기준
/// -네비메쉬 이동은 추후 추가 가능성은 있음 일단 보류
/// -나중에 최종페이즈 1인칭으로 변경될때도 재사용 염두할 것
/// </summary>
public class PlayerControl : MonoBehaviour
{
    [Header("필수 참조(눈 위치 Pivot)")]
    [SerializeField] private Transform eyePivot;

    [Header("마우스 감도")]
    [SerializeField] private float sensitivityX = 100.0f;
    [SerializeField] private float sensitivityY = 100.0f;

    [Header("Pitch(상하) 제한")]
    [SerializeField] private float minPitch = -70.0f;
    [SerializeField] private float maxPitch = 70.0f;

    [Header("이동 속도")]
    [SerializeField] private float moveSpeed = 8.0f;

    [Header("중력(간단 구현용)")]
    [SerializeField] private float gravity = -20.0f;

    [Header("커서 제어")]
    [SerializeField] private bool lockCursorOnEnable = true;

    private CharacterController characterController;

    //현재 누적 Pitch 값(상하 회전 누적/클램프용)
    private float pitch;

    //CharacterController용 수직 속도(중력)
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        //필수 참조 방어
        if (eyePivot == null)
        {
            Debug.LogError("[PlayerFpsController] eyePivot이 null. Player 하위 눈 위치 오브젝트를 연결필요");
            enabled = false;
            return;
        }

        //시작 시 현재 eyePivot 로컬 x각을 pitch로 동기화
        Vector3 localEuler = eyePivot.localEulerAngles;
        pitch = NormalizeAngle(localEuler.x);
    }

    private void OnEnable()
    {
        //FPS 모드에서만 커서 잠금
        if (lockCursorOnEnable)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        //enable될 때 수직 속도 초기화
        verticalVelocity = 0.0f;
    }

    private void OnDisable()
    {
        //탑뷰로 돌아갈 때 커서 사용 가능하게 풀어줌
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMove();
    }

    /// <summary>
    ///마우스 룩 처리
    ///-Yaw : 플레이어 루트 회전
    ///-Pitch: eyePivot 로컬 회전
    /// </summary>
    private void HandleMouseLook()
    {
        float dt = Time.deltaTime;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        //좌우(Yaw): 몸 방향 회전
        float yawDelta = mouseX * sensitivityX * dt;
        transform.Rotate(0.0f, yawDelta, 0.0f);

        //상하(Pitch): 눈 Pivot 회전
        float pitchDelta = mouseY * sensitivityY * dt;
        pitch -= pitchDelta;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        eyePivot.localRotation = Quaternion.Euler(pitch, 0.0f, 0.0f);
    }

    /// <summary>
    ///이동 처리
    ///-이동 기준은 바라보는 방향(eyePivot) 기반
    ///-pitch는 이동에 섞지 않기 위해 y를 제거한 forward/right를 사용
    /// </summary>
    private void HandleMove()
    {
        float dt = Time.deltaTime;

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        //eyePivot.forward/right를 사용, y성분 제거하고 이동 벡터 구성
        Vector3 forward = eyePivot.forward;
        forward.y = 0.0f;

        Vector3 right = eyePivot.right;
        right.y = 0.0f;

        //혹시 forward/right가 0벡터가 될수도 있음. 극단적인 상황 방어
        if (forward.sqrMagnitude > 0.0001f) forward.Normalize();
        if (right.sqrMagnitude > 0.0001f) right.Normalize();

        Vector3 move = (right * inputX) + (forward * inputZ);

        //대각선 속도 보정
        if (move.sqrMagnitude > 1.0f)
        {
            move.Normalize();
        }

        //중력 처리(CharacterController용)
        if (characterController.isGrounded)
        {
            //바닥에 붙게 살짝 음수로
            if (verticalVelocity < 0.0f) verticalVelocity = -1.0f;
        }
        else
        {
            verticalVelocity += gravity * dt;
        }

        Vector3 velocity = (move * moveSpeed) + (Vector3.up * verticalVelocity);
        characterController.Move(velocity * dt);
    }

    /// <summary>
    ///Unity Euler(0~360)을 -180~180 범위로 정규화
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        if (angle > 180.0f)
        {
            angle -= 360.0f;
        }
        return angle;
    }
}
