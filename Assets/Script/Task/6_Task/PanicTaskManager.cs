using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
/// <summary>
/// N6（もろさ）を測定する「パニックの試練」全体を管理するクラス。
/// </summary>
public class PanicTaskManager : MonoBehaviour
{
    public static PanicTaskManager Instance { get; private set; }

    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.N6_Vulnerability;

    [Header("UI参照")]
    public Text timerText;       // タイム表示用（フェーズ2で使用）
    public GameObject warningUI; // 画面を赤く点滅させる等の演出UI

    [Header("プレイヤーに表示するメッセージ")]
    public string[] startPhase2Messages = { "警告！防衛システム作動！全力で突破してください！" };

    [Header("演出用")]
    public AudioClip alarmSound;
    public Transform phase1ExitDoorRight, phase1ExitDoorLeft; // 急に閉まる扉
    public Transform endPositionRight1, endPositionLeft1;//閉まった際の場所
    public Transform phase2ExitDoorRight, phase2ExitDoorLeft; // 徐々に閉まる扉
    public Transform endPositionRight2, endPositionLeft2;//閉まった際の場所

    [Header("制限時間内に脱出できなかった場合")]
    public GameObject nextStoryGoalObject;
    public string[] timeUpMessages = { "扉が完全に閉じられてしまいましたね", "あちらにワープゾーンを出現させたので使ってください" };
    [Header("制限時間内に脱出できた場合")]
    public string[] clearMessages = { "無事脱出できましたね！", "あちらにワープゾーンを出現させたので使ってください" };
    // --- 測定データ ---
    // フェーズ1（ベースライン）
    private float phase1Time = 0f;//クリアタイム
    private int phase1Errors = 0;
    private float phase1DoorCloseDuration = 0.5f;// フェーズ1のドアが閉まる時間（急に閉まる演出用）

    // フェーズ2（ストレス）
    private float phase2Time = 0f;//クリアタイム
    private int phase2Errors = 0;

    // 内部状態
    private bool isPhase1Running = false;
    private bool isPhase2Running = false;
    private float phase2TimeLimit = 0f;//フェーズ2の制限時間：フェーズ１でのクリアタイムにプラス8秒に設定する
    private bool isScored = false;//万が一のスコア送信重複防止

    void Awake()
    {
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
        if (timerText != null)
            timerText.text = "";
    }

    void Update()
    {
        if (isPhase1Running)
        {
            phase1Time += Time.deltaTime;
        }

        if (isPhase2Running)
        {
            phase2Time += Time.deltaTime;

            // フェーズ2の演出：カウントダウン表示
            float remainingTime = phase2TimeLimit - phase2Time;
            if (timerText != null)
            {
                timerText.text = $"脱出まで: {remainingTime:F1}";
                if (remainingTime < 10f) timerText.color = Color.red;
            }

            //制限時間以内に脱出できなかった場合
            if (remainingTime < 0f)
            {
                TimeUp();
                isPhase2Running = false;
            }
        }
    }

    // 障害物から呼ばれる：エラーカウント
    public void AddErrorCount()
    {
        if (isPhase1Running)
        {
            phase1Errors++;
        }
        else if (isPhase2Running)
        {
            phase2Errors++;
            // ストレス時は追加で画面を揺らす、ノイズを走らせるなどの演出を入れても良い
        }
    }

    // エリアトリガーから呼ばれる：フェーズ進行制御
    public void OnAreaTriggerEnter(PanicTaskAreaTrigger.TriggerType type)
    {
        switch (type)
        {
            case PanicTaskAreaTrigger.TriggerType.StartPhase1:
                StartPhase1();
                break;

            case PanicTaskAreaTrigger.TriggerType.EndPhase1:
                EndPhase1();
                break;

            case PanicTaskAreaTrigger.TriggerType.StartPhase2:
                StartPhase2();
                break;

            case PanicTaskAreaTrigger.TriggerType.EndPhase2:
                EndPhase2();
                break;
        }
    }

    // --- フェーズ処理 ---

    private void StartPhase1()
    {
        Debug.Log("Phase 1 Started");
        isPhase1Running = true;
    }

    private void EndPhase1()
    {
        Debug.Log($"Phase 1 Ended. Time: {phase1Time}, Errors: {phase1Errors}");
        isPhase1Running = false;

        //メッセージを読み切ってから計測を始めるために中間地点に到達時にメッセージ、ストレス演出を開始する
        // ストレス演出開始
        // 天の声
        StoryManager.Instance.StartMiddleDialogue(startPhase2Messages);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLoopingSoundSub(alarmSound);
        }
        if (warningUI != null) warningUI.SetActive(true); // 赤点滅など
        if (timerText != null) timerText.gameObject.SetActive(true);

        // phase1ExitDoorRightとphase1ExitDoorLeftをendPositionRight1,endPositionLeft1に動かす
        // 「急に閉まる」ため、短い時間(0.5秒)で移動させます。
        if (phase1ExitDoorRight != null && endPositionRight1 != null)
        {
            phase1ExitDoorRight.DOMove(endPositionRight1.position, phase1DoorCloseDuration)
                .SetEase(Ease.InQuad); // バタンと閉まる感じ
        }
        if (phase1ExitDoorLeft != null && endPositionLeft1 != null)
        {
            phase1ExitDoorLeft.DOMove(endPositionLeft1.position, phase1DoorCloseDuration)
                .SetEase(Ease.InQuad);
        }

        //フェーズ2の制限時間を決定
        phase2TimeLimit = phase1Time + 10f;
    }

    private void StartPhase2()
    {
        Debug.Log("Phase 2 Started (STRESS MODE)");
        isPhase2Running = true;

        // phase2ExitDoorRightとphase2ExitDoorLeftをendPositionRight2,endPositionLeft2に
        // phase2TimeLimitかけて動かす.+5秒している理由は制限時間に到達していないのにも関わらずプレイヤーが通れないことがないようにするため
        // 制限時間いっぱいかけて徐々に閉まるため、Ease.Linear（等速）を使用します
        float doorCloseTime = phase2TimeLimit + 4f;
        if (phase2ExitDoorRight != null && endPositionRight2 != null)
        {
            phase2ExitDoorRight.DOMove(endPositionRight2.position, doorCloseTime)
                .SetEase(Ease.Linear);
        }
        if (phase2ExitDoorLeft != null && endPositionLeft2 != null)
        {
            phase2ExitDoorLeft.DOMove(endPositionLeft2.position, doorCloseTime)
                .SetEase(Ease.Linear);
        }
    }

    private void EndPhase2()
    {
        Debug.Log($"Phase 2 Ended. Time: {phase2Time}, Errors: {phase2Errors}");
        isPhase2Running = false;

        // 演出停止
        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopingSoundSub();
        if (warningUI != null) warningUI.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);

        // 結果計算とスコア送信
        CalculateAndSendScore();

        //プレイヤーにメッセージ表示
        StoryManager.Instance.StartMiddleDialogue(clearMessages);
    }

    // --- 評価とスコア送信 ---
    private void CalculateAndSendScore()
    {
        if (!isScored)
        {
            isScored = true;
            // エラー回数の差分 (ストレス時 - 平常時)
            // ※フェーズ2の方が焦ってミスが増える(=N6高い)と想定
            int errorDiff = phase2Errors - phase1Errors;
            // マイナス（フェーズ2の方がミスが減った）場合は0として扱う
            if (errorDiff < 0) errorDiff = 0;

            Debug.Log($"Error Diff: {errorDiff} (P1:{phase1Errors} -> P2:{phase2Errors})");

            // 評価基準（仮）に基づいてスコア化
            // 差分が大きいほど N6（もろさ）が高い
            int score = 0;

            if (errorDiff <= 1)
            {
                score = 0; // パフォーマンス維持（N6低）
            }
            else if (errorDiff == 2)
            {
                score = 1; // 小程度の影響
            }
            else if (errorDiff == 3)
            {
                score = 2; // 中程度の影響
            }
            else if (errorDiff == 4)
            {
                score = 3; // 大程度の影響
            }
            else
            {
                score = PersonalityManager.TASK_MAX_SCORE; // とても大きなパフォーマンス低下（N6高）
            }

            // PersonalityManagerへ送信
            if (PersonalityManager.Instance != null)
            {
                PersonalityManager.Instance.AddFacetScore(personalityFacet, score);
            }

        }
    }

    //制限時間以内に脱出できなかった場合
    private void TimeUp()
    {
        if (!isScored)
        {
            // PersonalityManagerへ送信
            if (PersonalityManager.Instance != null)
            {
                isScored = true;
                PersonalityManager.Instance.AddFacetScore(personalityFacet, PersonalityManager.TASK_MAX_SCORE);
            }
        }

        //プレイヤーにメッセージ表示
        nextStoryGoalObject.SetActive(true);
        StoryManager.Instance.StartMiddleDialogue(timeUpMessages);
        //次のシーンで音が鳴ることを防ぐ
        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopingSoundSub();
    }
}