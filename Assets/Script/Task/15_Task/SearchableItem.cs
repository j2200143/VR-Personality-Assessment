using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// 探索対象アイテム
/// InspectableObjectをベースに、探索タスク用の機能を追加
/// </summary>
public class SearchableItem : MonoBehaviour, IInteractable
{
    [Header("探索対象の設定")]
    [Tooltip("これが正解のオブジェクトか？")]
    public bool isTargetObject = false;

    [Header("テキスト設定")]
    [Tooltip("照準が当たった時に表示されるテキスト")]
    [TextArea(3, 10)]
    public string beforeActionString = "調べる";
    [Tooltip("調べた時のメッセージ")]
    [TextArea(3, 10)]
    public string message = "落とし物ではないようだ……";
    [Tooltip("対象オブジェクトを見つけた際にプレイヤーに表示するメッセージ")]
    public string[] showMessage = { "無くし物を見つけましたね", "落とし主に届けに行きましょう" };


    [Header("参照")]
    public GameObject InspectableObjectCanvas; // 調査Canvas
    public Text messageText; // 調査テキスト表示用

    [Header("プレイヤーの向きにCanvasを回転させるかどうか")]
    public bool isCanRotate = true; // デフォルトで回転有効にする

    private bool isChecked = false;
    private bool isExcuting = false; // InteractionManager制御用

    void Start()
    {
        // 初期状態でCanvasを非表示にする
        if (InspectableObjectCanvas != null)
        {
            InspectableObjectCanvas.SetActive(false);
        }
    }

    void Update()
    {
        // 実行中かつ回転有効なら、プレイヤー（カメラ）の方に向ける
        if (isExcuting && InspectableObjectCanvas != null && InspectableObjectCanvas.activeSelf)
        {
            if (Camera.main != null && isCanRotate)
            {
                InspectableObjectCanvas.transform.LookAt(Camera.main.transform.position);
                InspectableObjectCanvas.transform.forward = -InspectableObjectCanvas.transform.forward; // 反転対策
            }
        }
    }

    // --- IInteractable 実装 ---

    public void ShowCanvas()
    {
        // 調査済みでなく、実行中でない場合にCanvasを表示
        if (!isChecked && !isExcuting && InspectableObjectCanvas != null)
        {
            if (messageText != null)
            {
                // 「調べる」などのテキストを表示
                // InspectableObjectと同様のフォーマットを使用
                messageText.text = "<size=200>" + (isTargetObject ? "何かある..." : "何かある...") + "</size>" + "\n<size=150>" + beforeActionString + ":トリガー</size>";
            }
            InspectableObjectCanvas.SetActive(true);

            // 表示時にプレイヤーの方に向ける
            if (Camera.main != null && isCanRotate)
            {
                InspectableObjectCanvas.transform.LookAt(Camera.main.transform.position);
                InspectableObjectCanvas.transform.forward = -InspectableObjectCanvas.transform.forward;
            }
        }
    }

    public void Interact()
    {
        if (isChecked) return; // 一度調べたら終わり

        isChecked = true;
        isExcuting = true; // 実行中フラグを立てる

        // 調査結果のメッセージを表示
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundInspectable, AudioManager.Instance.Normal);

        messageText.text = message;
        InspectableObjectCanvas.SetActive(true);

        //対象オブジェクトなら
        if (isTargetObject)
        {
            if (Task_LostItem_O3.Instance != null)
            {
                Task_LostItem_O3.Instance.OnFindTarget();
            }

            //対象アイテムならオブジェクトを非表示にする
            CancelInvoke("HideObjectAndMessage");
            Invoke("HideObjectAndMessage", 1.5f);
        }
        else
        {
            if (Task_LostItem_O3.Instance != null)
            {
                Task_LostItem_O3.Instance.OnCheckDummy();
            }

            // 数秒後に非表示にする
            CancelInvoke("SetEnd");
            Invoke("SetEnd", 3f);
        }
    }

    public bool CheckExcute()
    {
        return isExcuting;
    }

    public GameObject GetInspectableCanvas()
    {
        return InspectableObjectCanvas;
    }


    //非表示にする
    public void SetEnd()
    {
        if (InspectableObjectCanvas != null && InspectableObjectCanvas.activeSelf)
        {
            InspectableObjectCanvas.SetActive(false);
        }
        isExcuting = false;
    }

    //オブジェクトを非表示・メッセージ表示
    public void HideObjectAndMessage()
    {
        gameObject.SetActive(false);
        isExcuting = false;

        StoryManager.Instance.StartMiddleDialogue(showMessage);
    }
}