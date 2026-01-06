using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


/// <summary>
/// 物体の詳細を提示するため（絵画や花に注目するため）のクラス。
/// IInteractableを実装し、InteractionManagerから直接操作できる。
/// </summary>
/// 
public class InspectableObject : MonoBehaviour, IInteractable
{
    [Header("物体の名前")]
    public string objectName = "絵画";

    [Header("ファセット測定に関連するなら設定する")]
    public bool isRelationScore = false;
    public PersonalityFacet personalityFacet = PersonalityFacet.O2_ArtisticInterests;//O2:Artistic interests-審美性
    private bool isScored = false;//スコア送信したならtrue
    [Tooltip("ファセットを測定するオブジェクトがこのSceneに何個あるか")]
    public int facetObjectNum = 2;
    [Header("照準が当たった時に表示されるテキスト")]
    [TextArea(3, 10)]
    public string beforeActionString = "調べる";
    [Header("トリガーが押された場合に表示するテキスト")]
    [TextArea(3, 10)]
    public string afterActionString = "美しい油絵だ。";

    [System.NonSerialized]
    public bool isExcuting = false;//InteractionManagerがセリフの途中でShowInteractionUI()実行しないように

    [Header("参照")]
    public Text messageText; // 調査テキスト表示用
    public GameObject InspectableObjectCanvas; // 調査Canvas
    [Header("プレイヤーの向きにCanvasを回転させるかどうか")]
    public bool isCanRotate = false;

    void Start()
    {
        if (InspectableObjectCanvas != null && InspectableObjectCanvas.activeSelf)
        {
            InspectableObjectCanvas.gameObject.SetActive(false);
        }
    }
    void Update()
    {
        if (isExcuting)
        {
            if (Camera.main != null && isCanRotate)//Textをプレイヤーの方に向ける
            {
                InspectableObjectCanvas.transform.LookAt(Camera.main.transform.position);//Canvasごと傾くので壁などにめりこむ
                InspectableObjectCanvas.transform.forward = -InspectableObjectCanvas.transform.forward;
            }
        }
    }
    #region IInteractableの実装
    public void ShowCanvas()
    {
        if (!isExcuting)
        {
            if (messageText == null)
            {
                Debug.Log("messageTextに対象のCanvasのTextがアタッチされていません");
            }
            else
            {
                int baseSize = messageText.fontSize;// 元のフォントサイズを取得
                int largeSize = Mathf.RoundToInt(baseSize * 1.2f);// 1.2倍のサイズを計算 (整数に丸める)
                                                                  //messageText.text = "<size=200>" + objectName + "</size>" + "\n<size=150>" + beforeActionString + ":トリガー      閉じる:B</size>";
                messageText.text = "<size=" + largeSize + ">" + objectName + "</size>" + "\n" + beforeActionString + ":Aボタン      閉じる:B";
                if (StoryManager.Instance.isPCMode)
                {
                    //messageText.text = "<size=200>" + objectName + "</size>" + "\n<size=150>" + beforeActionString + ":左クリック     閉じる:B</size>";
                    messageText.text = "<size=" + largeSize + ">" + objectName + "</size>" + "\n" + beforeActionString + ":左クリック     閉じる:B";
                }
            }
        }

        InspectableObjectCanvas.gameObject.SetActive(true);

        if (Camera.main != null && isCanRotate)//Textをプレイヤーの方に向ける
        {
            InspectableObjectCanvas.transform.LookAt(Camera.main.transform.position);//Canvasごと傾くので壁などにめりこむ
            InspectableObjectCanvas.transform.forward = -InspectableObjectCanvas.transform.forward;
        }
    }
    public void Interact()
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundInspectable, AudioManager.Instance.Normal);

        ShowMessageText();
        isExcuting = true;
        // スコア送信（仮の例）
        if (isRelationScore && !isScored)
        {
            isScored = true;
            PersonalityManager.Instance.AddFacetScore(personalityFacet, PersonalityManager.TASK_MAX_SCORE / facetObjectNum);
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
    public void SetEnd()
    {
        if (InspectableObjectCanvas.activeSelf)
        {
            InspectableObjectCanvas.SetActive(false);
        }
        isExcuting = false;
    }
    #endregion

    void ShowMessageText()
    {
        if (messageText == null || InspectableObjectCanvas == null) return;

        messageText.text = afterActionString;

        CancelInvoke("SetEnd"); // 前のInvokeがあればキャンセル
        Invoke("SetEnd", 3f); // 3秒後に非表示
        //インタラクト終了
    }
}
