using UnityEngine;
using System.Collections;

// ★ ZoneTrigger.cs と共有するための public な enum
// プレイヤーの現在地を定義
public enum PlayerLocation
{
    None,   // どちらのエリアにもいない（中立エリア）
    Group,  // 集団エリア
    Solo    // 個人エリア
}

/// <summary>
/// E2:群居性の測定タスクを管理するクラス。
/// プレイヤーが各エリアに滞在した時間を測定します。
/// </summary>
public class GregariousnessTracker : MonoBehaviour
{
    [Header("タスク設定")]
    [Tooltip("タスクの総制限時間（秒）。プレイヤーには明示しない。")]
    public float totalTaskTime = 60f;
    [Tooltip("E2:群居性のファセット")]
    public PersonalityFacet facetToMeasure = PersonalityFacet.E2_Gregariousness;
    [Tooltip("次のタスクシーンに遷移する用")]
    public GameObject nextStoryObject;

    // --- 測定データ ---
    private float timeInGroupZone = 0f;
    private float timeInSoloZone = 0f;
    private float elapsedTaskTime = 0f;
    private float timeInNoneZone = 0f;
    // --- 内部状態 ---
    private PlayerLocation currentLocation = PlayerLocation.None;
    private bool isTaskRunning = false;

    // このスクリプトのシングルトンインスタンス（ZoneTriggerからアクセスするため）
    public static GregariousnessTracker Instance { get; private set; }

    void Awake()
    {
        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (nextStoryObject != null)
        {
            nextStoryObject.SetActive(false);
        }

        StartTask();
    }

    /// <summary>
    /// 測定タスクを開始します。
    /// </summary>
    public void StartTask()
    {
        elapsedTaskTime = 0f;
        timeInGroupZone = 0f;
        timeInSoloZone = 0f;
        currentLocation = PlayerLocation.None;
        isTaskRunning = true;
        Debug.Log("E2 (Gregariousness) Task Started.");
    }

    void Update()
    {
        // タスクが実行中でなければ何もしない
        if (!isTaskRunning) return;

        // --- 1. 総合タイマーを進める ---
        elapsedTaskTime += Time.deltaTime;

        // --- 2. 滞在エリアのタイマーを進める ---
        switch (currentLocation)
        {
            case PlayerLocation.Group:
                timeInGroupZone += Time.deltaTime;
                break;
            case PlayerLocation.Solo:
                timeInSoloZone += Time.deltaTime;
                break;
            case PlayerLocation.None:
                timeInNoneZone += Time.deltaTime;
                break;
        }

        // --- 3. 制限時間に達したらタスクを終了 ---
        if (elapsedTaskTime >= totalTaskTime)
        {
            EndTask();
        }
    }

    /// <summary>
    /// プレイヤーが指定されたエリアに入った時 (ZoneTriggerから呼ばれる)
    /// </summary>
    public void OnPlayerEnterZone(PlayerLocation zone)
    {
        if (!isTaskRunning) return;
        currentLocation = zone;
        Debug.Log($"Player entered {zone} zone.");
    }

    /// <summary>
    /// プレイヤーがエリアから出た時 (ZoneTriggerから呼ばれる)
    /// </summary>
    public void OnPlayerLeaveZone(PlayerLocation zone)
    {
        if (!isTaskRunning) return;

        // もしプレイヤーが出たのが「現在入っている」はずのゾーンなら、中立地帯に戻す
        if (currentLocation == zone)
        {
            currentLocation = PlayerLocation.None;
            Debug.Log($"Player left {zone} zone.");
        }
    }

    /// <summary>
    /// タスクを終了し、結果を集計・報告します。
    /// </summary>
    private void EndTask()
    {
        isTaskRunning = false;
        Debug.Log("E2 (Gregariousness) Task Ended.");

        float timeInNeutral = totalTaskTime - timeInGroupZone - timeInSoloZone;

        // --- 結果のログ出力 ---
        Debug.Log($"Time in Group: {timeInGroupZone:F2} seconds");
        Debug.Log($"Time in Solo: {timeInSoloZone:F2} seconds");
        Debug.Log($"Time in Neutral: {timeInNeutral:F2} seconds");

        // --- スコアの計算 (例：集団滞在時間の割合をスコアとする) ---
        // (0.0 ～ 1.0 の値になる)
        float gregariousnessScore = 0f;
        if (totalTaskTime > 0)
        {
            // (中立時間を除く、エリア滞在時間だけを分母にする場合)
            float totalZoneTime = timeInGroupZone + timeInSoloZone;
            if (totalZoneTime > 0)
            {
                gregariousnessScore = timeInGroupZone / totalZoneTime;
            }
            else
            {
                gregariousnessScore = 0f; // どちらにも入らなかった場合
            }
        }

        Debug.Log($"Final E2 Score (0.0=Solo, 1.0=Group): {gregariousnessScore:F3}");

        //スコア送信
        // 0.0～1.0 のスコアを 0.0～4.0 のスケールに変換
        float rawScore = gregariousnessScore * PersonalityManager.TASK_MAX_SCORE; // (例: 0.7 * 4 = 2.8)

        // 四捨五入して 0, 1, 2, 3, 4 の5段階に丸める
        int finalScore = Mathf.RoundToInt(rawScore); // (例: 2.8 → 3点)

        //スコア送信
        Debug.Log($"Final E2 Score (5-level): {finalScore}");
        PersonalityManager.Instance.AddFacetScore(facetToMeasure, finalScore);

        //スコア記録
        AnalyticsManager.Instance.LogTime(facetToMeasure, timeInGroupZone);

        // --- 次のタスクやシーンに進む処理 ---
        StoryManager.Instance.StartDialogue(false, StoryManager.Instance.isStoryVersion);
        if (nextStoryObject != null)
        {
            nextStoryObject.SetActive(true);
        }
    }
}
