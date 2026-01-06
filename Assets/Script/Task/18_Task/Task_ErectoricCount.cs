using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// 処罰ボタンタスクの管理クラス
/// </summary>
public class Task_ErectoricCount : MonoBehaviour
{
    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.O6_Liberalism;

    [Header("電流のエフェクト")]
    public GameObject erectoricEffect;

    [Header("お仕置きを受けているNPC")]
    public Animator npcAnimator;
    public NPC npc;

    [System.Serializable]
    public class MessageGroup
    {
        [TextArea(2, 3)]
        public string[] messages;
    }
    [Header("タスク終了時に表示するメッセージ")]
    [Tooltip("Element 0:最低評価(10回以上) ～ Element 4:最高評価(0回) の順で設定")]
    public MessageGroup[] endMessage;

    [Header("ボタン")]
    public Button erectoricButton, endButton;
    [Header("ボタンアニメーション用")]
    public Transform touchTransform; // ボタンの可動部分
    public Transform afterTouchTransform; // 押し込まれた位置（目標地点）

    [Header("効果音")]
    public AudioClip audioClip_CryVoice;
    public AudioClip audioClip_Erectoric;

    // アニメーション設定
    private const string animatorDamageParameterName = "isDamage";

    // 電流ボタンを何回押したかカウント
    private int erectoricCount = 0;

    // 連打防止用フラグ
    private bool isProcessing = false;

    void Start()
    {
        if (erectoricEffect != null)
        {
            erectoricEffect.SetActive(false);
        }

        erectoricButton.onClick.AddListener(ErectoricButton);
        endButton.onClick.AddListener(EndTask);
    }

    // 電流ボタンにアタッチ
    public void ErectoricButton()
    {
        // 処理中なら反応しない（連打防止）
        if (isProcessing) return;

        StartCoroutine(ProcessPunishment());
    }

    // 処罰演出のコルーチン
    private IEnumerator ProcessPunishment()
    {
        isProcessing = true;
        erectoricCount++;

        // ボタンが押された場合の演出イベント

        //ボタンのアニメーション (DOTween)
        // 押し込んで戻る
        if (touchTransform != null && afterTouchTransform != null)
        {
            Vector3 originalPos = touchTransform.position;
            Sequence seq = DOTween.Sequence();
            seq.Append(touchTransform.DOMove(afterTouchTransform.position, 0.2f));
            seq.Append(touchTransform.DOMove(originalPos, 0.2f));
            yield return seq.WaitForCompletion();
        }
        else
        {
            yield return new WaitForSeconds(0.5f); // アニメーションがない場合のウェイト
        }

        // 叫び声再生
        if (AudioManager.Instance != null && audioClip_CryVoice != null)
        {
            AudioManager.Instance.PlaySound(audioClip_CryVoice, 1f);
        }

        //NPCのセリフ表示
        if (erectoricCount % 3 == 0)
        {
            npc.ShowFixedCanvas("ぐああ！");
        }
        else if (erectoricCount % 3 == 1)
        {
            npc.ShowFixedCanvas("あああ！");
        }
        else if (erectoricCount % 3 == 2)
        {
            npc.ShowFixedCanvas("ああ");
        }


        // アニメーター遷移（ダメージモーション開始）
        if (npcAnimator != null)
        {
            npcAnimator.SetBool(animatorDamageParameterName, true);
        }

        //電流音声再生
        if (AudioManager.Instance != null && audioClip_Erectoric != null)
        {
            AudioManager.Instance.PlaySound(audioClip_Erectoric, 1f);
        }
        // エフェクト再生（表示）
        if (erectoricEffect != null)
        {
            erectoricEffect.SetActive(true);
        }

        // 1秒待機（演出時間）
        yield return new WaitForSeconds(1.0f);

        // エフェクト停止
        if (erectoricEffect != null)
        {
            erectoricEffect.SetActive(false);
        }

        // アニメーション戻す
        if (npcAnimator != null)
        {
            npcAnimator.SetBool(animatorDamageParameterName, false);
        }

        isProcessing = false;
    }

    // 終了ボタンにアタッチ
    public void EndTask()
    {
        // 処理中（電流中）なら終われないようにする場合
        if (isProcessing) return;

        //連打防止
        endButton.gameObject.SetActive(false);

        int maxScore = PersonalityManager.TASK_MAX_SCORE;
        int firstStepScore = maxScore / 4;

        int score = 0;
        string[] message;

        // 判定ロジック
        if (erectoricCount >= 10)
        {
            // 0点 (最低評価)
            score = 0;
            message = endMessage[0].messages;
        }
        else if (erectoricCount >= 6)
        {
            // 1点相当
            score = firstStepScore * 1;
            message = endMessage[1].messages;
        }
        else if (erectoricCount >= 3)
        {
            // 2点相当
            score = firstStepScore * 2;
            message = endMessage[2].messages;
        }
        else if (erectoricCount >= 1)
        {
            // 3点相当
            score = firstStepScore * 3;
            message = endMessage[3].messages;
        }
        else
        {
            // 4点 (最高評価: 0回)
            score = firstStepScore * 4;
            message = endMessage[4].messages;
        }

        Debug.Log($"処罰回数: {erectoricCount}, スコア: {score}");

        // スコア送信
        if (PersonalityManager.Instance != null)
        {
            PersonalityManager.Instance.AddFacetScore(personalityFacet, score);
        }

        // メッセージを表示してシーン遷移
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.StartMiddleDialogue(message, () =>
            {
                StoryManager.Instance.MoveNextScene();
            });
        }
    }
}