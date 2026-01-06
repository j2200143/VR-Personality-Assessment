using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 見えない壁として、プレイヤーの行動を制御するクラス。
/// </summary>
public class WarningStoryObject : MonoBehaviour
{
    [Header("プレイヤーが衝突した際に表示するメッセージ")]
    public string message = "まだタスクが終了していません";
    [Header("メッセージを表示するCanvasとText")]
    public GameObject messageCanvas;
    public Text messageText;
    // メッセージが連続で表示されるのを防ぐためのフラグ
    private bool isShowingMessage = false;

    void Start()
    {
        if (messageCanvas != null)
        {
            messageCanvas.gameObject.SetActive(false);
        }
    }
    // 物理的な衝突が発生した時に呼び出されるメソッド
    private void OnCollisionEnter(Collision collision)
    {
        // 衝突した相手が"Player"タグでない、またはStoryManagerが設定されていない場合は何もしない
        if (!collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        if (!isShowingMessage && StoryManager.Instance.isExcuting == false)//メッセージが表示中でない場合
        {
            AudioManager.Instance.PlaySound(AudioManager.Instance.soundMiss, AudioManager.Instance.Half);
            StartCoroutine(ShowWarningCoroutine());
        }
    }

    // 警告メッセージを指定時間表示して非表示にするコルーチン
    private IEnumerator ShowWarningCoroutine()
    {
        isShowingMessage = true;

        // メッセージとCanvasを表示
        if (messageText != null)
        {
            messageText.text = message;
        }
        else
        {
            Debug.Log("messageTextをアタッチしていません。");
        }

        if (messageCanvas != null)
        {
            messageCanvas.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("messageCanvasをアタッチしていません。");
        }

        yield return new WaitForSeconds(3f);

        // Canvasを非表示にし、テキストをクリア
        if (messageText != null)
        {
            messageText.text = "";
        }

        if (messageCanvas != null)
        {
            messageCanvas.gameObject.SetActive(false);
        }

        isShowingMessage = false;
    }
}
