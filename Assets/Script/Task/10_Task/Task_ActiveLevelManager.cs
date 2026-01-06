using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class Task_ActiveLevelManager : MonoBehaviour
{
    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.E4_ActivityLevel;

    [Header("参照")]
    [Tooltip("プレイヤーオブジェクト（移動判定用）")]
    public CharacterMove characterMove;

    [Header("メッセージ")]
    public string[] startMessage = {
        "おや、岩が道を塞いでしまいましたね",
        "私が魔法でこの岩を全て片付けます",
        "1分ほどかかりますので、このあたりでしばらくお待ちください"
    };

    public string[] endMessage = {
        "お待たせしました。岩の撤去が完了しました",
        "先へ進みましょう"
    };

    [Header("計測設定")]
    [Tooltip("待機時間（秒）")]
    public float waitTimeDuration = 60f;

    [Header("岩イベント用")]
    [Tooltip("岩より奥に進むことができないようにするためのオブジェクト")]
    public GameObject obstacleObject;
    [Tooltip("転がす岩のオブジェクトリスト（手前から奥へ、片付けたい順に登録してください）")]
    public GameObject[] rockObjects;
    [Tooltip("斜面の終わり（坂下）の位置。岩の数だけ設定してください")]
    public Transform[] slopeBottomPositions;
    [Tooltip("各岩の到達点（rockObjectsと同じ数・順序で設定してください）")]
    public Transform[] endPositions;
    [Tooltip("岩が片付けられる移動先（岩の数と同じだけ、岩を置きたい場所に空のオブジェクトを配置して設定してください）")]
    public Transform[] cleanRockTransform;
    [Tooltip("岩が片付けられる際の経由地点（障害物を避けるための空中の点など）")]
    public Transform[] cleanHalfWayPoint;

    [Header("アニメーション設定")]
    [Tooltip("転がり終わるまでの時間（秒）")]
    public float rollDuration = 3.0f;
    [Tooltip("片付ける（移動する）ときのアニメーション時間（秒）")]
    public float cleanupAnimDuration = 4.0f;

    [Tooltip("回転量（転がる演出用）")]
    public Vector3 rotationAmount = new Vector3(0f, 0f, 720f);

    [Tooltip("砂のエフェクト")]
    public GameObject[] sandEffect;

    [Header("効果音")]
    public AudioClip audioClip_Rock;

    // 内部状態
    private bool isTaskRunning = false;
    private float currentWaitTime = 0f;

    // 測定データ
    private float activeTime = 0f;
    private float passiveTime = 0f;

    // 岩管理用
    private int removedRockCount = 0; // 既に片付けた岩のインデックス

    void Start()
    {
        // 初期状態では岩を表示しておく（または非表示にしてRollRocksで出す）
        // ここではRollRocksで移動させるため、初期位置にあると仮定します
        if (rockObjects != null)
        {
            foreach (var rock in rockObjects)
            {
                if (rock != null) rock.SetActive(true);
            }
        }

        //進行妨害オブジェクトを設置しプレイヤーが岩に巻き込まれないようにする
        obstacleObject.SetActive(true);
    }

    // TriggerEvent_Rockから呼ばれる
    public void RollRocks()
    {
        // 音再生
        if (AudioManager.Instance != null && audioClip_Rock != null)
        {
            AudioManager.Instance.PlayLoopingSound(audioClip_Rock);
        }
        //砂ぼこりエフェクト再生
        for (int i = 0; i < sandEffect.Length; i++)
        {
            sandEffect[i].SetActive(true);
        }
        // Sequence作成
        Sequence seq = DOTween.Sequence();

        // 回転量を定義（例：z軸に2回転 = 720度）
        Vector3 extraSpin = rotationAmount;

        // 全ての岩を目的地へ転がす
        for (int i = 0; i < rockObjects.Length; i++)
        {
            if (i >= endPositions.Length || rockObjects[i] == null) continue;

            Transform rock = rockObjects[i].transform;
            Transform slopeBottom = slopeBottomPositions[i];
            Transform target = endPositions[i];

            Vector3[] path = new Vector3[] { slopeBottom.position, target.position };

            // --- 移動アニメーション ---
            // PathType.Linear: 直線的に結ぶ（カクっと曲がる）。
            // 斜面と地面のつなぎ目を丸くしたい場合は PathType.CatmullRom を使いますが、
            // 岩が転がるなら Linear の方が地面に沿いやすいことが多いです。
            seq.Join(rock.DOPath(path, rollDuration, PathType.Linear).SetEase(Ease.InQuad));

            // --- 回転アニメーション（修正箇所） ---
            // 「ターゲットの最終角度」に「余分な回転量(720度など)」を足した値を目標にする
            Vector3 finalRotation = target.eulerAngles + extraSpin;

            // SetRelative は削除します。
            // RotateMode.FastBeyond360 を使うことで、360度を超えてグルグル回りながら、
            // 最終的に target.eulerAngles (の見た目) にピタリと着地します。
            seq.Join(rock.DORotate(finalRotation, rollDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.InQuad));
        }

        // 完了時の処理
        seq.OnComplete(() =>
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopLoopingSound();
            }

            // 念のため、位置と回転を完全に同期させる（補正）
            for (int i = 0; i < rockObjects.Length; i++)
            {
                if (i < endPositions.Length && rockObjects[i] != null)
                {
                    rockObjects[i].transform.position = endPositions[i].position;
                    rockObjects[i].transform.rotation = endPositions[i].rotation;
                }
            }

            // タスク開始
            StartTask();
        });
    }

    private void StartTask()
    {
        StoryManager.Instance.StartMiddleDialogue(startMessage, () =>
        {
            StartMeasurement();
        });
    }

    private void StartMeasurement()
    {
        Debug.Log("E4 Measurement Started");
        isTaskRunning = true;
        currentWaitTime = 0f;
        activeTime = 0f;
        passiveTime = 0f;
        removedRockCount = 0;
    }

    void Update()
    {
        if (!isTaskRunning) return;

        // 1. 時間経過
        currentWaitTime += Time.deltaTime;

        // 2. プレイヤーの状態判定
        CheckPlayerActivity();

        // 3. 岩の片付け処理（時間の進行に合わせて実行）
        UpdateRockCleanup();

        // 4. 終了判定
        if (currentWaitTime >= waitTimeDuration)
        {
            EndTask();
        }
    }

    /// <summary>
    /// 経過時間に応じて、岩を順番に消していく処理
    /// </summary>
    private void UpdateRockCleanup()
    {
        if (rockObjects == null || rockObjects.Length == 0) return;

        // 現在の進捗率 (0.0 ~ 1.0)
        // Clamp01で 1.0 を超えないようにする
        float progress = Mathf.Clamp01(currentWaitTime / waitTimeDuration);

        // この時間までに片付いているべき岩の個数 (進捗率 * 岩の総数)
        // 例: 進捗50%で岩が10個なら、5個消えているべき
        int targetRemovedCount = Mathf.FloorToInt(progress * rockObjects.Length);

        // まだ片付けていない岩があれば、片付ける
        while (removedRockCount < targetRemovedCount && removedRockCount < rockObjects.Length)
        {
            RemoveRock(removedRockCount);
            removedRockCount++;
        }
    }

    /// <summary>
    /// 指定インデックスの岩を移動させるアニメーション
    /// </summary>
    private void RemoveRock(int index)
    {
        GameObject rock = rockObjects[index];

        // 安全チェック
        if (rock == null || !rock.activeSelf) return;

        // 移動先と経由点があるかチェック
        if (cleanRockTransform == null || index >= cleanRockTransform.Length || cleanRockTransform[index] == null)
        {
            Debug.LogWarning($"Index {index} の cleanRockTransform が設定されていません。");
            return;
        }
        // 経由点がない場合は警告を出して中断（または直接移動にフォールバックしても良い）
        if (cleanHalfWayPoint == null || index >= cleanHalfWayPoint.Length || cleanHalfWayPoint[index] == null)
        {
            Debug.LogWarning($"Index {index} の cleanHalfWayPoint が設定されていません。");
            return;
        }

        Transform target = cleanRockTransform[index];
        Transform wayPoint = cleanHalfWayPoint[index];


        // --- 魔法で運ばれる演出 ---
        Sequence seq = DOTween.Sequence();

        // 1. 移動 (現在地 -> 経由点 -> 片付け場所)
        // パス配列を作成: { 経由点, 目的地 }
        Vector3[] path = new Vector3[] { wayPoint.position, target.position };

        // DOPathを使用
        // PathType.CatmullRom: 曲線で滑らかにつなぐ
        // PathType.Linear: 直線でつなぐ（カクカクする）
        seq.Join(rock.transform.DOPath(path, cleanupAnimDuration, PathType.CatmullRom)
            .SetEase(Ease.InOutSine));

        // 2. 回転 (ターゲットの回転に合わせる)
        seq.Join(rock.transform.DORotate(target.eulerAngles, cleanupAnimDuration)
            .SetEase(Ease.InOutSine));
    }

    private void CheckPlayerActivity()
    {
        bool isActive = false;

        if (characterMove != null && characterMove.IsMoving) isActive = true;

        if (InteractionManager.Instance != null && InteractionManager.Instance.activeInteractionCount > 0)
            isActive = true;

        if (isActive) activeTime += Time.deltaTime;
        else passiveTime += Time.deltaTime;
    }

    private void EndTask()
    {
        isTaskRunning = false;
        Debug.Log($"E4 Task Ended. Active: {activeTime:F1}s, Passive: {passiveTime:F1}s");

        // 念のため残っている岩があれば全て消す（誤差対策）
        for (int i = removedRockCount; i < rockObjects.Length; i++)
        {
            RemoveRock(i);
        }

        CalculateAndSendScore();

        //進行妨害オブジェクトを非表示にする
        obstacleObject.SetActive(false);

        // 終了メッセージ
        StoryManager.Instance.StartMiddleDialogue(endMessage);

    }

    private void CalculateAndSendScore()
    {
        float totalTime = activeTime + passiveTime;
        if (totalTime <= 0) return;

        float activeRatio = activeTime / totalTime;
        int score = 0;

        if (activeRatio < 0.2f) score = 0;
        else if (activeRatio < 0.4f) score = 1;
        else if (activeRatio < 0.6f) score = 2;
        else if (activeRatio < 0.8f) score = 3;
        else score = 4;

        Debug.Log($"E4 Score: {score} (Ratio: {activeRatio:P})");

        if (PersonalityManager.Instance != null)
        {
            PersonalityManager.Instance.AddFacetScore(personalityFacet, score);
        }
    }
}