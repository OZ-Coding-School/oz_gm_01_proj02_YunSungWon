using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임의 핵심 시스템 (타이머/침입/리셋) 관리
/// 1.루프 시작후 침입 타이밍 18초+-2초(16~20초)로 랜덤 설정
/// 2.시간이 지나면 침입 이벤트 발생-01/06(day1) 일단 로그만 띄우게 구현
/// 3.특정 조건(현관문 트리거 / 플레이어 사망)에서 루프 리셋 - 01/06(day1) 플레이어 사망은 일단 키 입력으로 테스트
/// 
/// [사용할 기술]
/// -상태관리관련 간단한 플래그 전환
/// -이벤트 연출 포인트
/// </summary>
public class LoopManager : MonoBehaviour
{
    [Header("플레이어 참조")]
    [SerializeField] private Transform playerTransform;

    [Header("루프 시작 위치")]
    [SerializeField] private Transform loopStartPoint;

    [Header("침입 시간 설정(최소/최대)")]
    [SerializeField] private float breakInMinSeconds = 16.0f;
    [SerializeField] private float breakInMaxSeconds = 20.0f;

    private float elapsedSeconds;
    private float breakInSeconds;
    private int loopCount;
    private bool breakInTriggered;

    //외부 접근용 프로퍼티
    public float ElapsedSeconds { get { return elapsedSeconds; } }
    public float BreakInSeconds { get { return breakInSeconds; } }
    public int LoopCount { get { return loopCount; } }
    public bool BreakInTriggered { get { return breakInTriggered; } }

    private void Start()
    {
        //첫 루프 시작
        StartNewLoop();
    }

    private void Update()
    {
        elapsedSeconds += Time.deltaTime;

        //침입 이벤트 트리거
        if (!breakInTriggered && elapsedSeconds >= breakInSeconds)
        {
            breakInTriggered = true;
            OnBreakInTriggered();
        }

        //사망 트리거 들어갈곳(일단 테스트용)
        if (Input.GetKeyDown(KeyCode.K))
        {
            ResetLoop("테스트 사망처리");
        }
    }

    /// <summary>
    /// 새 루프 시작 : 타이머/침입
    /// </summary>
    private void StartNewLoop()
    {
        loopCount += 1;

        elapsedSeconds = 0.0f;
        breakInSeconds = Random.Range(breakInMinSeconds, breakInMaxSeconds);
        breakInTriggered = false;

        if (playerTransform != null && loopStartPoint != null)
        {
            this.playerTransform.position = this.loopStartPoint.position;
            this.playerTransform.rotation = this.loopStartPoint.rotation;
        }

        OnLoopStart();
    }

    /// <summary>
    /// 루프 리셋 후 루프이유표시, 즉시 새 루프 시작
    /// </summary>
    /// <param name="reason"></param>
    public void ResetLoop(string reason)
    {
        Debug.Log("[LoopManager] 루프 리셋됨 루프된 이유 =" + reason);
        StartNewLoop();
    }

    /// <summary>
    /// [연출 훅] 루프 시작 지점(나중에 카메라/사운드/텍스트 붙일 자리)
    /// </summary>
    private void OnLoopStart()
    {
        Debug.Log("[LoopManager] 루프 시작됨 현재루프 =" + loopCount + "침입시간 = " + breakInSeconds.ToString("F2")+"s");
    }

    /// <summary>
    /// [연출 훅] 침입 발생 시점(나중에 도어락 사운드/라이트/괴한 스폰을 붙일 자리)
    /// </summary>
    private void OnBreakInTriggered()
    {
        Debug.Log("[LoopManager] 침입 트리거 발생시점" + elapsedSeconds.ToString("F2") + "s");
    }
}
