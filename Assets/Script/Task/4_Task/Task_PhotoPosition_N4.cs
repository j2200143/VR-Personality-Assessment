using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // List使用のため追加

public class Task_PhotoPosition_N4 : MonoBehaviour
{
    // 外部（ZoneTrigger）からアクセスするためのシングルトンインスタンス
    public static Task_PhotoPosition_N4 Instance { get; private set; }

    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.N4_SelfConsciousness;

    [Header("UI設定")]
    [Tooltip("状態を表示するテキスト")]
    public Text adviceText;

    [Header("演出用")]
    [Tooltip("撮影時のフラッシュエフェクト（Panelを一瞬白くするなど）")]
    public GameObject flashEffect;
    [Tooltip("シャッター音")]
    public AudioClip shutterSound;

    [Header("操作")]
    public Button shutterButton;
    public Text shutterText;
    bool isPCMode = false;

    [System.Serializable]
    public class MessageGroup
    {
        [TextArea(2, 3)]
        public string[] messages;
    }

    [Header("撮影後のメッセージ（天の声）")]
    [Tooltip("Element 0:中央(0点) ～ Element 4:枠外(4点)")]
    public MessageGroup[] resultMessages;

    // 現在プレイヤーが滞在しているゾーンのリスト
    // （ゾーンが重なっている場合、複数のスコアが入る可能性があるためリストで管理）
    private List<int> activeZones = new List<int>();

    // 処理重複防止フラグ
    private bool isPhotoTaken = false;

    void Awake()
    {
        // シングルトンパターンの実装
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
        if (flashEffect != null) flashEffect.SetActive(false);
        if (shutterButton != null)
        {
            shutterButton.onClick.AddListener(TakePhoto);
        }

        // 初期状態のテキスト更新
        UpdateAdviceText();

        if (StoryManager.Instance.isPCMode)
        {
            shutterText.text = "右クリック:撮影";
            isPCMode = true;
        }
    }

    void Update()
    {
        if (isPCMode)
        {
            if (Input.GetMouseButton(1))
            {
                TakePhoto();
            }
        }
    }

    // ZoneTriggerから呼ばれる：ゾーンに入った
    public void OnPlayerEnterZone(int scoreType)
    {
        // まだリストになければ追加
        if (!activeZones.Contains(scoreType))
        {
            activeZones.Add(scoreType);
            // 状況が変わったのでUIを更新
            UpdateAdviceText();
        }
    }

    // ZoneTriggerから呼ばれる：ゾーンから出た
    public void OnPlayerExitZone(int scoreType)
    {
        // リストにあれば削除
        if (activeZones.Contains(scoreType))
        {
            activeZones.Remove(scoreType);
            // 状況が変わったのでUIを更新
            UpdateAdviceText();
        }
    }

    // 現在のベストスコア（最も低い値）を計算する
    private int GetCurrentScore()
    {
        // どのゾーンにもいない場合は4点（枠外）
        if (activeZones.Count == 0) return 4;

        // リストを昇順（小さい順）にソート
        activeZones.Sort();

        // 最も良いスコア（0に近いもの）を返す
        return activeZones[0];
    }

    // UIテキストの更新処理
    private void UpdateAdviceText()
    {
        if (adviceText == null || isPhotoTaken) return;

        int currentScore = GetCurrentScore();

        // 4点（エリア外）のときだけ警告を表示
        if (currentScore == 4)
        {
            adviceText.text = "カメラに写っていません";
        }
        else
        {
            adviceText.text = ""; // 範囲内なら表示を消す
        }
    }

    // シャッターボタン等から呼ばれる撮影処理
    public void TakePhoto()
    {
        if (isPhotoTaken || StoryManager.Instance.isExcuting) return;
        isPhotoTaken = true;

        StartCoroutine(ProcessPhotoSession());
    }

    private IEnumerator ProcessPhotoSession()
    {
        // 1. シャッター演出
        if (AudioManager.Instance != null && shutterSound != null)
        {
            AudioManager.Instance.PlaySound(shutterSound, 0.4f);
        }

        if (flashEffect != null)
        {
            flashEffect.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            flashEffect.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 2. スコア確定
        int score = GetCurrentScore();

        // メッセージ取得（配列外参照を防ぐ）
        string[] message = new string[0];
        if (resultMessages != null && score < resultMessages.Length)
        {
            message = resultMessages[score].messages;
        }

        Debug.Log($"撮影完了。立ち位置スコア: {score}");

        // 3. スコア送信
        if (PersonalityManager.Instance != null)
        {
            PersonalityManager.Instance.AddFacetScore(personalityFacet, score);
        }

        // 4. メッセージ表示とシーン遷移
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.StartMiddleDialogue(message, () =>
            {
                StoryManager.Instance.MoveNextScene();
            });
        }
    }
}