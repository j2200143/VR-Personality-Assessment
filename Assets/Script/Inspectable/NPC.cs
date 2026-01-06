using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro;
using System;
/// <summary>
/// NPCとの対話、選択肢、性格スコアの管理を行うクラス。
/// IInteractableを実装し、InteractionManagerから直接操作できる。
/// </summary>
public class NPC : MonoBehaviour, IInteractable
{
    // 対話の状態を管理するための列挙型
    private enum DialogueState
    {
        Idle,           // 待機中
        ShowingDialogue, // 対話文を表示中
        ShowingChoices,  // 選択肢を表示中
        AfterChoice      // 選択肢後のメッセージ表示中
    }

    [Header("NPCの名前")]
    public string npcName = "村人A";
    [Header("NPCへの反応")]
    private string speakMessage = "話しかける:Aボタン";
    [Header("対話内容")]
    [TextArea(2, 10)]
    public string[] dialogueLines; // NPCのセリフの配列

    [Header("性格診断に関連するなら/選択肢を表示したいなら設定する")]
    [Tooltip("選択肢を持つNPCならばtrue")]
    public bool hasChoices = false;
    [Tooltip("スコア測定するなら")]
    public bool isRelationTask = true;//後々追加したため仕方なくtrueで設定。
    [Tooltip("選択肢に対応した文章")]
    public string[] choiceOptions;
    [Tooltip("選択肢に答えた場合の点数。それぞれの選択肢のインデックスに対応*PersonalityManagerのTASK_MAX_SCOREが上限")]
    public int[] plusArray;//isRelationTaskがfalseの場合にも0,0などで設定する
    [Tooltip("選択肢を選んだ後のメッセージ。それぞれの選択肢のインデックスに対応")]
    public string[] afterChoiceMessage;
    [Tooltip("選択肢を選んだ後に表示するオブジェクト。それぞれの選択肢のインデックスに対応")]
    public GameObject[] objectArrayAfterChoice;
    [Tooltip("選択肢を選んだ後に非表示にするオブジェクト。それぞれの選択肢のインデックスに対応")]
    public GameObject[] objectArrayAfterChoice_Hide;
    [Tooltip("どのファセットを測定するか")]
    public PersonalityFacet personalityFacet;
    [Tooltip("選択肢のButton")]
    public GameObject[] choiceButtonArray;
    [Tooltip("選択肢のText")]
    public TextMeshProUGUI[] choiceButtonText;
    [Header("複数選択肢を用いてファセット測定を行う場合")]
    public NPCMultiChoice npcMultiChoice;
    private bool isMultiChoiceMode = false; // 現在マルチモード中か
    private int currentMultiStageIndex = 0; // 現在どの段階にいるか


    [Header("参照")]
    public GameObject InspectableObjectCanvas;
    public Text characterNameText;
    public Text messageText;

    [Header("固定のメッセージを表示したい場合のみ設定")]
    public GameObject fixedCanvas;//textを表示し続けるOR途中で表示する場合に設定するCanvas
    public Text fixedCharacterNameText, fixedMessageText;//textを表示し続けるOR途中で表示する場合に設定するText
    [Tooltip("表示したいメッセージが一通りの場合のみ設定:インデックス0のみに書き込み可能")]
    public string[] fixMessage;//インデックス0のみになってしまった理由：lengthで表示したいメッセージがあるか判定するという謎判定を採用してしまった過去の過ち

    [Header("Canvasが回転した場合他のオブジェクトにめりこむ場合はfalse")]
    public bool isCanRotate = false;

    [Header("会話したかを判定したい場合のみアタッチ")]
    public CountShowNextStory countShowNextStory;
    private bool isCounted = false;

    [Header("その他設定")]
    public float typingSpeed = 0.05f;

    [Header("Attachプレハブを使用している場合のみアタッチ")]//後々Attachプレハブ作成したため仕方なく作成
    [Tooltip("Animator取得用")]
    public GameObject characterGameObject;



    // --- プライベート変数 ---
    private DialogueState currentState = DialogueState.Idle;
    private int currentDialogueIndex = 0;
    private Coroutine typingCoroutine;
    private string currentAfterChoiceMessage; //選択肢後のメッセージを保持する変数
    private bool isFirstDialogueShowed = false;//最初の会話がInteract()のトリガーと混合してスキップされてしまうことを防ぐため

    private bool isTalked = false;//2回選択肢を表示しないようにする.trueの場合、InspectableObjectCanvasが表示されなくなる

    private int[] originalChoiceIndex;//シャッフルした選択肢の元々の選択肢のインデックスを記しておく

    private bool justEndedDialogue = false;//連続会話バグを防ぐ

    private bool wasRightPressedLastFrame = false; // 前フレームの右ボタン状態
    private bool wasLeftPressedLastFrame = false;  // 前フレームの左ボタン状態

    //アニメーション設定
    //Animatorの『話しているかどうか』を制御するboolパラメータ名
    private const string animatorTalkParameterName = "isTalking";
    [HideInInspector]
    public Animator animator;

    private InputDevice rightController;
    private InputDevice leftController;


    void Start()
    {
        if (InspectableObjectCanvas != null && InspectableObjectCanvas.activeSelf)
        {
            InspectableObjectCanvas.gameObject.SetActive(false);
        }

        for (int i = 0; i < choiceButtonArray.Length; i++)//選択肢非表示
        {
            choiceButtonArray[i].SetActive(false);
        }

        //Animation設定
        if (characterGameObject == null)
            animator = GetComponent<Animator>();
        else
            animator = characterGameObject.GetComponent<Animator>();
        // NPCが同じ動きを同じ時間に行わないようにこのNPCのIdleOffsetパラメータに、0.0〜1.0のランダムな値を設定する
        if (animator != null)
        {
            animator.SetFloat("IdleOffset", UnityEngine.Random.Range(0f, 1f));
        }

        //計測用
        if (hasChoices)
            originalChoiceIndex = new int[choiceOptions.Length];

        if (StoryManager.Instance.isPCMode)
        {
            messageText.text = "話しかける:左クリック";
        }
        else
        {
            messageText.text = speakMessage;
        }
    }

    void Update()
    {
        // 現在の対話状態に基づいて入力を処理
        if (currentState != DialogueState.Idle && isTalked == false)
        {
            switch (currentState)
            {
                case DialogueState.ShowingDialogue:
                    HandleDialogueInput();
                    break;
                case DialogueState.AfterChoice:
                    HandleAfterChoiceInput();
                    break;
            }

            if (Camera.main != null && isCanRotate)//UIをプレイヤーの方に向ける
            {
                InspectableObjectCanvas.transform.LookAt(Camera.main.transform.position);
                InspectableObjectCanvas.transform.forward = -InspectableObjectCanvas.transform.forward;
            }
        }
    }

    #region IInteractableの実装
    public void ShowCanvas()
    {
        characterNameText.text = npcName;
        messageText.text = speakMessage;
        if (isTalked == false)
            InspectableObjectCanvas.gameObject.SetActive(true);

        if (Camera.main != null && isCanRotate)//UIをプレイヤーの方に向ける
        {
            InspectableObjectCanvas.transform.LookAt(Camera.main.transform.position);
            InspectableObjectCanvas.transform.forward = -InspectableObjectCanvas.transform.forward;
        }
    }
    //話しかけた場合の処理
    public void Interact()
    {
        if (currentState != DialogueState.Idle || isTalked || justEndedDialogue)
            return;

        AudioManager.Instance.PlaySound(AudioManager.Instance.soundInspectable, AudioManager.Instance.Normal);

        // 複数選択肢を行う場合
        if (npcMultiChoice != null && npcMultiChoice.dialogueStages.Count > 0)
        {
            StartMultiChoiceDialogue();
        }
        else
        {
            isMultiChoiceMode = false;
            StartDialogue();
        }

        InteractionManager.Instance.activeInteractionCount++;
    }
    public bool CheckExcute()//実行中か確認する。つまり、アイドル状態のときだけfalseを返す。
    {
        return currentState != DialogueState.Idle;
    }
    public GameObject GetInspectableCanvas()
    {
        return InspectableObjectCanvas;
    }
    public void SetEnd()
    {
        if (currentState == DialogueState.Idle)
        {
            InspectableObjectCanvas.SetActive(false);
        }
    }
    #endregion

    #region メッセージの処理
    private void StartDialogue()
    {
        SetController();
        currentState = DialogueState.ShowingDialogue;
        //アニメーション設定
        animator.SetBool(animatorTalkParameterName, true); // "Talk" アニメーション開始

        currentDialogueIndex = 0;

        messageText.text = "";

        ShowNextMessage();
    }
    public void EndDialogue()
    {
        InspectableObjectCanvas.SetActive(false);
        messageText.text = "";
        currentDialogueIndex = 0;
        currentState = DialogueState.Idle;
        //アニメーション設定
        animator.SetBool(animatorTalkParameterName, false); // "Talk" アニメーション終了

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

            //テキスト音終了
            AudioManager.Instance.StopLoopingSound();
        }

        isFirstDialogueShowed = false;

        InteractionManager.Instance.activeInteractionCount--;

        //選択肢を持つNPCならば
        if (hasChoices)
        {
            //選択肢を2度選ばせないように:通常メッセージCanvasが2度目は表示されなくなる
            isTalked = true;
            //固定でメッセージを表示する
            StartCoroutine(ShowFixedCanvasAfterTime());
        }

        //会話したかを判定したい場合のみカウントする
        if (countShowNextStory != null && !isCounted)
        {
            countShowNextStory.CountPlasForShowObject();
            isCounted = true;
        }

        StartCoroutine(DialogueCooldown());
    }
    private void HandleEndChoice(int choiceIndex)
    {
        currentState = DialogueState.AfterChoice;
        if (choiceIndex < afterChoiceMessage.Length && !string.IsNullOrEmpty(afterChoiceMessage[choiceIndex]))
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            currentAfterChoiceMessage = afterChoiceMessage[choiceIndex];
            typingCoroutine = StartCoroutine(TypeText(currentAfterChoiceMessage));
        }
        else
        {
            EndDialogue();
        }

        // 選択肢後に表示するオブジェクト処理
        if (objectArrayAfterChoice.Length > 0 && objectArrayAfterChoice.Length > originalChoiceIndex[choiceIndex] && objectArrayAfterChoice[originalChoiceIndex[choiceIndex]] != null)
        {
            objectArrayAfterChoice[originalChoiceIndex[choiceIndex]].SetActive(true);
        }

        // 選択肢後に非表示にするオブジェクト処理
        if (objectArrayAfterChoice_Hide.Length > 0 && objectArrayAfterChoice_Hide.Length > originalChoiceIndex[choiceIndex] && objectArrayAfterChoice_Hide[originalChoiceIndex[choiceIndex]] != null)
        {
            objectArrayAfterChoice_Hide[originalChoiceIndex[choiceIndex]].SetActive(false);
        }
    }
    private void ShowNextMessage()
    {
        if (currentDialogueIndex >= dialogueLines.Length)//全ての文章を表示したら
        {
            if (hasChoices)
            {
                DisplayChoices();
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(dialogueLines[currentDialogueIndex]));

    }
    //ChoiceButtonを表示
    private void DisplayChoices()
    {
        currentState = DialogueState.ShowingChoices;
        InitialChoices();
        for (int i = 0; i < choiceOptions.Length; i++)
        {
            choiceButtonArray[i].SetActive(true);
        }

        //選択肢を選ぶまでの時間計測開始
        if (isRelationTask)
            AnalyticsManager.Instance.StartResponseTimer(personalityFacet.ToString());
    }
    private void InitialChoices()//選択肢をシャッフル
    {
        // バリデーション
        int maxChoices = choiceOptions.Length;

        if (maxChoices != plusArray.Length || maxChoices != afterChoiceMessage.Length)
        {
            Debug.LogError("選択肢のデータ配列の長さが一致しません！");
            return;
        }

        //配列が未初期化、またはサイズが合わない場合はここで初期化する ▼▼▼
        if (originalChoiceIndex == null || originalChoiceIndex.Length != maxChoices)
        {
            originalChoiceIndex = new int[maxChoices];
        }

        // シャッフル対象の「インデックスのリスト」を作成 [0, 1, 2, 3]
        List<int> indices = new List<int>();
        for (int i = 0; i < maxChoices; i++)
        {
            indices.Add(i);
        }

        // 「インデックスのリスト」をシャッフル [2, 0, 3, 1] など
        int n = indices.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            int temp = indices[k];
            indices[k] = indices[n];
            indices[n] = temp;
        }

        // 元のデータを「新しい」一時配列に「コピー」する
        string[] tempOptions = new string[maxChoices];
        Array.Copy(choiceOptions, tempOptions, maxChoices);

        int[] tempPlus = new int[maxChoices];
        Array.Copy(plusArray, tempPlus, maxChoices);

        string[] tempAfterMessages = new string[maxChoices];
        Array.Copy(afterChoiceMessage, tempAfterMessages, maxChoices);

        // シャッフルされたインデックスに基づき、データを並べ替える
        for (int i = 0; i < maxChoices; i++)
        {
            int shuffledIndex = indices[i];

            // ボタンのテキストを設定
            choiceButtonText[i].text = tempOptions[shuffledIndex];

            // 内部データ (スコア) を並べ替え
            plusArray[i] = tempPlus[shuffledIndex];

            // 内部データ (メッセージ) を並べ替え
            afterChoiceMessage[i] = tempAfterMessages[shuffledIndex];

            //計測用に選択肢を覚えておく
            originalChoiceIndex[i] = shuffledIndex;
        }

        //ボタンにonClickイベントをInspectorで登録していなければ
        for (int i = 0; i < maxChoices; i++)
        {
            if (choiceButtonArray[i].GetComponent<Button>().onClick.GetPersistentEventCount() == 0)
            {
                int index = i;
                choiceButtonArray[i].GetComponent<Button>().onClick.AddListener(() => OnChoiceSelected(index));
            }
        }
    }
    //選択肢が選ばれた後の処理
    public void OnChoiceSelected(int choiceIndex)
    {
        if (currentState != DialogueState.ShowingChoices) return;

        AudioManager.Instance.PlaySound(AudioManager.Instance.soundInspectable, AudioManager.Instance.Normal);

        for (int i = 0; i < choiceButtonArray.Length; i++)
        {
            choiceButtonArray[i].SetActive(false);
        }

        // シャッフル対応: 実際に選ばれたデータのIndexを取得
        int realIndex = originalChoiceIndex[choiceIndex];

        // スコア加算
        if (isRelationTask)
        {
            PersonalityManager.Instance.AddFacetScore(personalityFacet, plusArray[choiceIndex]); // シャッフル済みの配列から取るのでchoiceIndexでOK
            AnalyticsManager.Instance.LogResponse(originalChoiceIndex[choiceIndex]);
        }


        // 分岐ロジックの追加
        if (isMultiChoiceMode)
        {
            // 現在のステージデータから、選ばれた選択肢の設定を取得
            var stageData = npcMultiChoice.dialogueStages[currentMultiStageIndex];
            var selectedOption = stageData.choices[realIndex]; // 元データへのアクセスにはrealIndexを使う

            if (selectedOption.isContinue)
            {
                // === 次の会話へ進む場合 ===
                currentMultiStageIndex = selectedOption.nextStageIndex;
                LoadMultiChoiceStage(currentMultiStageIndex); // 次のデータを注入

                // 状態を会話表示中に戻す
                currentState = DialogueState.ShowingDialogue;
                messageText.text = "";

                // アニメーション再開
                animator.SetBool(animatorTalkParameterName, true);

                // 次のテキスト表示を開始
                ShowNextMessage();
            }
            else
            {
                HandleEndChoice(choiceIndex);
            }
        }
        else
        {
            // 通常モード（既存処理）
            HandleEndChoice(choiceIndex);
        }


    }
    //複数選択肢
    private void StartMultiChoiceDialogue()
    {
        isMultiChoiceMode = true;
        currentMultiStageIndex = 0;
        LoadMultiChoiceStage(0);    // データをロード

        StartDialogue();
    }
    private void LoadMultiChoiceStage(int stageIndex)
    {
        if (stageIndex >= npcMultiChoice.dialogueStages.Count) return;

        var stageData = npcMultiChoice.dialogueStages[stageIndex];

        // 既存のフィールドを一時的に上書きして使い回す（メモリ確保を抑え、表示ロジックを共通化）
        this.dialogueLines = stageData.messages;
        this.hasChoices = true; // 強制的に選択肢ありモードにする

        // 選択肢データの配列サイズを合わせる
        this.choiceOptions = new string[stageData.choices.Length];
        this.plusArray = new int[stageData.choices.Length];
        this.afterChoiceMessage = new string[stageData.choices.Length];

        // データをコピー
        for (int i = 0; i < stageData.choices.Length; i++)
        {
            this.choiceOptions[i] = stageData.choices[i].buttonText;
            this.plusArray[i] = stageData.choices[i].score;

            if (!stageData.choices[i].isContinue)
            {
                // 終了する場合のメッセージ
                this.afterChoiceMessage[i] = stageData.choices[i].endMessage;
            }
            else
            {
                // 続く場合はメッセージなし（即座に次の会話へ）
                this.afterChoiceMessage[i] = "";
            }
        }

        // 会話インデックスをリセット
        currentDialogueIndex = 0;
    }

    #endregion

    #region 入力処理
    private void SetController()
    {

        this.rightController = InteractionManager.Instance.rightController;
        this.leftController = InteractionManager.Instance.leftController;
    }
    private void HandleDialogueInput()//トリガーを引いた場合、次の文章を表示するか現在の文章をスキップするか
    {
        bool triggerPressed = false;

        // PCモード対応
        if (StoryManager.Instance.isPCMode)
        {
            // マウス左クリック
            if (Input.GetMouseButtonDown(0))
            {
                triggerPressed = true;
            }
        }
        else
        {
            // --- VR入力修正版 (押しっぱなし検知を防ぐ処理) ---

            bool isRightPressedNow = false;
            bool isLeftPressedNow = false;

            //現在のフレームの状態を取得
            if (rightController.isValid)
                rightController.TryGetFeatureValue(CommonUsages.primaryButton, out isRightPressedNow);

            if (leftController.isValid)
                leftController.TryGetFeatureValue(CommonUsages.primaryButton, out isLeftPressedNow);

            //前回は押されておらず」かつ「今は押されている」場合のみ true (GetButtonDown相当)
            if (isRightPressedNow && !wasRightPressedLastFrame)
                triggerPressed = true;

            if (isLeftPressedNow && !wasLeftPressedLastFrame)
                triggerPressed = true;

            //現在の状態を「過去の状態」として保存（次フレーム用）
            wasRightPressedLastFrame = isRightPressedNow;
            wasLeftPressedLastFrame = isLeftPressedNow;
        }

        // デバッグキー(N)
        if (Input.GetKeyDown(KeyCode.N)) triggerPressed = true;

        if (triggerPressed)
        {
            // テキストがタイピング中の場合、スキップして全文表示
            if (typingCoroutine != null)
            {
                if (currentDialogueIndex < dialogueLines.Length)
                {
                    if (isFirstDialogueShowed)
                        SkipTextScrolling(dialogueLines[currentDialogueIndex]);
                    else
                        isFirstDialogueShowed = true;
                }
            }
            // テキストが全文表示されている場合、次のメッセージへ進む
            else
            {
                currentDialogueIndex++;
                ShowNextMessage();
            }
        }
    }
    private void HandleAfterChoiceInput()
    {

        bool triggerPressed = false;

        // PCモード対応
        if (StoryManager.Instance.isPCMode)
        {
            // マウス左クリック
            if (Input.GetMouseButtonDown(0))
            {
                triggerPressed = true;
            }
        }
        else
        {
            // 既存のVR入力チェック
            bool rightPress = false, leftPress = false;
            if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.primaryButton, out rightPress) && rightPress) triggerPressed = true;
            if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.primaryButton, out leftPress) && leftPress) triggerPressed = true;
        }

        // デバッグキー(N)
        if (Input.GetKeyDown(KeyCode.N)) triggerPressed = true;

        if (triggerPressed)
        {
            // テキストがタイピング中の場合、スキップして全文表示
            if (typingCoroutine != null)
            {
                SkipTextScrolling(currentAfterChoiceMessage);
            }
            // テキストが全文表示されている場合、対話を終了
            else
            {
                EndDialogue();
            }
        }

    }
    #endregion

    #region コルーチンとテキスト処理
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
    #endregion

    #region fixedCanvasの実装
    public void ShowFixedCanvasWithTypeSound(string message)//呼び出し元：Button,もしくは他のscriptの関数
    {
        //通常の会話を表示しなくする
        InspectableObjectCanvas.SetActive(false);
        isTalked = true;

        //会話途中である場合を配慮
        animator.SetBool(animatorTalkParameterName, false); // "Talk" アニメーション終了

        //メッセージの表示
        fixedCharacterNameText.text = npcName;
        fixedCanvas.SetActive(true);
        StartCoroutine(TypeTextFixed(message));
    }
    private IEnumerator TypeTextFixed(string message)
    {
        //テキスト音開始
        AudioManager.Instance.PlayLoopingSound(AudioManager.Instance.soundStoryMessage);

        fixedMessageText.text = "";
        foreach (char letter in message.ToCharArray())
        {
            fixedMessageText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        //テキスト音終了
        AudioManager.Instance.StopLoopingSound();
    }
    public void ShowFixedCanvas(string message)//呼び出し元：Button,もしくは他のscriptの関数
    {
        //通常の会話を表示しなくする
        InspectableObjectCanvas.SetActive(false);
        isTalked = true;

        //会話途中である場合を配慮
        animator.SetBool(animatorTalkParameterName, false); // "Talk" アニメーション終了

        //メッセージの表示
        if (fixedCharacterNameText != null)
            fixedCharacterNameText.text = npcName;
        if (fixedMessageText != null)
            fixedMessageText.text = message;
        if (fixedCanvas != null)
            fixedCanvas.SetActive(true);
    }
    private IEnumerator ShowFixedCanvasAfterTime()//選択肢を持つNPC限定
    {
        yield return new WaitForSeconds(2f);
        if (fixMessage.Length == 0)
            ShowFixedCanvas(currentAfterChoiceMessage);
        else
            ShowFixedCanvas(fixMessage[0]);
    }
    public void CloseFixedCanvas()
    {
        fixedCanvas.SetActive(false);
    }
    #endregion

    //連続会話バグを防ぐ
    private IEnumerator DialogueCooldown()
    {
        justEndedDialogue = true;
        yield return new WaitForSeconds(0.8f);
        justEndedDialogue = false;
    }
}

