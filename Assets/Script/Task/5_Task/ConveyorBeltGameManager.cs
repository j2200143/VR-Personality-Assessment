using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // シャッフル用に必要

public class ConveyorBeltGameManager : MonoBehaviour
{
    [Header("コンテナ設定")]
    [Tooltip("シーン内に配置されているコンテナの実体を全て登録してください")]
    public GameObject[] containerObjects;

    [Tooltip("コンテナが流れる速度（秒）")]
    public float moveTime = 3.0f;
    [Tooltip("次のコンテナが流れてくるまでの間隔（秒）")]
    public float spawnInterval = 3.0f;

    [Header("位置設定")]
    public Transform startPos;
    public Transform showPos;
    public Transform checkPos;
    public Transform rightLaneEndPos;
    public Transform leftLaneEndPos;

    [Header("スタック位置")]
    public Transform[] rightStackPositions;
    public Transform[] leftStackPositions;

    [Header("レバー設定")]
    public Transform leverModel;
    public float rightRotationAngle = 45f;
    public float leftRotationAngle = -45f;

    [Header("ガイド表示")]
    public GameObject rightArrowGroup;
    public GameObject leftArrowGroup;

    [Header("スコア表示")]
    public Text scoreText;

    [Header("ゲーム開始ボタン")]
    public Button startButton;

    [Header("効果音")]
    public AudioClip audioClip_Lever;
    public AudioClip audioClip_ConveyorBelt;
    public AudioClip audioClip_Correct;
    // --- 内部変数 ---
    private bool isLeverRight = true;
    private int currentStackIndexRight = 0;
    private int currentStackIndexLeft = 0;
    private int correctCount = 0;
    private int wrongCount = 0;
    private int processedCount = 0;

    // 処理順を決めるためのキュー
    private Queue<GameObject> containerQueue;

    [Header("タスク開始用")]
    public ImmoderationManager immoderationManager;

    void Start()
    {
        // 初期化
        if (startButton != null)
        {

            startButton.onClick.AddListener(StartGame);
        }
        UpdateLeverVisuals();
        scoreText.text = "";

        // コンテナの準備
        SetupContainers();

    }

    /// <summary>
    /// コンテナを初期位置に移動させ、順番をランダムにする
    /// </summary>
    private void SetupContainers()
    {
        // 配列をランダムにシャッフルしてキューに入れる
        // (using System.Linq; が必要)
        var shuffledList = containerObjects.OrderBy(x => System.Guid.NewGuid()).ToList();
        containerQueue = new Queue<GameObject>(shuffledList);

        // 全てのコンテナをスタート位置（壁の裏など）に移動させ、非表示にしておく
        foreach (var container in containerObjects)
        {
            container.transform.position = startPos.position;
            container.transform.rotation = Quaternion.identity;
            container.SetActive(false); // 出番が来るまで隠しておく
        }
    }

    public void StartGame()
    {
        if (!StoryManager.Instance.isExcuting)
        {
            // ゲームループ開始
            StartCoroutine(GameLoop());

            startButton.gameObject.SetActive(false);
        }

    }
    /// <summary>
    /// ゲーム進行ループ
    /// </summary>
    private IEnumerator GameLoop()
    {
        //ベルトコンベアの効果音再生
        AudioManager.Instance.PlayLoopingSoundSub(audioClip_ConveyorBelt);

        // キューが空になるまで（全コンテナを処理するまで）繰り返す
        while (containerQueue.Count > 0)
        {
            // 次のコンテナを取り出す
            GameObject currentContainer = containerQueue.Dequeue();

            // 待ち状態を示すフラグ
            bool reachedCheckPoint = false;

            // 移動開始。コールバックでフラグを立てる処理を渡す
            MoveContainer(currentContainer, () =>
      {
          reachedCheckPoint = true;
      });

            yield return new WaitUntil(() => reachedCheckPoint);

            // 分岐点に到達したら、次のコンテナが流れてくるまでの間隔を空ける
            // ※ここでは、spawnIntervalは「次のコンテナが流れ始めるまでの準備時間」として機能します
            yield return new WaitForSeconds(spawnInterval);
        }

        // 処理が終わった時のチェックはMoveContainerのOnCompleteに任せるため、ここでは不要
    }
    /// <summary>
    /// コンテナを表示して移動を開始させる
    /// </summary>
    private void MoveContainer(GameObject container, System.Action onCheckPointReached)
    {
        // 表示オン
        container.SetActive(true);

        ContainerInfo info = container.GetComponent<ContainerInfo>();

        // --- シーケンス1：スタートから分岐点まで ---
        Sequence seq1 = DOTween.Sequence();

        // 1. 出現 -> 分岐点へ
        seq1.Append(container.transform.DOMove(showPos.position, moveTime).SetEase(Ease.Linear));
        seq1.Append(container.transform.DOMove(checkPos.position, moveTime).SetEase(Ease.Linear));

        // 2. 分岐点到達時の処理
        seq1.OnComplete(() =>
        {
            // GameLoopに通知
            onCheckPointReached?.Invoke();

            // 判定ロジック
            bool isSuccess = JudgeContainer(info);
            DisplayResult(isSuccess);

            // この時点でのレバーの向きに基づいて、後半の移動を開始する
            MoveContainerAfterCheck(container, isLeverRight);
        });
    }

    /// <summary>
    /// 分岐点以降の移動処理（動的に分岐）
    /// </summary>
    private void MoveContainerAfterCheck(GameObject container, bool moveRight)
    {
        Sequence seq2 = DOTween.Sequence();

        if (moveRight)
        {
            // 右レーンへ
            seq2.Append(container.transform.DOMove(rightLaneEndPos.position, moveTime).SetEase(Ease.Linear));

            // スタック位置へ
            if (currentStackIndexRight < rightStackPositions.Length)
            {
                seq2.Append(container.transform.DOMove(rightStackPositions[currentStackIndexRight].position, 1f));
                seq2.Join(container.transform.DORotate(new Vector3(0, 90, 0), 1f));
                currentStackIndexRight++;
            }
            else
            {
                // スタック満杯時
                seq2.Append(container.transform.DOScale(0, 0.5f));
                seq2.AppendCallback(() => container.SetActive(false));
            }
        }
        else
        {
            // 左レーンへ
            seq2.Append(container.transform.DOMove(leftLaneEndPos.position, moveTime).SetEase(Ease.Linear));

            // スタック位置へ
            if (currentStackIndexLeft < leftStackPositions.Length)
            {
                seq2.Append(container.transform.DOMove(leftStackPositions[currentStackIndexLeft].position, 1f));
                seq2.Join(container.transform.DORotate(new Vector3(0, -90, 0), 1f));
                currentStackIndexLeft++;
            }
            else
            {
                // スタック満杯時
                seq2.Append(container.transform.DOScale(0, 0.5f));
                seq2.AppendCallback(() => container.SetActive(false));
            }
        }

        // 終了判定
        seq2.OnComplete(() =>
        {
            processedCount++;
            if (processedCount >= containerObjects.Length)
            {
                Debug.Log("ゲーム終了");
                AudioManager.Instance.StopLoopingSoundSub();

                // タスクを開始する
                if (immoderationManager != null)
                {
                    immoderationManager.StartTask();
                }
            }
        });
    }

    // --- 以下、レバー操作や判定ロジック ---

    public void ToggleLever()
    {
        isLeverRight = !isLeverRight;
        UpdateLeverVisuals();

        AudioManager.Instance.PlaySound(audioClip_Lever, 1f);
    }

    private void UpdateLeverVisuals()
    {
        if (isLeverRight)
        {
            leverModel.DOLocalRotate(new Vector3(rightRotationAngle, 0, 0), 0.3f);
            if (rightArrowGroup)
                rightArrowGroup.SetActive(true);

            if (leftArrowGroup)
                leftArrowGroup.SetActive(false);
        }
        else
        {
            leverModel.DOLocalRotate(new Vector3(leftRotationAngle, 0, 0), 0.3f);
            if (rightArrowGroup)
                rightArrowGroup.SetActive(false);

            if (leftArrowGroup)
                leftArrowGroup.SetActive(true);
        }
    }

    private bool JudgeContainer(ContainerInfo info)
    {
        bool isCorrect = false;
        // ContainerInfoがアタッチされていない場合の安全策
        if (info == null)
        {
            Debug.Log("コンテナオブジェクトにContainerInfoがアタッチされていません");
            return false;
        }

        if (info.type == ContainerType.Right && isLeverRight)
        {
            isCorrect = true;
        }
        else if (info.type == ContainerType.Left && !isLeverRight)
        {
            isCorrect = true;
        }

        if (isCorrect)
        {
            correctCount++;
            AudioManager.Instance.PlaySound(audioClip_Correct, 1f);
        }
        else
        {
            wrongCount++;
            AudioManager.Instance.PlaySound(AudioManager.Instance.soundMiss, 1f);
        }


        UpdateScoreUI();

        return isCorrect;
    }

    private void DisplayResult(bool isSuccess)
    {
        if (isSuccess) Debug.Log("〇 正解！");
        else Debug.Log("× 不正解...");
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            // 〇を赤(red)、×を黒(black)にする
            scoreText.text = $"<color=red>〇</color>: {correctCount}  <color=black>×</color>: {wrongCount}";
        }

    }
}