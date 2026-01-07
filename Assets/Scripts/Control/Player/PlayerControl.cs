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
    [Header("이동 속도")]
    [SerializeField] private float moveSpeed = 1.0f;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0.0f, vertical);

        //대각선 속도 보정
        if (input.sqrMagnitude > 1.0f) input.Normalize();

        Vector3 velocity = input * moveSpeed;

        //중력 설정 - 일단 약하게 (바닥 접지용)
        velocity.y = -5.0f;

        characterController.Move(velocity * Time.deltaTime);
    }
}
