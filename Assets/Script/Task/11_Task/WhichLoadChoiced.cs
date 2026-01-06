using UnityEngine;

/// <summary>
/// 分かれ道でどちらを選んだかを判定し、スコアを加算するスクリプト。
/// 道の入り口や出口に設置した透明な壁（Trigger）にアタッチします。
/// </summary>
[RequireComponent(typeof(Collider))]
public class PathChoiceDetector : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("この道が測定するファセット")]
    public PersonalityFacet targetFacet = PersonalityFacet.E5_ExcitementSeeking;

    [Tooltip("この道を選んだ時に加算するスコア（例：刺激的な道=2, 通常の道=0）")]
    public int scoreToAdd = 2;

    [Header("参照")]
    [Tooltip("もう一方の道の判定オブジェクト（こちらを通ったら、あちらは無効化する）")]
    public GameObject otherPathDetector;

    // 二重反応防止フラグ
    private bool hasTriggered = false;

    /// <summary>
    /// プレイヤーがトリガーに接触した瞬間に呼ばれる
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // すでに判定済みなら何もしない
        if (hasTriggered) return;

        // プレイヤーかどうか判定 ("Player"タグがついている前提)
        if (other.CompareTag("Player"))
        {
            DetectChoice();
        }
    }

    private void DetectChoice()
    {
        hasTriggered = true;
        Debug.Log($"道を選択しました: {gameObject.name} (Score: {scoreToAdd})");

        // 1. スコアを加算
        if (PersonalityManager.Instance != null)
        {
            PersonalityManager.Instance.AddFacetScore(targetFacet, scoreToAdd);
        }
        else
        {
            Debug.LogError("PersonalityManager がシーンに存在しません！");
        }

        // 2. もう一方の道の判定を無効化（後戻りして両方のスコアが入るのを防ぐ）
        if (otherPathDetector != null)
        {
            otherPathDetector.SetActive(false);
        }

        // 3. このオブジェクト自体も無効化または削除
        gameObject.SetActive(false);
    }
}