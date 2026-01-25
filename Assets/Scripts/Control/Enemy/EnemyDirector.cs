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
/// -IResetTable : 루프 시작시 ResetState 로 일괄 초기화
/// 
/// --정리--
/// 루프 매니저의 이벤트를 받아 괴한 스폰/제거 를 수행하는 연출 감독역할
/// 루프매니저(시간/상태)와 EnemyControl(행동)을 느슨하게 결합(옵저버)
/// -에너미 프리팹이 씬 오브젝트를 직접 참조 못하는 문제를 해결-각 참조요소들 여기서 주입
/// 
/// --지금 유니티 라이프 사이클 관련, 참조 주입이 제대로 안되고 있음
/// -주입이 되기도전에, 프리팹이 생성되어버려서, 문제
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

    [Header("현관문 컨트롤러")]
    [SerializeField] private FrontDoorControl frontDoorControl;

    [Header("비상키 침입 시 잠금해제 연출시간(임시)")]
    [SerializeField] private float emergencyUnlockDelay = 1.5f;

    [Header("현관문 열기 연출 전 딜레이(임시)")]
    [SerializeField] private float doorOpenDelay = 0.2f;

    [Header("화장실 문 체크 포인트")]
    [SerializeField] private Transform bathRoomDoorPoint;

    [Header("화장실 문 컨트롤러")]
    [SerializeField] private BathRoom_DoorControl bathRoomDoorControl;

    [Header("부엌 열쇠 탐색 포인트")]
    [SerializeField] private Transform kitchenKeySearchPoint;

    [Header("소파 감시 포인트")]
    [SerializeField] private Transform sofaWatchPoint;

    [Header("부엌 진입 포인트")]
    [SerializeField] private Transform kitchenEntryPoint;

    //현재 소환된 괴한 프리팹 저장용
    private GameObject curEnemy;

    //라스트 페이즈 동안 루프 괴한 스폰/연출 차단 플래그
    private bool isLoopEnemyBlocked;

    //현관문 연출 코루틴 핸들
    private Coroutine entranceDoorFlowRoutine;

    private void Awake()
    {
        if (loopManager == null) loopManager = LoopManager.Instance;
    }

    /// <summary>
    /// 이벤트 등록 + 리셋 테이블 등록
    /// </summary>
    private void OnEnable()
    {
        loopManager.LoopStarted += OnLoopStarted;
        loopManager.BreakInSucceeded += OnBreakInSucceeded;
        loopManager.BreakInFailedByBattery += OnBreakInFailedByBattery;
    }

    /// <summary>
    /// 이벤트 해제 + 리셋 테이블 해제
    /// </summary>
    private void OnDisable()
    {
        loopManager.LoopStarted -= OnLoopStarted;
        loopManager.BreakInSucceeded -= OnBreakInSucceeded;
        loopManager.BreakInFailedByBattery -= OnBreakInFailedByBattery;
    }

    private void OnLoopStarted(int loopCount)
    {
        if (isLoopEnemyBlocked) return;
        DeSpawnEnemy();
    }

    /// <summary>
    /// 침입 성공시 괴한 스폰, 연출 후에 추격 시작
    /// </summary>
    /// <param name="method"></param>
    private void OnBreakInSucceeded(LoopManager.BreakInMethod method)
    {
        if (isLoopEnemyBlocked) return;

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
        if (isLoopEnemyBlocked) return;
        SoundManager.Instance.PlaySfxByName("DoorTryLock_SFX");
    }

    /// <summary>
    /// 괴한 스폰 로직
    /// 지정 프리팹 으로 지정위치에 생성
    /// 생성된 프리팹에 에너미컨트롤에서 참조 주입, 추격로직 대본쥐어주기(상황 별 다르게, 현재 임시로 디버그로그)
    /// </summary>
    private void SpawnEnemy(LoopManager.BreakInMethod method)
    {
        if (enemyPrefab == null || enemySpawnPoint == null)
        {
            return;
        }

        curEnemy = Instantiate(enemyPrefab,enemySpawnPoint.position,enemySpawnPoint.rotation);

        EnemyControl enemyControl = curEnemy.GetComponent<EnemyControl>();
        EnemyVisionSensor sensor = curEnemy.GetComponent<EnemyVisionSensor>();

        //참조 주입 구간
        enemyControl.Initialize(playerTransform, loopManager, bathRoomDoorControl, sensor);

        //각 포인트 여기서 주입
        enemyControl.SetSearchPoints(bathRoomDoorPoint, kitchenKeySearchPoint, sofaWatchPoint, kitchenEntryPoint);

        if (method == LoopManager.BreakInMethod.DoorLock)
            enemyControl.SetEntranceText("내 집에서 나가!!");
        else 
        { 
            enemyControl.SetEntranceText("비상키가 있어서 다행이지..\n 뭐야! 당신 누구야!"); 
        }
        enemyControl.BeginScenario();

        if (entranceDoorFlowRoutine != null)
        {
            StopCoroutine(entranceDoorFlowRoutine);
            entranceDoorFlowRoutine = null;
        }

        entranceDoorFlowRoutine = StartCoroutine(EntranceDoorFlowCo(method));
    }

    private IEnumerator EntranceDoorFlowCo(LoopManager.BreakInMethod method)
    {
        if (frontDoorControl == null)
        {
            yield break;
        }

        //이미 부숴졌거나 열려있으면 스킵
        if (frontDoorControl.CurState == FrontDoorControl.FrontDoorState.Broken ||
            frontDoorControl.CurState == FrontDoorControl.FrontDoorState.Open)
        {
            yield break;
        }

        //도어락 연동부분-도어락 해제->열림
        if (method == LoopManager.BreakInMethod.DoorLock)
        {
            //잠겨 있으면 "잠금해제"연출 
            if (frontDoorControl.CurState == FrontDoorControl.FrontDoorState.Locked)
            {
                frontDoorControl.EnemyTryUnlock("도어락 침입");
            }

            SoundManager.Instance.PlaySfxByName("DoorLockPW_SFX");
            //문 여는 템포
            if (doorOpenDelay > 0.0f) yield return new WaitForSeconds(doorOpenDelay);
            SoundManager.Instance.PlaySfxByName("DoorOpen_SFX");

            frontDoorControl.EnemyTryOpenDoor("도어락 침입");
            yield break;
        }

        //EmergncyKey 연동부분 : 기다렸다가 잠금해제 후 열기
        if (method == LoopManager.BreakInMethod.EmergencyKey)
        {
            if (emergencyUnlockDelay > 0.0f) yield return new WaitForSeconds(emergencyUnlockDelay);

            if (frontDoorControl.CurState == FrontDoorControl.FrontDoorState.Locked)
            {
                frontDoorControl.EnemyTryUnlock("비상키 침입");
            }

            SoundManager.Instance.PlaySfxByName("DoorTryLock_SFX");
            if(doorOpenDelay>0.0f) yield return new WaitForSeconds(doorOpenDelay);
            SoundManager.Instance.PlaySfxByName("DoorOpen_SFX");

            frontDoorControl.EnemyTryOpenDoor("비상키 침입");
        }
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

    /// <summary>
    /// 외부(라스트페이즈용)에서 괴한 강제 제거
    /// </summary>
    /// <param name="reason"></param>
    public void ForceDespawnEnemy(string reason)
    {
        DeSpawnEnemy();
    }

    /// <summary>
    /// 라스트페이즈 진입 시 루프괴한의 스폰,연출 원천 차단용
    /// </summary>
    /// <param name="blocked"></param>
    /// <param name="reason"></param>
    public void SetLoopEnemyBlocked(bool blocked, string reason)
    {
        isLoopEnemyBlocked = blocked;

        if (blocked)
        {
            if (entranceDoorFlowRoutine != null)
            {
                StopCoroutine(entranceDoorFlowRoutine);
                entranceDoorFlowRoutine = null;
            }

            DeSpawnEnemy();
        }
    }
}
