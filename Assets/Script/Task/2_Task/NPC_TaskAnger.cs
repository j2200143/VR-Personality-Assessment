using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// N2（怒り）測定タスク専用のNPC制御クラス。
/// 目的地への移動、途中での停止イベント、NPCスクリプトへの介入を行う。
/// </summary>
public class NPC_TaskAnger : MonoBehaviour
{
    [Header("操作対象のNPC")]
    [Tooltip("Hierarchyにある、動かしたいNPCのGameObjectをここにドラッグ＆ドロップしてください")]
    public GameObject targetNPC;
    [Tooltip("対象のNPCの名前")]
    public string npcName = "オルド";
    [Tooltip("このタスク専用のUI")]
    public GameObject talkCanvas;
    [Tooltip("このタスクを始めるボタン")]
    public Button taskStartButton;
    [Tooltip("プレイヤーに表示する選択肢")]
    public GameObject btnCanvas;
    public Button btnA, btnB;
    [Tooltip("NPCが表示するメッセージ")]
    public Text talkText;
    [Tooltip("選択肢のテキスト")]
    public Text btnAText, btnBText;
    [Tooltip("NPC名前用テキスト")]
    public Text characterNameText;
    [Tooltip("画面端に表示する経過時間表示テキスト")]
    public Text timerText;

    // 内部参照用
    private NavMeshAgent agent;
    private Animator animator;

    [Header("設定")]
    [Tooltip("このタスクが測定するファセット")]
    public PersonalityFacet personalityFacet = PersonalityFacet.N2_Anger;
    [Tooltip("NPCの移動速度")]
    public float walkSpeed = 1.5f;
    [Tooltip("NPCの最終目的地")]
    public Transform finalDestination;
    [Tooltip("NPCが立ち寄る場所、表示するメッセージ")]
    public List<StopEventPoint> stopPoints = new List<StopEventPoint>();
    [Tooltip("最後の目的地に着くまでにNPCが表示するメッセージ")]
    public string beforeFinalDestionationMessage = "もう少しで着くね、君には迷惑をかけたよ";

    [Header("選択後の待機時間設定")]
    [Tooltip("選択肢A（忍耐）を選んだ後の待機時間（例：ゆっくり再開）")]
    public float delayForPatience = 6.0f;
    [Tooltip("選択肢B（苛立ち）を選んだ後の待機時間（例：すぐ再開）")]
    public float delayForIrritation = 1f;

    // 内部状態
    private int currentStopIndex = 0;
    private bool isTaskRunning = false;
    private bool isEventTriggered = false;
    private float elapsedTime = 0f;
    private float typingSpeed = 0.05f;
    private Coroutine typingCoroutine;

    //アニメーション設定
    //Animatorの『歩行中かどうか』を制御するboolパラメータ名
    private const string animatorWalkParameterName = "isWalking";

    [System.Serializable]
    public class StopEventPoint
    {
        [Tooltip("立ち止まる場所")]
        public Transform location;
        [Tooltip("立ち止まった時のセリフ（1行だけ設定する想定）")]
        [TextArea] public string npcMessage;
        [Tooltip("移動中のセリフ（1行だけ設定する想定）")]
        [TextArea] public string npcMessage_Walking;
        [Tooltip("選択肢A（忍耐：0点）のテキスト")]
        public string optionA_Patience = "ええ、待ちますよ";
        [Tooltip("選択肢B（苛立ち：1点）のテキスト")]
        public string optionB_Irritation = "急いでください";
        [Tooltip("選択肢Aを選んだ際のNPCのメッセージ")]
        public string optionA_NPCMessage = "ありがとう";
        [Tooltip("選択肢Bを選んだ際のNPCのメッセージ")]
        public string optionB_NPCMessage = "急ぐことにするよ";
        [Tooltip("停止時間（選択肢が出るまでの演出用）")]
        public float waitDuration = 1.0f;
    }

    void Start()
    {
        if (targetNPC == null)
        {
            Debug.LogError("NPC_TaskAnger: 操作対象のNPC (Target NPC) が設定されていません！");
            return;
        }

        // コンポーネントの取得（targetNPCから取得する）
        agent = targetNPC.GetComponent<NavMeshAgent>();
        animator = targetNPC.GetComponent<Animator>();

        // 取得できたかチェック
        if (agent == null) Debug.LogError("Target NPCに NavMeshAgent がありません。");

        // 初期設定
        if (agent != null) agent.speed = walkSpeed;

        // ボタンイベントの自動登録
        // ボタンA : 忍耐 -> 長めの待機時間を渡す
        if (btnA != null)
        {
            // ラムダ式 `() =>` を使うことで引数付きメソッドを登録できる
            btnA.onClick.AddListener(() => ResumeWalking(true));
        }
        // ボタンB : 苛立ち -> 短い待機時間を渡す
        if (btnB != null)
        {
            btnB.onClick.AddListener(() => ResumeWalking(false));
        }
        if (btnCanvas != null)
        {
            btnCanvas.SetActive(false);
        }
        if (timerText != null)
        {
            timerText.text = "";
        }

        //スタートボタンを設定しているため以下
        if (characterNameText != null)
        {
            characterNameText.text = npcName;
        }
        if (taskStartButton != null)
        {
            taskStartButton.onClick.AddListener(StartEscortTask);
        }
    }

    /// <summary>
    /// タスクを開始する（ボタンから呼び出し、またはエリア侵入トリガーから呼び出し）
    /// </summary>
    public void StartEscortTask()
    {
        isTaskRunning = true;
        currentStopIndex = 0;
        MoveToNextPoint();

        if (taskStartButton != null)
        {
            taskStartButton.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isTaskRunning)
        {
            //経過時間表示
            elapsedTime += Time.deltaTime;
            timerText.text = $"護衛時間:{elapsedTime:F1}";

            // 目的地に到着したかチェック
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    if (!isEventTriggered)
                    {
                        OnReachDestination();
                    }
                }
            }


            if (Camera.main != null)//UIをプレイヤーの方に向ける
            {
                talkCanvas.transform.LookAt(Camera.main.transform.position);
                talkCanvas.transform.forward = -talkCanvas.transform.forward;
            }
        }


    }

    private void MoveToNextPoint()
    {
        isEventTriggered = false;

        if (currentStopIndex < stopPoints.Count)
        {
            agent.SetDestination(stopPoints[currentStopIndex].location.position);

            //NPCの移動中のセリフを表示
            talkCanvas.SetActive(true);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(stopPoints[currentStopIndex].npcMessage_Walking));
        }
        else
        {
            agent.SetDestination(finalDestination.position);

            //NPCの移動中のセリフを表示
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(beforeFinalDestionationMessage));
        }
        agent.isStopped = false;

        animator.SetBool(animatorWalkParameterName, true); // "walk" アニメーション開始
    }

    private void OnReachDestination()
    {
        isEventTriggered = true;

        if (currentStopIndex < stopPoints.Count)
        {
            StartCoroutine(HandleStopEvent(stopPoints[currentStopIndex]));
        }
        else
        {
            CompleteTask();
        }

        animator.SetBool(animatorWalkParameterName, false); // "walk" アニメーション終了
    }

    private IEnumerator HandleStopEvent(StopEventPoint point)
    {
        agent.isStopped = true;

        // NPCが休んだりよそ見をする演出時間
        yield return new WaitForSeconds(point.waitDuration);

        //セリフを表示
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(point.npcMessage));

        //選択肢の表示
        btnAText.text = point.optionA_Patience;
        btnBText.text = point.optionB_Irritation;
        btnCanvas.SetActive(true);
        btnA.gameObject.SetActive(true);
        btnB.gameObject.SetActive(true);
    }

    /// <summary>
    /// ボタンが押されたときに呼ばれる（引数で遅延時間を受け取るように変更）
    /// </summary>
    public void ResumeWalking(bool isChoicedA)
    {

        int score = 0;
        float delayTime = 0f;
        string npcReaction;
        if (isChoicedA)
        {
            delayTime = delayForPatience;
            npcReaction = stopPoints[currentStopIndex].optionA_NPCMessage;
        }
        else//Bが選ばれた場合
        {
            delayTime = delayForIrritation;
            npcReaction = stopPoints[currentStopIndex].optionB_NPCMessage;
            score = (int)((float)PersonalityManager.TASK_MAX_SCORE / stopPoints.Count);
        }

        Debug.Log($"選択肢が選ばれました。{delayTime}秒後に移動を再開します。");

        //スコアの追加
        PersonalityManager.Instance.AddFacetScore(personalityFacet, score);

        currentStopIndex++;

        btnCanvas.SetActive(false);
        btnA.gameObject.SetActive(false);
        btnB.gameObject.SetActive(false);

        // 受け取った delayTime 分だけ待ってから移動開始
        Invoke(nameof(MoveToNextPoint), delayTime);

        //選択肢を選んだ際のNPCの反応
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(npcReaction));
    }

    private void CompleteTask()
    {
        isTaskRunning = false;
        agent.isStopped = true;
        Debug.Log("護衛タスク完了！");

        // ここでクリア報酬の処理
        // 例NPCの感謝のメッセージを表示
        talkText.text = "ありがとう。助かったよ";
        timerText.gameObject.SetActive(false);

        //タスク終了時にメッセージを表示してシーン遷移する
        StoryManager.Instance.StartDialogue(false, StoryManager.Instance.isStoryVersion);
    }

    private IEnumerator TypeText(string message)
    {
        //テキスト音開始
        AudioManager.Instance.PlayLoopingSound(AudioManager.Instance.soundStoryMessage);

        talkText.text = "";
        foreach (char letter in message.ToCharArray())
        {
            talkText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        //テキスト音終了
        AudioManager.Instance.StopLoopingSound();

    }
}