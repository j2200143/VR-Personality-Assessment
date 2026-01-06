using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

/// <summary>
/// ポーション作成ミニゲームを管理するクラス。
/// 瓶選択 -> 葉選択 -> タイミングゲーム -> 結果表示 -> 反省会(ReflectManager)へ移行する。
/// </summary>
public class MakePotionManager : MonoBehaviour
{
    [Header("ReflectManager参照")]
    public ReflectManager reflectManager;

    [Header("指示1: 瓶の選択")]
    [Tooltip("選択前のテーブル上にある瓶")]
    public GameObject[] beforeBottle;
    [Tooltip("選んだ際に釜に注ぐ動きをする瓶（アニメーション用）")]
    public GameObject[] afterBottle;
    [Tooltip("瓶を傾ける角度")]
    public float bottleRotationNum = -45f;
    [Tooltip("瓶選択ボタン")]
    public Button[] bottleButton;
    [Tooltip("液体が落ちるエフェクト")]
    public GameObject dropEffect;

    [Header("指示2: 葉の選択")]
    public string[] instructionMessage_2 = { "次に、［月光草の葉］を一枚、釜に入れてください" };
    [Tooltip("選択前のテーブル上にある葉")]
    public GameObject[] beforeLeaf;
    [Tooltip("釜に入っていく動きをする葉")]
    public GameObject[] afterLeaf;
    [Tooltip("葉選択ボタン")]
    public Button[] leafButton;
    [Tooltip("葉が落ちた時のエフェクト（泡など）")]
    public GameObject dropleafEffect;
    [Tooltip("釜のポジション")]
    public Transform podTransform;

    [Header("指示3: タイミング")]
    public string[] instructionMessage_3 = { "最後に、適切なタイミングで仕上げの一滴を入れてください" };
    [Tooltip("タイミングゲームのCanvas")]
    public GameObject timingCanvas;
    [Tooltip("カーソルが移動するゲージ")]
    public Image gageImage;
    [Tooltip("往復するカーソル（Image）")]
    public RectTransform timingCursor;
    [Tooltip("カーソルの移動時間（片道）")]
    public float cursorSpeed = 0.8f;
    [Tooltip("ストップボタン")]
    public Button timingStopButton;

    [Header("結果演出")]
    [Tooltip("完成時の煙")]
    public GameObject smokeEffect;
    [Tooltip("完成したポーション")]
    public GameObject resultObject;

    [Header("効果音")]
    public AudioClip audioClip_Drop;
    public AudioClip audioClip_DropLeaf;
    public AudioClip audioClip_Smoke;
    public AudioClip audioClip_Timing; // タイミング決定音

    // 内部状態
    private Tween cursorTween;

    void Start()
    {
        // 初期化：ボタンイベント登録
        for (int i = 0; i < bottleButton.Length; i++)
        {
            int index = i; // クロージャ対策
            bottleButton[i].onClick.AddListener(() => ChoiceBottle(index));
        }

        for (int i = 0; i < leafButton.Length; i++)
        {
            int index = i;
            leafButton[i].onClick.AddListener(() => ChoiceLeaf(index));
        }

        if (timingStopButton != null)
        {
            timingStopButton.onClick.AddListener(StopTimingGame);
        }

        // 初期状態設定
        dropEffect.SetActive(false);
        dropleafEffect.SetActive(false);
        timingCanvas.SetActive(false);
        resultObject.SetActive(false);
        if (smokeEffect != null) smokeEffect.SetActive(false);
    }

    //天の声がまずはあの瓶を選んでくださいなど指示する
    // --- 指示1: 瓶の選択 ---
    public void ChoiceBottle(int index)
    {
        // ボタンを無効化（連打防止）
        foreach (var btn in bottleButton) btn.interactable = false;

        // 操作するオブジェクトの定義
        GameObject moveBottle = beforeBottle[index]; // 移動する瓶
        GameObject bottle = afterBottle[index];      // 注ぐ用の瓶

        // 一連のアニメーションを管理するシーケンスをここで作成
        Sequence seq = DOTween.Sequence();

        // 移動：beforeBottleをafterBottleの位置まで移動させる
        seq.Append(moveBottle.transform.DOMove(bottle.transform.position, 1f).SetEase(Ease.OutQuad));

        // 切り替え：移動が終わったら表示を切り替える
        seq.AppendCallback(() =>
        {
            moveBottle.SetActive(false);
            bottle.SetActive(true);
        });

        // 1. 傾ける
        seq.Append(bottle.transform.DOLocalRotate(new Vector3(bottleRotationNum, 0, 0), 0.5f));

        // 2. 液体エフェクトON & 音再生
        seq.AppendCallback(() =>
        {
            dropEffect.SetActive(true);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(audioClip_Drop, 1f);
        });

        seq.AppendInterval(0.1f); // 注いでいる時間

        // 3. 元に戻す & エフェクトOFF
        seq.AppendCallback(() => dropEffect.SetActive(false));
        seq.Append(bottle.transform.DOLocalRotate(Vector3.zero, 0.5f));

        // 4. 瓶を消して次の指示へ
        seq.OnComplete(() =>
        {
            bottle.SetActive(false);

            // 次の指示（葉の選択）を表示
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.StartMiddleDialogue(instructionMessage_2, () =>
                {
                    // メッセージ読了後に葉のボタンを有効化
                    foreach (var btn in leafButton) btn.gameObject.SetActive(true);
                });
            }
        });
    }

    // --- 指示2: 葉の選択 ---
    public void ChoiceLeaf(int index)
    {
        // ボタン無効化
        foreach (var btn in leafButton) btn.interactable = false;

        GameObject moveLeaf = beforeLeaf[index]; // 手元から移動する葉

        Sequence seq = DOTween.Sequence();

        // 移動：（釜の上）の位置まで移動
        seq.Append(moveLeaf.transform.DOMove(afterLeaf[index].transform.position, 1.0f).SetEase(Ease.OutQuad));

        // 落下：釜の中（podTransform）へ移動
        // Appendで繋いでいるので、移動＆切り替えが終わってから実行されます
        seq.Append(moveLeaf.transform.DOMove(podTransform.position, 1.0f).SetEase(Ease.InQuad));

        // 回転も同時に行う（Join）
        seq.Join(moveLeaf.transform.DORotate(new Vector3(0, 90, 0), 1.0f));

        // 着水
        seq.OnComplete(() =>
        {
            moveLeaf.SetActive(false);
            dropleafEffect.SetActive(true);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(audioClip_DropLeaf, 1f);

            // エフェクトを少し表示してから次の指示へ
            DOVirtual.DelayedCall(1.5f, () =>
            {
                dropleafEffect.SetActive(false);

                // 次の指示（タイミングゲーム）を表示
                if (StoryManager.Instance != null)
                {
                    StoryManager.Instance.StartMiddleDialogue(instructionMessage_3, () =>
                    {
                        StartTimingGame();
                    });
                }
            });
        });
    }

    // --- 指示3: タイミングゲーム ---
    private void StartTimingGame()
    {
        timingCanvas.SetActive(true);

        // ゲージの幅の半分を移動範囲（振幅）として計算
        // ImageコンポーネントがアタッチされているGameObjectのRectTransformを取得
        RectTransform gageRect = gageImage.GetComponent<RectTransform>();
        float calculatedRange = gageRect.rect.width / 2f; // 幅の半分が振幅になる

        // カーソルを左右に往復させるループアニメーション
        // 初期位置を左端に設定 (カーソルがゲージの左端に揃う)
        timingCursor.anchoredPosition = new Vector2(-calculatedRange, 0);

        // 【修正点】DOAnchorPosXの目標値を計算した振幅に置き換える
        cursorTween = timingCursor.DOAnchorPosX(calculatedRange, cursorSpeed)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // ストップボタンが押された時
    public void StopTimingGame()
    {
        // アニメーション停止
        if (cursorTween != null) cursorTween.Kill();
        timingStopButton.interactable = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(audioClip_Timing, 1f);

        // 演出: 煙が出てポーション完成
        StartCoroutine(CompleteSequence());
    }

    private IEnumerator CompleteSequence()
    {
        yield return new WaitForSeconds(0.5f);

        // Canvas消す
        timingCanvas.SetActive(false);

        // 煙エフェクト
        if (smokeEffect != null)
        {
            smokeEffect.SetActive(true);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(audioClip_Smoke, 1f);
        }

        yield return new WaitForSeconds(2f);

        // 完成品表示
        resultObject.SetActive(true);

        // 煙消す
        if (smokeEffect != null) smokeEffect.SetActive(false);

        yield return new WaitForSeconds(1.0f);

        // 反省会（ReflectManager）を開始
        if (reflectManager != null)
        {
            reflectManager.StartTask();
        }
        else
        {
            Debug.LogError("ReflectManager is not assigned!");
        }
    }
}