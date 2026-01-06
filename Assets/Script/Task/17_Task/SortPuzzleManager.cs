using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 並び替えパズルの問題管理と正解判定を行うメインスクリプト。
/// </summary>
public class SortPuzzleManager : MonoBehaviour
{
    [Header("問題番号")]
    public int thisQuestionID;

    [Header("回答スロット")]
    [Tooltip("回答スロット（DropSlot付き）のGameObjectを順番通りに設定")]
    public GameObject[] answerArray;

    [Header("確定ボタン")]
    public Button confirmButton;

    [Header("問題パネル")]
    public GameObject parentPanel;

    [Header("正答時の効果音")]
    public AudioClip soundCorrect;
    [Header("誤答時の効果音")]
    public AudioClip soundMiss;

    private bool _isAnswered = false;    // 解答済みかどうかのフラグ

    void Start()
    {
        // 確認ボタンにメソッドを登録
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(CheckAnswer);
        }

    }

    /// <summary>
    /// 「確定ボタン」から呼び出される正解判定メソッド
    /// </summary>
    public void CheckAnswer()
    {
        //メッセージ表示中なら受け付けない
        if (StoryManager.Instance.isExcuting)
        {
            return;
        }

        // answerArrayの全スロットを順番にチェック
        for (int i = 0; i < answerArray.Length; i++)
        {
            GameObject slot = answerArray[i];

            // --- チェック1：スロットが空でないか ---
            if (slot.transform.childCount == 0)
            {
                Debug.Log("不正解: スロット " + i + " が空です。");
                AudioManager.Instance.PlaySound(soundMiss, 1f);
                return; // 判定終了
            }

            // --- チェック2：スロットの中のアイテムの「whatNumber」が正しいか ---
            Transform itemInSlot = slot.transform.GetChild(0); // スロット内にあるアイテムを取得
            WhatNumber whatNum = itemInSlot.GetComponent<WhatNumber>();

            if (whatNum == null)
            {
                Debug.LogError("エラー: " + itemInSlot.name + " にWhatNumberスクリプトがありません。");
                return; // 判定終了
            }

            // アイテムの whatNumber が、スロットのインデックス (i) と一致しているか
            if (whatNum.whatNumber != i)
            {
                Debug.Log("不正解: スロット " + i + " にある " + itemInSlot.name + " (whatNumber=" + whatNum.whatNumber + ") は間違いです。");
                AudioManager.Instance.PlaySound(soundMiss, 1f);
                return; // 判定終了
            }
        }

        // --- 全てのスロットが正しかった場合 ---
        // ループを最後まで（returnされずに）抜けたら、全て正解
        AudioManager.Instance.PlaySound(soundCorrect, 0.8f);
        _isAnswered = true;

        //正解したらパネルを非表示にする
        parentPanel.SetActive(false);

        //正解した場合のイベント発生させる
        WisdomTrialEventManager.Instance.Event(thisQuestionID);
    }

    public void OpenPanel()
    {
        if (!_isAnswered)
        {
            AudioManager.Instance.PlaySound(AudioManager.Instance.soundClick, 1f);

            parentPanel.SetActive(true);
        }
    }
    public void ClosePanel()
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundClick, 1f);

        parentPanel.SetActive(false);
    }
}