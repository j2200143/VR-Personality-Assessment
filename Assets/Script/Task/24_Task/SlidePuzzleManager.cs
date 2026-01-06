using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 3x3 スライドパズル全体を管理し、C4（達成追求）を測定するスクリプト。
/// 時間制限、再挑戦/スキップの分岐、スコア送信機能を実装。
/// </summary>
public class SlidePuzzleManager : MonoBehaviour
{
    [Header("測定するファセット")]
    public PersonalityFacet personalityFacet = PersonalityFacet.C4_AchievementStriving;

    [Header("問題設定")]
    public int thisQuestionID;

    [Header("パズル構成")]
    [Tooltip("3x3の9つのスロット（Panelなど）を左上から右下の順で設定")]
    public Transform[] slots; // 0-8
    [Tooltip("8枚のパネル（Button）")]
    public SlidePuzzlePanel[] panels; // 0-7
    [Tooltip("空きマスを表す透明なオブジェクト")]
    public Transform emptySlotObject;
    [Tooltip("パズルの親パネル")]
    public GameObject parentPanel;

    [Header("アニメーション設定")]
    public float moveDuration = 0.2f;

    [Header("UI設定")]
    [Tooltip("パズルを開始するボタン")]
    public Button openButton;
    [Tooltip("残り時間を表示するテキスト")]
    public Text limitText;
    [Tooltip("選択肢（再挑戦/スキップ）を表示するパネル")]
    public GameObject choicePanel;
    [Tooltip("「もう一度挑戦する」ボタン")]
    public Button retryButton;
    [Tooltip("「スキップする」ボタン")]
    public Button skipButton;

    [Header("時間設定")]
    [Tooltip("最初の制限時間（秒）。クリア困難な短さに設定")]
    public float initialTimeLimit = 15.0f;
    [Tooltip("再挑戦時に追加される時間（秒）")]
    public float addedTimeOnRetry = 25.0f;

    [Header("効果音")]
    public AudioClip audioClip_Move;
    public AudioClip audioClip_NoMove;
    public AudioClip soundCorrect;
    public AudioClip audioClip_Timer;
    public AudioClip audioClip_LimitTimer;
    public AudioClip audioClip_Door;

    [Header("演出")]
    [Tooltip("最初の失敗時に表示する天の声メッセージ")]
    [TextArea]
    public string[] firstMessage;

    [Tooltip("試練突破時に開けるドア")]
    public Transform doorTransform;
    [Tooltip("ドアが開いたときのY軸の角度")]
    public float targetRotationY = 196f;
    [Tooltip("ドアが開くまでの時間（秒）")]
    public float animDuration = 1.5f;
    [Tooltip("プレイヤーに表示するメッセージ(スキップ時)")]
    public string[] messages_skipped;
    [Tooltip("プレイヤーに表示するメッセージ(非スキップ時)")]
    public string[] messages_noSkipped;
    [Tooltip("試練突破時に表示するオブジェクト")]
    public GameObject storyGoalObject;

    // 内部変数
    private Transform _emptySlotTransform;
    private bool _isAnswered = false;
    private bool isMoving = false;
    private bool isChanged = false;
    // 測定用変数
    private float currentTime;       // 現在の残り時間
    private bool isTimerRunning = false; // タイマーが動いているか
    private int retryCount = 0;      // 再挑戦した回数
    private bool hasFailedOnce = false; // 一度でも失敗したか

    void Start()
    {
        if (openButton != null)
            openButton.onClick.AddListener(OpenPanel);

        // 選択肢ボタンのイベント登録
        if (retryButton != null)
            retryButton.onClick.AddListener(OnSelectRetry);
        if (skipButton != null)
            skipButton.onClick.AddListener(OnSelectSkip);

        // 初期化
        if (choicePanel != null) choicePanel.SetActive(false);
        if (limitText != null) limitText.text = "";
        if (storyGoalObject != null)
            storyGoalObject.SetActive(false);
    }

    void Update()
    {
        // タイマー処理
        if (isTimerRunning && !_isAnswered)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                OnTimeUp(); // 時間切れ処理
            }

            UpdateTimerUI();
        }
    }

    // パズル画面を開く（開始）
    public void OpenPanel()
    {
        if (!_isAnswered)
        {
            // 初期設定（初回のみ）
            if (!hasFailedOnce && retryCount == 0)
            {
                if (slots.Length != 9 || panels.Length != 8)
                {
                    Debug.LogError("エラー: スロットは9個、パネルは8個設定してください。");
                    return;
                }
                InitializePuzzleLayout();
            }

            ShufflePanels(); // シャッフル

            // 親パネル表示
            parentPanel.SetActive(true);
            openButton.gameObject.SetActive(false);
            AudioManager.Instance.PlaySound(AudioManager.Instance.soundClick, 1f);

            // タイマー開始（初回は短い時間、リトライなら加算された時間）
            if (retryCount == 0)
            {
                currentTime = initialTimeLimit;
            }
            // ※リトライ時はOnSelectRetryで時間を設定済み

            isTimerRunning = true;
            SetPanelsInteractable(true);

            //タイマー再生
            AudioManager.Instance.PlayLoopingSound(audioClip_Timer);
        }
    }
    //パズル画面を閉じる
    public void ClosePanel()
    {
        parentPanel.SetActive(false);
        emptySlotObject.gameObject.SetActive(false);
        SetPanelsInteractable(false);
    }
    // パズル配置の初期化
    private void InitializePuzzleLayout()
    {
        _emptySlotTransform = slots[8];
        emptySlotObject.SetParent(_emptySlotTransform, false);
        emptySlotObject.localPosition = Vector3.zero;

        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].transform.SetParent(slots[i], false);
            panels[i].transform.localPosition = Vector3.zero;
            panels[i].manager = this;
        }
    }

    // タイマー表示更新
    private void UpdateTimerUI()
    {
        if (limitText != null)
        {
            limitText.text = $"残り時間: {currentTime:F1}秒";
            if (currentTime <= 5.0f)
            {
                if (!isChanged)
                {
                    isChanged = true;

                    limitText.color = Color.red;
                    AudioManager.Instance.StopLoopingSound();
                    //タイマー再生
                    AudioManager.Instance.PlayLoopingSound(audioClip_LimitTimer);
                }
            }
            else limitText.color = Color.white;
        }
    }

    // 時間切れ時の処理
    private void OnTimeUp()
    {
        isTimerRunning = false;
        ClosePanel();

        // 失敗フローのコルーチン開始
        StartCoroutine(FailSequence());

        //タイマーストップ
        AudioManager.Instance.StopLoopingSound();
    }

    // 失敗時の演出フロー
    private IEnumerator FailSequence()
    {
        // 最初の失敗時のみメッセージを表示
        if (!hasFailedOnce)
        {
            hasFailedOnce = true;

            // 天の声メッセージ表示 
            StoryManager.Instance.StartMiddleDialogue(firstMessage);


            // メッセージ読了や余韻のために待機
            yield return new WaitForSeconds(4.0f);
        }
        else
        {
            // 2回目以降の失敗は少しだけ待ってすぐ選択肢へ
            yield return new WaitForSeconds(1.0f);
        }

        // 選択肢パネルを表示
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }
    }

    // 選択肢A: もう一度挑戦する
    private void OnSelectRetry()
    {
        // 選択肢パネルを閉じる
        choicePanel.SetActive(false);

        // 制限時間を延長
        retryCount++;
        currentTime = initialTimeLimit + (retryCount * addedTimeOnRetry);

        AudioManager.Instance.PlaySound(AudioManager.Instance.soundClick, 1f);
        Debug.Log($"再挑戦: {retryCount}回目 (制限時間: {currentTime}秒)");

        OpenPanel();
    }

    // 選択肢B: スキップする
    private void OnSelectSkip()
    {
        choicePanel.SetActive(false);
        ClosePanel(); // クリア扱いにはしないがパネルは閉じる

        // --- スコア評価 ---
        int score = 0;
        if (retryCount == 0)
        {
            // 0点: 1回目の失敗で即座にスキップ
            score = 0;
        }
        else
        {
            // 2点: 少なくとも1回は再挑戦したが、最終的に諦めた
            score = 2; // (中間評価の定義が2点の場合)
        }

        // スコア送信
        SendScore(score);

        //ドアを開けるアニメーション
        OpenDoor(true);
    }

    // パズル操作時の処理 (DOTween対応)
    public void OnPanelClicked(SlidePuzzlePanel clickedPanel)
    {
        if (isMoving || !isTimerRunning) return; // タイマー停止中も操作不可

        Transform clickedSlot = clickedPanel.transform.parent;

        if (IsAdjacent(clickedSlot, _emptySlotTransform))
        {
            isMoving = true;
            Transform targetSlot = _emptySlotTransform;
            Vector3 targetPosition = targetSlot.position;

            // 空きスロット移動
            emptySlotObject.SetParent(clickedSlot, true);
            emptySlotObject.DOMove(clickedSlot.position, moveDuration).SetEase(Ease.OutQuad);

            // パネル移動
            clickedPanel.transform.SetParent(targetSlot, true);
            clickedPanel.transform.DOMove(targetPosition, moveDuration).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _emptySlotTransform = clickedSlot;
                    isMoving = false;
                    CheckAnswer();
                });

            AudioManager.Instance.PlaySound(audioClip_Move, 1f);
        }
        else
        {
            AudioManager.Instance.PlaySound(audioClip_NoMove, 1f);
        }
    }

    // 正解判定
    private void CheckAnswer()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            SlidePuzzlePanel panel = panels[i];
            int currentSlotIndex = GetSlotIndex(panel.transform.parent);
            if (panel.correctID != currentSlotIndex) return;
        }

        // クリアした場合
        OnClear();
    }

    // クリア処理
    private void OnClear()
    {
        Debug.Log($"問題番号{thisQuestionID}: クリア！");
        _isAnswered = true;
        isTimerRunning = false;

        if (choicePanel != null) choicePanel.SetActive(false);
        if (limitText != null) limitText.text = "CLEAR!";
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound(soundCorrect, 1f);

        // --- スコア評価 ---
        // 1回目でクリア、または再挑戦してクリア（粘り強さ）
        int score = PersonalityManager.TASK_MAX_SCORE; //  (最大評価)

        SendScore(score);

        // パネルを閉じる処理
        ClosePanel();

        //タイマーストップ
        AudioManager.Instance.StopLoopingSound();


        //ドアを開けるアニメーション
        OpenDoor(false);
    }

    // スコア送信共通処理
    private void SendScore(int score)
    {
        if (PersonalityManager.Instance != null)
        {
            // スコア加算
            PersonalityManager.Instance.AddFacetScore(personalityFacet, score);
        }
    }

    //突破時のイベント
    private void OpenDoor(bool isSkipped)
    {
        AudioManager.Instance.PlaySound(audioClip_Door, 0.7f);
        // --- アニメーションのシーケンス（順序）を作成 ---
        Sequence seq = DOTween.Sequence();

        // ドアのアニメーション (回転)
        // 現在の角度から、指定した角度へ回転させる
        // LocalRotateにすることで、親オブジェクトが回転していても正しく動きます
        seq.Append(doorTransform.DOLocalRotate(new Vector3(0, targetRotationY, 0), animDuration));

        // 終了処理
        seq.OnComplete(() =>
        {
            //プレイヤーにメッセージを表示
            if (isSkipped)
            {
                StoryManager.Instance.StartMiddleDialogue(messages_skipped);
            }
            else
            {
                StoryManager.Instance.StartMiddleDialogue(messages_noSkipped);
            }
        });

        storyGoalObject.SetActive(true);
    }

    // --- ユーティリティ ---

    private void ShufflePanels()
    {
        SetPanelsInteractable(false);
        int shuffleMoves = 30;
        int lastMovedIndex = -1;

        for (int i = 0; i < shuffleMoves; i++)
        {
            int emptyIndex = GetSlotIndex(_emptySlotTransform);
            List<int> adjacentIndexes = GetAdjacentSlotIndexes(emptyIndex);
            adjacentIndexes.Remove(lastMovedIndex);
            if (adjacentIndexes.Count == 0) continue;

            int targetSlotIndex = adjacentIndexes[Random.Range(0, adjacentIndexes.Count)];
            Transform panelToMove = null;
            foreach (Transform child in slots[targetSlotIndex])
            {
                if (child != emptySlotObject) { panelToMove = child; break; }
            }

            if (panelToMove == null) continue;

            panelToMove.SetParent(_emptySlotTransform, false);
            panelToMove.localPosition = Vector3.zero;
            emptySlotObject.SetParent(slots[targetSlotIndex], false);
            emptySlotObject.localPosition = Vector3.zero;

            _emptySlotTransform = slots[targetSlotIndex];
            lastMovedIndex = emptyIndex;
        }
    }

    private void SetPanelsInteractable(bool interactable)
    {
        foreach (var panel in panels)
        {
            if (panel != null)
                panel.GetComponent<Button>().interactable = interactable;
        }
    }

    private int GetSlotIndex(Transform slot)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == slot) return i;
        return -1;
    }

    private List<int> GetAdjacentSlotIndexes(int index)
    {
        List<int> adjacentIndexes = new List<int>();
        int col = index % 3;
        int row = index / 3;

        if (col > 0) adjacentIndexes.Add(index - 1);
        if (col < 2) adjacentIndexes.Add(index + 1);
        if (row > 0) adjacentIndexes.Add(index - 3);
        if (row < 2) adjacentIndexes.Add(index + 3);

        return adjacentIndexes;
    }

    private bool IsAdjacent(Transform slotA, Transform slotB)
    {
        int indexA = GetSlotIndex(slotA);
        int indexB = GetSlotIndex(slotB);
        if (indexA == -1 || indexB == -1) return false;

        int colA = indexA % 3;
        int rowA = indexA / 3;
        int colB = indexB % 3;
        int rowB = indexB / 3;

        return (rowA == rowB && Mathf.Abs(colA - colB) == 1) ||
               (colA == colB && Mathf.Abs(rowA - rowB) == 1);
    }


}