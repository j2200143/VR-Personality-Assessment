using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening; // DOTweenを使用するために追加
using TMPro;
/// <summary>
/// N5:Immoderation (衝動性) を測定するための「我慢の試練」タスクを管理するスクリプト
/// </summary>
public class ImmoderationManager : MonoBehaviour
{
    [Header("測定するファセット")]
    public PersonalityFacet personalityFacet = PersonalityFacet.N5_Immoderation;

    [Header("プレイヤーに最初に表示するメッセージ")]
    public string[] firstMessage = { "お仕事おつかれ様でした。報酬を選んでください", "今すぐ「確実な報酬」（50ゴールド）を受け取りますか？", "それとも、より高額な『我慢の報酬』（150ゴールド）に挑戦しますか？" };

    [Header("選択肢UI")]
    public GameObject buttonParentObject;
    public Button btnA, btnB;
    public TextMeshProUGUI btnAText, btnBText;
    public Text btnASubText, btnBSubText;
    public string btnAMessage = "確実な報酬（50ゴールド）";
    public string btnASubMessage = "※今すぐ受け取れる";
    public string btnBMessage = "我慢の報酬（150ゴールド）";
    public string btnBSubMessage = "※受け取るには、この部屋で1分間待機する必要がある";

    [Tooltip("選択肢A（確実）を選んだ際にプレイヤーに表示するメッセージ")]
    public string[] choiceAMessage = { "かしこまりました、報酬の50ゴールドをあなたのお財布に入れておきました", "コンテナの仕分けお疲れ様でした" };

    [Tooltip("選択肢B（我慢）を選んだ際にプレイヤーに表示するメッセージ")]
    public string[] choiceBMessage = { "よろしい。では、私が戻るまでの1分間、この部屋で待っていてください", "ただし、部屋の中央にある『ボタン』には決して押さないでください", "もし押してしまったら、報酬はゼロになります" };

    [Header("誘惑ボタン")]
    public Button temtationButton;
    public Transform touchTransform; // ボタンの可動部分
    public Transform afterTouchTransform; // 押し込まれた位置（目標地点）
    public GameObject warningUI; // 警告UI

    [Header("メッセージ（結果）")]
    public string[] completeMessage = { "一分が経過しました", "約束通り報酬の150ゴールドをあなたのお財布に入れておきました", "コンテナの仕分けお疲れ様でした" };
    public string[] noCompleteMessage_0 = { "ボタンを押さないでくださいと言ったばかりですよね？", "全く我慢できない人ですね", "約束通り報酬は没収です" };
    public string[] noCompleteMessage_1 = { "もう少し我慢してほしかったですね...", "報酬は没収です" };
    public string[] noCompleteMessage_2 = { "おしい！あと少しだったのに...", "残念ながら報酬は没収です" };

    [Header("効果音")]
    public AudioClip audioClip_Money;
    public AudioClip audioClip_TouchButton;
    public AudioClip audioClip_Alarm;

    [Header("設定")]
    public float countDownDuration = 60f; // カウントダウン時間（秒）

    // 内部変数
    private float elapsedCountDownTime = 0f; // 経過時間（秒）
    private bool isTimerRunning = false;
    private bool isTemptationTouched = false;



    void Start()
    {
        // 初期化
        btnA.onClick.AddListener(ChoiceA);
        btnAText.text = btnAMessage;
        btnASubText.text = btnASubMessage;

        btnB.onClick.AddListener(ChoiceB);
        btnBText.text = btnBMessage;
        btnBSubText.text = btnBSubMessage;

        temtationButton.onClick.AddListener(TouchTemptationButton);

        // 最初は非表示
        buttonParentObject.SetActive(false);
        warningUI.SetActive(false);
        // 誘惑ボタンも最初は押せないようにしておく（または非表示）
        temtationButton.interactable = false;
    }

    // 外部から呼び出し：タスク開始
    public void StartTask()
    {


        // 天の声のメッセージ表示
        StoryManager.Instance.StartMiddleDialogue(firstMessage, () =>
        {
            // メッセージ終了時
            buttonParentObject.SetActive(true);
        });
    }

    // 選択肢A（確実な報酬）を選んだ場合
    public void ChoiceA()
    {
        buttonParentObject.SetActive(false);

        // スコア送信(最低評価: 0点)
        // 衝動性が高い行動 = N5高
        PersonalityManager.Instance.AddFacetScore(personalityFacet, PersonalityManager.TASK_MAX_SCORE);

        // お金SE
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(audioClip_Money, 1f);

        // 天の声のメッセージ表示
        StoryManager.Instance.StartMiddleDialogue(choiceAMessage, () =>
        {
            // メッセージ終了時、次のシーンに移動
            StoryManager.Instance.MoveNextScene();
        });
    }

    // 選択肢B（我慢の報酬）を選んだ場合
    public void ChoiceB()
    {
        buttonParentObject.SetActive(false);

        // 天の声のメッセージ表示
        StoryManager.Instance.StartMiddleDialogue(choiceBMessage, () =>
        {
            // メッセージ終了時
            StartCoroutine(CountDownCoroutine());
        });
    }


    // カウントダウン処理
    private IEnumerator CountDownCoroutine()
    {
        isTimerRunning = true;
        elapsedCountDownTime = 0f;

        // 誘惑ボタンを有効化
        temtationButton.interactable = true;

        while (elapsedCountDownTime < countDownDuration)
        {
            // ボタンが押されたらループを抜ける
            if (isTemptationTouched)
            {
                isTimerRunning = false;
                yield break;
            }

            elapsedCountDownTime += Time.deltaTime;
            yield return null;
        }

        // 時間経過でクリア
        isTimerRunning = false;
        Complete();
    }

    // カウントダウン終了時の処理 (我慢に成功した場合)
    private void Complete()
    {
        // スコア送信 (最高評価)
        PersonalityManager.Instance.AddFacetScore(personalityFacet, 0);

        // 誘惑ボタンを無効化
        temtationButton.interactable = false;

        // お金SE (大金)
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(audioClip_Money, 1f);

        // 天の声のメッセージ表示
        StoryManager.Instance.StartMiddleDialogue(completeMessage, () =>
        {
            // メッセージ終了時
            StoryManager.Instance.MoveNextScene();
        });
    }

    // 誘惑ボタンに途中で触れた場合
    public void TouchTemptationButton()
    {
        if (!isTimerRunning) return; // タイマーが動いていないときは無視

        isTemptationTouched = true;
        temtationButton.interactable = false; // 二度押し防止

        // 評価ロジック (5段階評価のうちの中間3つ)
        //最大評価が4点の場合
        // 4点: 最初からAを選んだ (ChoiceAで処理済み)
        // 3点: 20秒未満
        // 2点: 20秒以上40秒未満
        // 1点: 40秒以上60秒未満
        // 0点: 60秒完遂 (Completeで処理済み)

        float oneThirdTime = countDownDuration / 3f;
        int baseScore = (int)((float)PersonalityManager.TASK_MAX_SCORE / 4);
        int score = 0;
        string[] message;

        // float計算の誤差を考慮しつつ判定
        if (elapsedCountDownTime < oneThirdTime)
        {
            score = baseScore * 3;
            message = noCompleteMessage_0;
        }
        else if (elapsedCountDownTime < oneThirdTime * 2)
        {
            score = baseScore * 2;
            message = noCompleteMessage_1;
        }
        else
        {
            score = baseScore;
            message = noCompleteMessage_2;
        }

        PersonalityManager.Instance.AddFacetScore(personalityFacet, score);

        StartCoroutine(HandleTouchButtonEvent(message));
    }

    // ボタンが押された場合の演出イベント
    private IEnumerator HandleTouchButtonEvent(string[] resultMessage)
    {
        // 1. ボタンSE
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(audioClip_TouchButton, 1f);

        // 2. ボタンのアニメーション (DOTween)
        // 押し込んで戻る
        if (touchTransform != null && afterTouchTransform != null)
        {
            Vector3 originalPos = touchTransform.position;
            Sequence seq = DOTween.Sequence();
            seq.Append(touchTransform.DOMove(afterTouchTransform.position, 0.2f));
            seq.Append(touchTransform.DOMove(originalPos, 0.2f));
            yield return seq.WaitForCompletion();
        }
        else
        {
            yield return new WaitForSeconds(0.5f); // アニメーションがない場合のウェイト
        }

        // 3. 警告演出
        if (AudioManager.Instance != null) AudioManager.Instance.PlayLoopingSoundSub(audioClip_Alarm);
        if (warningUI != null) warningUI.SetActive(true);

        // 5秒待機
        yield return new WaitForSeconds(5.0f);

        // 4. 演出終了
        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopingSoundSub();
        if (warningUI != null) warningUI.SetActive(false);

        // 5. 天の声のメッセージ表示
        StoryManager.Instance.StartMiddleDialogue(resultMessage, () =>
        {
            // メッセージ終了時
            StoryManager.Instance.MoveNextScene();
        });
    }
}