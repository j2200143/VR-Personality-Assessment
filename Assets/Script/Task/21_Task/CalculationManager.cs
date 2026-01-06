using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タスク10全体を管理するスクリプト
/// </summary>
public class CalculationManager : MonoBehaviour
{
    [Tooltip("親パネル")]
    public GameObject parentPanel;

    [Tooltip("問題の答えとして提出するボタン")]//4つを想定
    public Button[] answerButton;
    [Tooltip("ボタンにアタッチされている答えが記されているテキスト")]
    public Text[] answerText;
    [Tooltip("問題文のテキスト")]
    public Text questionText;
    [Tooltip("現在の問題数のテキスト")]
    public Text nowQuestionNumText;
    [Tooltip("現在の正解数のテキスト")]
    public Text correctNumText;
    [Tooltip("タイマーテキスト")]
    public Text timerText;
    [Tooltip("問題数")]
    public int questionNum = 5;
    [Tooltip("問題終了時に提示するメッセージ")]
    public string[] firstMessages = { "お疲れ様です", "あなたのスコアは『平均的』でした", "多くの挑戦者がこの程度のスコアです", "帰還するなら後ろのワープゾーンで帰還してください" };
    public string[] secondMessages = { "お疲れ様です！", "見事です！平均のスコアを遥かに超えました", "あなたの素晴らしい結果を記録し、", "他の挑戦者たちの模範として公示することもできますが、どうしますか？" };
    [Tooltip("記録を公示するオブジェクト")]
    public GameObject memoryGridObject;
    [Tooltip("記録")]
    public string[] yesMessages = { "あなたの結果を記録しました", "お疲れさまでした", "ワープゾーンで帰還しましょう" };
    public string[] noMessages = { "お疲れさまでした", "ワープゾーンで帰還しましょう" };

    [Tooltip("問題を開始するためのボタン")]
    public Button startButton;
    public Text startText;
    [Tooltip("次のSceneへのオブジェクト")]
    public GameObject nextStoryGoalObject;
    [Header("効果音")]
    public AudioClip audioClip_Timer;
    public AudioClip audioClip_Correct;
    //答えの値
    private int answerNum = 0;
    //答えを格納しているボタンのインデックス
    private int answerButtonIndex = 0;
    //現在の問題数
    private int currentQuestionNum = 0;
    //現在の正解数
    private int correctNum = 0;
    //現在の経過秒数
    private float currentTime = 0f;
    //タイマーを動かしているか判定
    private bool isTimerRunning = false;
    //初めての挑戦なら比較的に難しい問題を実行する
    private bool isFirst = true;

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OpenPanel);
            startText.text = "試練を開始する";
        }

    }
    void Update()
    {
        if (isTimerRunning)
        {
            currentTime += Time.deltaTime;
            timerText.text = $"{currentTime:F1}秒";
        }
    }

    public void OpenPanel()
    {
        parentPanel.SetActive(true);
        StartQuestion();
    }

    private void StartQuestion()
    {
        //タイマー開始
        AudioManager.Instance.PlayLoopingSound(audioClip_Timer);
        isTimerRunning = true;
        currentQuestionNum = 0;
        correctNum = 0;
        correctNumText.text = $"{correctNum}";
        currentTime = 0f;
        startButton.gameObject.SetActive(false);

        MakeQuestion(!isFirst);
    }
    private void EndQuestion()
    {
        //タイマーストップ
        AudioManager.Instance.StopLoopingSound();
        parentPanel.SetActive(false);

        if (isFirst)
        {
            isFirst = false;
            StoryManager.Instance.StartMiddleDialogue(firstMessages);
            startButton.gameObject.SetActive(true);
            startText.text = "試練に再挑戦する";
            nextStoryGoalObject.SetActive(true);
        }
        else
        {
            StoryManager.Instance.StartMiddleDialogue(secondMessages);
            memoryGridObject.SetActive(true);
        }
    }

    //問題作成
    private void MakeQuestion(bool isEasy)
    {
        int beforeNum = 0, afterNum = 0;
        if (isEasy)
        {
            beforeNum = Random.Range(1, 20);
            afterNum = Random.Range(1, 20);
        }
        else
        {
            beforeNum = Random.Range(10, 100);
            afterNum = Random.Range(10, 100);
        }

        //答えの値
        answerNum = beforeNum + afterNum;

        //問題提示
        questionText.text = $"{beforeNum} + {afterNum} = ?";

        //答え設置
        answerButtonIndex = Random.Range(0, answerButton.Length);
        //ランダムに答え提示
        for (int i = 0; i < answerText.Length; i++)
        {
            if (Random.Range(0, 2) == 0)
            {
                answerText[i].text = $"{answerNum + Random.Range(1, 10)}";
            }
            else
            {
                answerText[i].text = $"{answerNum - Random.Range(1, 10)}";
            }
        }
        //正答提示
        answerText[answerButtonIndex].text = $"{answerNum}";

        //問題数提示
        nowQuestionNumText.text = $"問題{currentQuestionNum + 1} / {questionNum}";
    }

    //答えボタンにアタッチ
    public void Answer(int index)
    {
        currentQuestionNum++;

        if (index == answerButtonIndex)
        {
            //正解
            AudioManager.Instance.PlaySound(audioClip_Correct, 1f);

            correctNum++;
            correctNumText.text = $"{correctNum}";
        }
        else
        {
            //不正解
            AudioManager.Instance.PlaySound(AudioManager.Instance.soundMiss, 1f);
        }

        if (currentQuestionNum == questionNum)
        {
            //全問題終了
            EndQuestion();
        }
        else
        {
            //次の問題
            MakeQuestion(!isFirst);
        }
    }

    //結果を記録し公示する
    public void Yes()
    {
        StoryManager.Instance.StartMiddleDialogue(yesMessages);
        memoryGridObject.SetActive(false);
    }
    //結果を記録しない
    public void No()
    {
        StoryManager.Instance.StartMiddleDialogue(noMessages);
        memoryGridObject.SetActive(false);
    }
}
