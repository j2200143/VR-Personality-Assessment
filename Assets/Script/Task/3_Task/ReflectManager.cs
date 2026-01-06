using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using TMPro;

/// <summary>
/// N3（抑うつ）を測定する「反省会」タスクを管理するクラス。
/// 2段階の質問分岐により、0〜4点の5段階評価を行う。
/// </summary>
public class ReflectManager : MonoBehaviour
{
    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.N3_Depression;

    [Header("UI参照")]
    public GameObject resultCanvas; // 結果表示用Canvas（固定表示など）
    public Text resultText;         // 「魔法安定率：65%...」などを表示

    [Header("対話設定")]
    [Header("フェーズ1")]
    // フェーズ1: 結果の受け止め方
    public string[] phase1Message = { "ふむ...成功度65％ですか。悪くはないですが完璧でもないですね", "正直、この結果をあなたはどう思いますか？" };
    public GameObject choicePanel_Phase1;
    public Button btnA_Fail, btnB_External, btnC_Enough; // A:失敗, B:指示のせい, C:十分
    public string[] choiceMessage_Phase1 = { "失敗だ。やっぱり私はだめだ", "まあ、こんなものだろう。指示も難しかったし", "ベストを尽くしたから十分な結果だ" };
    public TextMeshProUGUI[] choiceText_Phase1;
    [Header("フェーズ2A")]
    // フェーズ2A: 失敗を選んだ後の深掘り
    public string[] phase2AMessage = { "そう卑下することもないですよ。なぜそう思うのですか？" };
    public GameObject choicePanel_Phase2A;
    public Button btnA1_Always, btnA2_Mistake; // A1:いつもこうだ(4点), A2:今回はミス(3点)
    public string[] choiceMessage_Phase2_A = { "私は何をやっても上手くいかない", "今回は判断ミスをした。次は気をつける" };
    public TextMeshProUGUI[] choiceText_Phase2_A;
    [Header("フェーズ2B")]
    // フェーズ2C: 十分を選んだ後の深掘り
    public string[] phase2CMessage = { "ほう、その自信はどこから来るのですか？" };
    public GameObject choicePanel_Phase2C;
    public Button btnC1_SelfAccept, btnC2_Switch; // C1:自分に満足(0点), C2:気にしない(1点)
    public string[] choiceMessage_Phase2_C = { "結果はどうあれ、自分のできることをやった", "まあ、終わったことを気にしても仕方ない" };
    public TextMeshProUGUI[] choiceText_Phase2_C;

    // 終了メッセージ
    public string[] endMessage = { "なるほど、あなたの考えは分かりました", "" };

    //一段階のスコア
    private int oneStepScore = 0;

    void Start()
    {
        // ボタンイベント登録
        if (btnA_Fail != null) btnA_Fail.onClick.AddListener(OnSelect_Phase1_A);
        if (btnB_External != null) btnB_External.onClick.AddListener(OnSelect_Phase1_B);
        if (btnC_Enough != null) btnC_Enough.onClick.AddListener(OnSelect_Phase1_C);

        oneStepScore = (int)((float)PersonalityManager.TASK_MAX_SCORE / 4);
        if (btnA1_Always != null) btnA1_Always.onClick.AddListener(() => SubmitScoreAndEnd(PersonalityManager.TASK_MAX_SCORE));
        if (btnA2_Mistake != null) btnA2_Mistake.onClick.AddListener(() => SubmitScoreAndEnd(3 * oneStepScore));

        if (btnC1_SelfAccept != null) btnC1_SelfAccept.onClick.AddListener(() => SubmitScoreAndEnd(0));
        if (btnC2_Switch != null) btnC2_Switch.onClick.AddListener(() => SubmitScoreAndEnd(oneStepScore));

        // 初期化：非表示
        if (choicePanel_Phase1 != null) choicePanel_Phase1.SetActive(false);
        if (choicePanel_Phase2A != null) choicePanel_Phase2A.SetActive(false);
        if (choicePanel_Phase2C != null) choicePanel_Phase2C.SetActive(false);
        if (resultCanvas != null) resultCanvas.SetActive(false);

        //選択肢
        for (int i = 0; i < choiceText_Phase1.Length; i++)
        {
            choiceText_Phase1[i].text = choiceMessage_Phase1[i];
        }
        for (int i = 0; i < choiceText_Phase2_A.Length; i++)
        {
            choiceText_Phase2_A[i].text = choiceMessage_Phase2_A[i];
        }
        for (int i = 0; i < choiceText_Phase2_C.Length; i++)
        {
            choiceText_Phase2_C[i].text = choiceMessage_Phase2_C[i];
        }
    }

    /// <summary>
    /// ポーション作成後に呼ばれる
    /// </summary>
    public void StartTask()
    {
        // 結果Canvas表示
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(true);
            if (resultText != null) resultText.text = "魔法安定率：65%\n（評価：中の下）";
        }

        // このタスクが実行されたことを登録
        if (PersonalityManager.Instance != null)
            PersonalityManager.Instance.RegisterExecutedTask(personalityFacet);

        // フェーズ1開始
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.StartMiddleDialogue(phase1Message, () =>
            {
                choicePanel_Phase1.SetActive(true);
            });
        }
    }

    // --- フェーズ1の選択処理 ---

    // A: 「失敗です...」 -> フェーズ2Aへ
    private void OnSelect_Phase1_A()
    {
        choicePanel_Phase1.SetActive(false);
        StoryManager.Instance.StartMiddleDialogue(phase2AMessage, () =>
        {
            choicePanel_Phase2A.SetActive(true);
        });
    }

    // B: 「指示が悪い」 -> 終了 (2点)
    private void OnSelect_Phase1_B()
    {
        choicePanel_Phase1.SetActive(false);
        // 外的帰属は中間的評価 (2点)
        SubmitScoreAndEnd(2 * oneStepScore);
    }

    // C: 「十分です」 -> フェーズ2Cへ
    private void OnSelect_Phase1_C()
    {
        choicePanel_Phase1.SetActive(false);
        StoryManager.Instance.StartMiddleDialogue(phase2CMessage, () =>
        {
            choicePanel_Phase2C.SetActive(true);
        });
    }

    // --- スコア送信と終了 ---

    private void SubmitScoreAndEnd(int score)
    {
        // パネルを閉じる
        if (choicePanel_Phase2A != null) choicePanel_Phase2A.SetActive(false);
        if (choicePanel_Phase2C != null) choicePanel_Phase2C.SetActive(false);
        if (resultCanvas != null) resultCanvas.SetActive(false);

        // スコア送信
        Debug.Log($"N3 Score Sent: {score}");
        if (PersonalityManager.Instance != null)
        {
            PersonalityManager.Instance.AddFacetScore(personalityFacet, score);
        }

        // 終了メッセージを表示してシーン移動
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.StartMiddleDialogue(endMessage, () =>
            {
                StoryManager.Instance.MoveNextScene();
            });
        }
    }
}