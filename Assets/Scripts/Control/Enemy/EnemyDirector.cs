using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 루프 매니저의 침입 이벤트를 받아, 괴한스폰을 수행하는 연출 감독역할
/// -침입 성공시 에너미 프리팹 스폰 후 연출 -> 추적시작 흐름
/// -루프 리셋/시작시에는 기존 에너미 프리팹 제거(매 루프마다 초기화)
/// 
/// -[디자인 패턴 활용]
/// -여기서 드디어 옵저버 활용, 루프매니저의 이벤트 구독, 느슨한 결합위함
/// -책임 분리(SRP)->루프매니저는 시간/상태, 여기 에너미디렉터는 연출/스폰만 담당
/// </summary>
public class EnemyDirector : MonoBehaviour
{
    [Header("루프매니저 참조")]
    [SerializeField] private LoopManager loopManager;

    [Header("플레이어 트랜스폼")]
    [SerializeField] private Transform playerTransform;

    [Header("괴한 프리팹")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("스폰위치")]
    [SerializeField] private Transform enemySpawnPoint;

    //현재 소환된 괴한 프리팹 저장용
    private GameObject curEnemy;

    /// <summary>
    /// 이벤트 등록
    /// </summary>
    private void OnEnable()
    {
        loopManager.LoopStarted += OnLoopStarted;
        loopManager.BreakInSucceeded += OnBreakInSucceeded;
        loopManager.BreakInFailedByBattery += OnBreakInFailedByBattery;
    }

    /// <summary>
    /// 이벤트 해제
    /// </summary>
    private void OnDisable()
    {
        loopManager.LoopStarted -= OnLoopStarted;
        loopManager.BreakInSucceeded -= OnBreakInSucceeded;
        loopManager.BreakInFailedByBattery -= OnBreakInFailedByBattery;
    }

    /// <summary>
    /// 루프 시작되면 이전 루프의 괴한은 제거
    /// </summary>
    /// <param name="loopCount"></param>
    private void OnLoopStarted(int loopCount)
    {
        DeSpawnEnemy();
    }

    /// <summary>
    /// 침입 성공시 괴한 스폰, 연출 후에 추격 시작
    /// </summary>
    /// <param name="method"></param>
    private void OnBreakInSucceeded(LoopManager.BreakInMethod method)
    {
        Debug.Log("[EnemyDirector] 침입 성공 이벤트 수신" + method);

        if (curEnemy != null) return;
        SpawnEnemy(method);
    }

    /// <summary>
    /// [연출용] 배터리 제거로 1차 침입이 실패했을때,
    /// 괴한의 대사 출력(임시로 디버그 로그)
    /// 추후 여기서 도어락 흔들림 사운드, 노크, 보이스, 재생으로 교체할 것
    /// </summary>
    /// <param name="emergencyTimeSeconds"></param>
    private void OnBreakInFailedByBattery(float emergencyTimeSeconds)
    {
        Debug.Log("[EnemyDirector] 괴한 대사 : 뭐야? 배터리 다 됐어? 미치겠네..");
    }

    /// <summary>
    /// 괴한 스폰 로직
    /// 지정 프리팹 으로 지정위치에 생성
    /// 생성된 프리팹에 에너미컨트롤에서 참조 주입, 추격로직 대본쥐어주기(상황 별 다르게, 현재 임시로 디버그로그)
    /// </summary>
    private void SpawnEnemy(LoopManager.BreakInMethod method)
    {
        curEnemy = Instantiate(enemyPrefab,enemySpawnPoint.position,enemySpawnPoint.rotation);
        EnemyControl enemyControl = curEnemy.GetComponent<EnemyControl>();
        enemyControl.Initialize(playerTransform, loopManager);

        if (method == LoopManager.BreakInMethod.DoorLock)
            enemyControl.SetEntranceText("내 집에서 나가!!");
        else 
        { 
            enemyControl.SetEntranceText("비상키가 있어서 다행이지..\n 뭐야! 당신 누구야!"); 
        }
        enemyControl.BeginEntranceChase();
    }

    /// <summary>
    /// 괴한 프리팹 제거(기존루프 배우 해고용)
    /// </summary>
    private void DeSpawnEnemy()
    {
        if (curEnemy == null) return;
        Destroy(curEnemy);
        curEnemy = null;
    }
}
