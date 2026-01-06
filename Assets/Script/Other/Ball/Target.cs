using UnityEngine;
using TMPro; // TextMeshProを使う場合
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// 的のスクリプト。
/// 物理衝突(OnCollisionEnter)の代わりに、BallTossからスコア情報を受け取り、
/// 表示と音の再生のみを行うように修正。
/// </summary>
public class Target : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("スコアを表示するTextMeshPro UI")]
    public TextMeshProUGUI scoreText;
    [Tooltip("獲得したスコアの合計")]
    public Text allScoreText;
    private int allScore = 0;

    [Tooltip("スコア表示が消えるまでの時間")]
    public float scoreDisplayTime = 2.0f;

    [Header("効果音 (オプション)")]
    [Tooltip("BallToss.csから渡される音。Inspectorでの設定は不要な場合もあります")]
    public AudioClip hitExcellentSound;
    public AudioClip hitGoodSound;
    public AudioClip hitNormalSound;
    public AudioClip hitBadSound;

    [Header("AudioSource")]
    [Tooltip("効果音の再生用AudioSource")]
    public AudioSource audioSource;

    private Coroutine scoreDisplayCoroutine;

    void Start()
    {
        if (scoreText != null) scoreText.gameObject.SetActive(false);

        // audioSource が Inspector で設定されていない場合、自動で取得・追加する
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (allScoreText != null)
        {
            allScoreText.text = "";
        }
    }



    /// <summary>
    /// ボールが着弾したことを通知され、スコアと結果を表示します。
    /// </summary>
    /// <param name="score">表示するスコア値</param>
    /// <param name="qualityText">表示する品質テキスト (例: "Excellent!")</param>
    /// <param name="clipToPlay">再生する効果音</param>
    public void DisplayResult(int score, string qualityText, AudioClip clipToPlay)
    {
        // スコア表示
        ShowScore(score, qualityText);
        ShowAllScore(score);
        // 効果音再生
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
        else
        {
            Debug.LogWarning("再生するAudioClipが指定されていません。", this);
        }
    }

    // スコア表示処理 
    public void ShowScore(int score, string qualityText)
    {
        if (scoreText == null) return;

        scoreText.text = $"{qualityText}\n{score} Points";
        scoreText.gameObject.SetActive(true);

        // 古いコルーチンが動いていれば停止
        if (scoreDisplayCoroutine != null)
        {
            StopCoroutine(scoreDisplayCoroutine);
        }
        // 新しいコルーチンを開始
        scoreDisplayCoroutine = StartCoroutine(HideScoreText());
    }

    // (HideScoreText は変更なし)
    private IEnumerator HideScoreText()
    {
        yield return new WaitForSeconds(scoreDisplayTime);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
    }

    // オールスコア表示処理 
    public void ShowAllScore(int score)
    {
        allScore += score;
        if (allScoreText == null) return;

        allScoreText.text = $"{allScore}";
    }
}

