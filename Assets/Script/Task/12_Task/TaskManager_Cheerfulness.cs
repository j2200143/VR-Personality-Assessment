using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;

public class TaskManager_Cheerfulness : MonoBehaviour
{
    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.E6_Cheerfulness;

    [Header("メッセージ")]
    public string[] startMessage = {
        "素晴らしい腕前でした！",
        "褒美として、宝箱を差し上げましょう",
        "広場の中央に出現させたので確認してください"
    };
    public string[] questionMessage = {
        "ブーメランです！面白いものでしょう？",
    };
    public string[] afterQuestionMessage = {
        "あちらの少し広い場所で投げてみてはいかがですか？",
    };

    [Header("宝箱")]
    public GameObject chest;//煙のエフェクト、宝箱を空けるボタン付き
    public Transform chestCover;//宝箱の蓋
    public Button chestOpenButton;
    [Tooltip("Chestが開いたときのX軸の角度")]
    public float targetCoverRotationX = -110f;
    [Tooltip("Chest開くまでの時間（秒）")]
    public float coverAnimDuration = 1.5f;
    [Header("宝箱の中身")]
    public GameObject boomerangInChest;
    [Header("効果音")]
    public AudioClip audioClip_OpenChest;

    [Header("選択肢")]
    public string btnAMessage = "面白いね！ありがとう！", btnBMessage = "何も言わずにアイテムを受け取る", btnCMessage = "もっと良いものがよかった…";
    public GameObject choiceObject;
    public Button btnA, btnB, btnC;
    public Text btnAText, btnBText, btnCText;

    [Header("目的地")]
    public GameObject targetObject;//目的地を示すオブジェクト
    public GameObject noMoveCanvas;//「ブーメランを試さない」ボタンを持つキャンバス
    public Button noMoveButton;

    void Start()
    {
        if (chestOpenButton != null) chestOpenButton.onClick.AddListener(OpenChest);

        btnA.onClick.AddListener(() => ChoiceBtn(0));
        btnB.onClick.AddListener(() => ChoiceBtn(1));
        btnC.onClick.AddListener(() => ChoiceBtn(2));

        btnAText.text = btnAMessage;
        btnBText.text = btnBMessage;
        btnCText.text = btnCMessage;

        if (noMoveButton != null) noMoveButton.onClick.AddListener(SkipBoomerang);

        // 初期非表示
        if (chest != null) chest.SetActive(false);
        if (choiceObject != null) choiceObject.SetActive(false);
        if (targetObject != null) targetObject.SetActive(false);
        if (noMoveCanvas != null) noMoveCanvas.SetActive(false);
    }
    public void StartTask()
    {
        StoryManager.Instance.StartMiddleDialogue(startMessage, () =>
        {
            //宝箱を表示
            chest.SetActive(true);
        });
    }

    //宝箱が開く演出
    public void OpenChest()
    {
        chestOpenButton.gameObject.SetActive(false);// 連打防止

        // 蓋を開けるアニメーション
        AudioManager.Instance.PlaySound(audioClip_OpenChest, 1f);
        chestCover.DOLocalRotate(new Vector3(targetCoverRotationX, 0, 0), coverAnimDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // エフェクト再生
                if (boomerangInChest != null) boomerangInChest.SetActive(true);

                // 少し待ってから感想を聞く
                StartCoroutine(ShowQuestionDelay());
            });
    }
    private IEnumerator ShowQuestionDelay()
    {
        yield return new WaitForSeconds(1.0f);

        StoryManager.Instance.StartMiddleDialogue(questionMessage, () =>
        {
            choiceObject.SetActive(true);
        });
    }

    //選択肢を選んだ反応
    public void ChoiceBtn(int index)
    {
        choiceObject.SetActive(false);
        //選択肢によるスコア加算
        int firstStepScore = (int)(float)PersonalityManager.TASK_MAX_SCORE / 4;
        int score = 0;
        switch (index)
        {
            case 0:// A: 面白い (肯定)
                score = firstStepScore * 2;
                break;
            case 1:// B: 無言 (中立)
                score = firstStepScore;
                break;
            case 2:// C: 不満 (否定)
                score = 0;
                break;
        }

        PersonalityManager.Instance.AddFacetScore(personalityFacet, score);

        //宝箱の中身を非表示にする.受け取った合図として
        boomerangInChest.SetActive(false);


        StoryManager.Instance.StartMiddleDialogue(afterQuestionMessage, () =>
        {
            //目的地提示
            targetObject.SetActive(true);


            //目的地に行かない場合にシーン転換するためのボタン表示
            noMoveCanvas.SetActive(true);
        });
    }

    // ブーメランを試さずに進む場合
    public void SkipBoomerang()
    {
        noMoveCanvas.SetActive(false);

        // 行動スコア加算なし（0点）で終了
        Debug.Log("ブーメランを試さず終了 -> 次のシーンへ");
        StoryManager.Instance.MoveNextScene();
    }

}
