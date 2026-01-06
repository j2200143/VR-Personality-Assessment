using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
/// <summary>
/// タスクのメッセージ表示とタスクの管理
/// プレイヤーへのメッセージ表示
/// </summary>
/// 
public class StoryManager : MonoBehaviour
{
    //FindSceneReferences()で設定
    [System.NonSerialized]
    public GameObject storyCanvas;
    [System.NonSerialized]
    public Text messageText;
    private InteractionManager interactionManager;

    [Header("ストーリーverで行うタスクを設定:全ファセット測定推奨(ストーリーverとは中断無し、タスク終了時に次のタスクに自動で遷移するverのこと)")]
    public List<TaskSectionSO> doTaskSectionSOList = new List<TaskSectionSO>();//行うタスク
    [Header("ストーリーverならtrue")]
    public bool isStoryVersion = true;
    [Header("シーンのタスクSO設定:測定開始Scene && デバッグ用")]
    public TaskSectionSO initialTaskSectionSO;

    [Header("PCモード設定")]
    public bool isPCMode = false;
    [Header("PC上でVRのシミュレートするなら設定")]
    public bool isEmulatingVR = false;

    // --- プライベート変数 ---
    //InteractionManagerからコントローラを取得する
    private InputDevice rightController;
    private InputDevice leftController;

    private int currentDialogueIndex;
    private Coroutine typingCoroutine;
    private float typingSpeed = 0.05f;

    private bool isInitialScene = true;//始めのシーンだけTaskSectionSOを設定するために
    public TaskSectionSO thisSceneSO;//実行しているタスクのSO
    private List<int> didTaskNumList = new List<int>();//実行済みのタスク
    [Header("測定結果表示scene")]
    public TaskSectionSO endSceneSO;
    [Header("タスク選択デバッグscene")]
    public TaskSectionSO choiceTaskScene;
    [System.NonSerialized]
    public bool isExcuting = false;//messageを表示中かどうか
    private bool isBeforeMessageActive = false;

    //前フレームでボタンが押されていたかを記録するフラグ
    private bool isInputPressedPrev = false;

    //タスク途中に表示するメッセージ
    private string[] middleMessages = null;//タスク途中に表示するメッセージ内容
    private int currentMiddleDialogueIndex;
    [System.NonSerialized]
    public bool isMiddleExcuting = false;//messageを表示中かどうか
    private Coroutine typingMiddleCoroutine;
    //ダイアログ終了時に実行する処理を保持する変数
    private Action onMiddleDialogueComplete;

    //タスク終了時にメッセージを表示し、表示し終わった場合に次のシーンに遷移させるため
    private bool isMoveScene = false;


    //メッセージを表示するcanvasにつけるタグの名前
    private const string StoryCanvasTagName = "StoryCanvas";
    //PCモードなら
    private const string StoryCanvasPCModeTagName = "StoryCanvas_PCMode";
    //メッセージを表示するtextにつけるタグの名前
    private const string MessageTextTagName = "MessageText";
    //PCモードなら
    //メッセージを表示するtextにつけるタグの名前
    private const string MessageTextPCModeTagName = "MessageText_PCMode";

    public static StoryManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }
    void Start()
    {
        SetController();
    }
    void Update()
    {
        //Scene開始時、またはタスク終了時にメッセージが表示された場合の処理
        HandleDialogueInput();

        //タスク途中にメッセージが表示された場合の処理
        HandleMiddleDialogueInput();
    }


    //次に実行するタスクを選択する
    private string ChoiceTaskNum()
    {
        if (isStoryVersion)
        {
            // 1.まだ実行していないタスクだけのリストを作成
            //didTaskNumList には実行済みの「taskNum」が格納されていることが前提
            List<TaskSectionSO> availableTasks = doTaskSectionSOList.Where(taskSO => !didTaskNumList.Contains(taskSO.taskNum)).ToList();

            // 2. 実行可能なタスクが残っていない場合
            if (availableTasks.Count == 0)
            {
                thisSceneSO = endSceneSO;
                return endSceneSO.sceneName;
            }

            // 3. 未実行タスクのリストからランダムなインデックスを選びます。
            int randomIndex = UnityEngine.Random.Range(0, availableTasks.Count);
            TaskSectionSO chosenTask = availableTasks[randomIndex];

            // 4. 選択したタスクを「実行済み」として記録し、現在のタスクとして設定します。
            didTaskNumList.Add(chosenTask.taskNum);
            thisSceneSO = chosenTask; // thisSceneSOを更新

            // 5. 選択したタスクの名前を返します。
            return chosenTask.sceneName;
        }
        else
        {
            thisSceneSO = choiceTaskScene;
            return choiceTaskScene.sceneName;
        }
    }
    //シーン移動
    public void MoveNextScene()
    {
        MoveScene(ChoiceTaskNum());
    }
    public void MoveScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneFader.Instance.LoadSceneWithFade(sceneName);
        }
        else
        {
            Debug.LogError("読み込むシーン名が指定されていません。");
        }
    }


    //メッセージ関連
    //トリガー検知でメッセージの表示・スキップ
    private void HandleDialogueInput()
    {
        if (isExcuting)
        {
            //現在の入力状態を取得する
            bool isCurrentPressed = false;

            // VRコントローラー (Aボタン / Primary Button)
            bool rightPressed = false;
            bool leftPressed = false;

            if (rightController.isValid) rightController.TryGetFeatureValue(CommonUsages.primaryButton, out rightPressed);
            if (leftController.isValid) leftController.TryGetFeatureValue(CommonUsages.primaryButton, out leftPressed);

            // PC入力 (Nキー または マウス左クリック) ※PCモードの場合はマウスDownも「押されている状態」として扱う
            bool pcInput = Input.GetKey(KeyCode.N) || (isPCMode && Input.GetMouseButton(0));

            // いずれかが押されていれば True
            if (rightPressed || leftPressed || pcInput)
            {
                isCurrentPressed = true;
            }

            //「押された瞬間」だけ処理を実行する
            // (今は押されている && 前フレームでは押されていなかった)
            if (isCurrentPressed && !isInputPressedPrev)
            {
                // --- 実行処理  ---
                if (typingCoroutine != null)
                {
                    if (isBeforeMessageActive)
                    {
                        if (isStoryVersion)
                        {
                            if (currentDialogueIndex < thisSceneSO.beforeTaskMessages.Length)
                                SkipTextScrolling(thisSceneSO.beforeTaskMessages[currentDialogueIndex]);
                        }
                        else
                        {
                            if (currentDialogueIndex < thisSceneSO.beforeTaskManualMessages.Length)
                                SkipTextScrolling(thisSceneSO.beforeTaskManualMessages[currentDialogueIndex]);
                        }
                    }
                    else
                    {
                        if (isStoryVersion)
                        {
                            if (currentDialogueIndex < thisSceneSO.afterTaskMessages.Length)
                                SkipTextScrolling(thisSceneSO.afterTaskMessages[currentDialogueIndex]);
                        }
                        else
                        {
                            if (currentDialogueIndex < thisSceneSO.afterTaskManualMessages.Length)
                                SkipTextScrolling(thisSceneSO.afterTaskManualMessages[currentDialogueIndex]);
                        }
                    }
                }
                else
                {
                    currentDialogueIndex++;
                    ShowNextMessage(isBeforeMessageActive, isStoryVersion);
                }
                // -----------------------------
            }

            // 3. 現在の状態を「過去の状態」として保存する
            isInputPressedPrev = isCurrentPressed;
        }
    }
    //メッセージの表示の開始
    public void StartDialogue(bool isBeforeMessage, bool isStoryVersion)
    {
        isExcuting = true;
        currentDialogueIndex = 0;

        messageText.text = "";
        storyCanvas.SetActive(true);

        ShowNextMessage(isBeforeMessage, isStoryVersion);

        //タスク終了時に表示するメッセージなら
        if (isBeforeMessage == false)
        {
            isMoveScene = true;
        }
        else
        {
            isMoveScene = false;
        }
    }
    //メッセージの表示
    private void ShowNextMessage(bool isBeforeMessage, bool isStoryVersion)
    {
        if (isBeforeMessage)
        {
            if (isStoryVersion)
            {
                if (currentDialogueIndex >= thisSceneSO.beforeTaskMessages.Length)//全ての文章を表示したら
                {
                    EndDialogue();
                    return;
                }
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeText(thisSceneSO.beforeTaskMessages[currentDialogueIndex]));
            }
            else
            {
                if (currentDialogueIndex >= thisSceneSO.beforeTaskManualMessages.Length)//全ての文章を表示したら
                {
                    EndDialogue();
                    return;
                }
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeText(thisSceneSO.beforeTaskManualMessages[currentDialogueIndex]));
            }
            isBeforeMessageActive = true;
        }
        else
        {
            if (isStoryVersion)
            {
                if (currentDialogueIndex >= thisSceneSO.afterTaskMessages.Length)//全ての文章を表示したら
                {
                    EndDialogue();
                    return;
                }
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeText(thisSceneSO.afterTaskMessages[currentDialogueIndex]));
            }
            else
            {
                if (currentDialogueIndex >= thisSceneSO.afterTaskManualMessages.Length)//全ての文章を表示したら
                {
                    EndDialogue();
                    return;
                }
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeText(thisSceneSO.afterTaskManualMessages[currentDialogueIndex]));
            }
            isBeforeMessageActive = false;
        }
    }
    //メッセージの表示終了
    private void EndDialogue()
    {
        isExcuting = false;
        storyCanvas.SetActive(false);
        messageText.text = "";
        currentDialogueIndex = 0;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

            //テキスト音終了
            AudioManager.Instance.StopLoopingSound();
        }

        //タスク終了時ならばシーンを遷移する
        if (isMoveScene)
        {
            MoveNextScene();
        }
    }
    //メッセージの内容を次第に表示する
    private IEnumerator TypeText(string message)
    {
        //テキスト音開始
        AudioManager.Instance.PlayLoopingSound(AudioManager.Instance.soundStoryMessage);

        messageText.text = "";

        foreach (char letter in message.ToCharArray())
        {
            messageText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        //テキスト音終了
        AudioManager.Instance.StopLoopingSound();

        typingCoroutine = null; // コルーチンが終了したことを示す
    }
    //メッセージをTypeTextを無視して一気に表示する
    private void SkipTextScrolling(string fullMessage)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

            //テキスト音終了
            AudioManager.Instance.StopLoopingSound();
        }
        messageText.text = fullMessage;
    }

    //タスク途中に表示するメッセージ関連
    //トリガー検知でメッセージの表示・スキップ
    private void HandleMiddleDialogueInput()
    {
        if (isMiddleExcuting)
        {
            // 1. 現在の入力状態を取得 (HandleDialogueInputと同じロジック)
            bool isCurrentPressed = false;
            bool rightPressed = false;
            bool leftPressed = false;

            if (rightController.isValid) rightController.TryGetFeatureValue(CommonUsages.primaryButton, out rightPressed);
            if (leftController.isValid) leftController.TryGetFeatureValue(CommonUsages.primaryButton, out leftPressed);

            bool pcInput = Input.GetKey(KeyCode.N) || (isPCMode && Input.GetMouseButton(0));

            if (rightPressed || leftPressed || pcInput)
            {
                isCurrentPressed = true;
            }

            //押された瞬間」だけ処理
            //HandleDialogueInputと変数を共有すると干渉する可能性があるため、
            // 厳密には別のフラグを用意するか、もしくは「どちらか片方しか実行されない」前提なら共有でも可。
            // ここでは簡易的に共有変数 isInputPressedPrev を使用
            // もし挙動がおかしい場合は private bool isMiddleInputPressedPrev = false; 

            if (isCurrentPressed && !isInputPressedPrev)
            {
                // --- 実行処理 ---
                if (typingMiddleCoroutine != null && middleMessages != null)
                {
                    SkipMiddleTextScrolling(middleMessages[currentMiddleDialogueIndex]);
                }
                else
                {
                    currentMiddleDialogueIndex++;
                    ShowNextMiddleMessage();
                }
            }

            // 3. 状態更新
            isInputPressedPrev = isCurrentPressed;
        }
    }
    public void StartMiddleDialogue(string[] messages, Action onComplete = null)
    {
        if (messages != null)
        {
            // 終了時に実行したい処理を受け取って保存
            onMiddleDialogueComplete = onComplete;

            middleMessages = messages;

            isMiddleExcuting = true;
            currentMiddleDialogueIndex = 0;

            messageText.text = "";
            storyCanvas.SetActive(true);

            ShowNextMiddleMessage();
        }
    }
    //メッセージの表示
    private void ShowNextMiddleMessage()
    {
        if (currentMiddleDialogueIndex >= middleMessages.Length)//全ての文章を表示したら
        {
            EndMiddleDialogue();
            return;
        }
        if (typingMiddleCoroutine != null) StopCoroutine(typingMiddleCoroutine);
        typingMiddleCoroutine = StartCoroutine(TypeMiddleText(middleMessages[currentMiddleDialogueIndex]));
    }
    //メッセージの表示終了
    private void EndMiddleDialogue()
    {
        middleMessages = null;
        isMiddleExcuting = false;
        storyCanvas.SetActive(false);
        messageText.text = "";
        currentMiddleDialogueIndex = 0;

        if (typingMiddleCoroutine != null)
        {
            StopCoroutine(typingMiddleCoroutine);
            typingMiddleCoroutine = null;

            //テキスト音終了
            AudioManager.Instance.StopLoopingSound();
        }

        // 保存しておいた処理があれば実行する
        if (onMiddleDialogueComplete != null)
        {
            onMiddleDialogueComplete.Invoke();
            onMiddleDialogueComplete = null; // 実行後は空にしておく
        }
    }
    //メッセージの内容を次第に表示する
    private IEnumerator TypeMiddleText(string message)
    {
        //テキスト音開始
        AudioManager.Instance.PlayLoopingSound(AudioManager.Instance.soundStoryMessage);

        messageText.text = "";

        foreach (char letter in message.ToCharArray())
        {
            messageText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        //テキスト音終了
        AudioManager.Instance.StopLoopingSound();

        typingMiddleCoroutine = null; // コルーチンが終了したことを示す
    }
    //メッセージをTypeTextを無視して一気に表示する
    private void SkipMiddleTextScrolling(string fullMessage)
    {
        if (typingMiddleCoroutine != null)
        {
            StopCoroutine(typingMiddleCoroutine);
            typingMiddleCoroutine = null;

            //テキスト音終了
            AudioManager.Instance.StopLoopingSound();
        }
        messageText.text = fullMessage;
    }


    //Scene開始時の処理追加
    void OnEnable()
    {
        Debug.Log(this.gameObject.name + "が有効になりました！");
        // シーンがロードされた時にOnSceneLoadedメソッドが呼ばれるように登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        Debug.Log(this.gameObject.name + "が無効になりました。");
        // オブジェクトが破棄される時に登録を解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    // シーンがロードされた直後に自動で実行されるメソッド
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //未使用
        //interactionManager設定
        //FindSceneReferences();

        //thisSceneSOに基づいてメッセージを表示する 
        //SceneFader.csでフェードイン終了時にStartDialogue(true, StoryManager.Instance.isStoryVersion)を実行

        //thisSceneSOの設定は初回のシーンの一回のみで後はシーン移動時にChoiceTaskNum()で設定している(storyVersinなら)。
        if (isInitialScene)//if (isInitialScene || !isStoryVersion)
        {
            thisSceneSO = initialTaskSectionSO;
            isInitialScene = false;
        }

        //測定するファセットを登録する
        for (int i = 0; i < thisSceneSO.personalityFacetArray.Length; i++)
            PersonalityManager.Instance.RegisterExecutedTask(thisSceneSO.personalityFacetArray[i]);
    }
    //UI設定
    private void FindSceneReferences()
    {
        interactionManager = FindFirstObjectByType<InteractionManager>();

        if (interactionManager != null)
        {
            GetController();
        }
        else
        {
            Debug.LogWarning("現在のシーンに InteractionManager が見つかりません。");
        }
    }
    //メッセージを表示するキャンバスとtextを設定. SceneUIBinderから呼ばれる
    public void RegisterSceneUI(GameObject canvas, Text text)
    {
        this.storyCanvas = canvas;
        this.messageText = text;
    }
    //コントローラ取得　未使用
    private void GetController()
    {
        this.rightController = interactionManager.rightController;
        this.leftController = interactionManager.leftController;
    }
    //コントローラ取得
    private void SetController()
    {
        this.rightController = InteractionManager.Instance.rightController;
        this.leftController = InteractionManager.Instance.leftController;
    }
}
