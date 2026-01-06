using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro; // TextMeshProを使う場合

/// <summary>
/// ボール投げゲームのメインロジック。
/// プレイヤーの入力検知、タイミング判定（スケール式）、ボールの発射を管理する。
/// オブジェクトプーリング対応。
/// </summary>
public class BallToss : MonoBehaviour
{
    // ユーザー定義の状態に更新
    private enum GameState
    {
        Idle,       // プレイヤーが範囲外
        CanThrow,   // 範囲内。入力待ち
        Aiming,     // 1回目のAボタン押下。タイミング計測中
        Throwed     // 投擲後。リセット待ち
    }
    private GameState currentState = GameState.Idle;

    // 投擲の質
    private enum ThrowQuality { Bad, Normal, Good, Excellent }

    [Header("必須コンポーネント参照")]
    [Tooltip("投げるボールのプレハブ")]
    public GameObject ballPrefab;
    [Tooltip("ボールが発射される地点（例: プレイヤーの右手）")]
    public Transform throwSpawnPoint;
    [Tooltip("的のTarget.csスクリプト")]
    public Target targetScript;
    [Tooltip("的の中心（Excellent）のTransform")]
    public Transform targetCenter;
    [Tooltip("オブジェクトプール用のリスト")]
    private List<GameObject> ballPool;

    [Header("UI参照")]
    [Tooltip("「Aボタンで投げる」のプロンプトUI")]
    public GameObject promptCanvas;
    public Text promptText;
    private string message = "VR:Aボタン,PC:Mキー";
    [Tooltip("投球カーソル（円状）の親Canvas")]
    public GameObject cursorCanvas;
    [Tooltip("投球カーソル（スケールが変わる輪状のUI要素）")]
    public RectTransform cursorImage;
    [Tooltip("全獲得スコア")]
    public GameObject allScoreCanvas;

    [Header("投擲設定")]
    [Tooltip("投擲後のクールダウン時間（秒）")]
    public float throwCooldown = 2.0f;
    [Tooltip("あらかじめ生成しておくボールの数")]
    public int ballPoolSize = 10;
    [Tooltip("ボールが的に当たるまでの時間")]
    public float throwDuration = 2f;
    [Tooltip("ボールが的に当たるまでの最大の高さ")]
    public float throwArcHeight = 1.5f;
    // ... (既存の throwCooldown の下などに追加) ...

    [Header("投擲ターゲット位置")]
    [Tooltip("Bad判定時に狙うTransform")]
    public Transform badTransform;
    [Tooltip("Normal判定時に狙うTransform")]
    public Transform NormalTransform;
    [Tooltip("Good判定時に狙うTransform")]
    public Transform goodTransform;
    [Tooltip("Excellent判定時に狙うTransform (的の中心)")]
    public Transform excelentTransform;

    [Header("タイミング設定 (スケール)")]
    [Tooltip("カーソルのスケールが1から0に達する時間（秒）")]
    public float aimDuration = 1.0f;
    // 判定をタイミング（時間）からスケール（0.0～1.0）に変更
    [Tooltip("Excellent判定になるスケール範囲")]
    public Vector2 excellentRange = new Vector2(0.0f, 0.15f); // 0f以上~0.15f未満
    [Tooltip("Good判定になるスケール範囲")]
    public Vector2 goodRange = new Vector2(0.15f, 0.35f); // 0.15f以上~0.35f未満
    [Tooltip("Normal判定になるスケール範囲")]
    public Vector2 normalRange = new Vector2(0.6f, 0.9f); // 0.6f以上~0.9f未満
    //上記以外はbad

    // --- プライベート変数 ---
    private Coroutine aimingCoroutine;
    private float currentAimScale = 0f; // ★ 現在のカーソルスケールを保持
    private bool playerIsInArea = false; // ★ プレイヤーが範囲内にいるか

    // VR入力用
    private InputDevice rightController;
    private InputDevice leftController;
    private bool wasRightPrimaryPressed = false;
    private bool wasLeftPrimaryPressed = false;

    void Start()
    {
        promptCanvas.SetActive(false);
        if (cursorCanvas != null) cursorCanvas.SetActive(false);
        allScoreCanvas.SetActive(false);

        InitializeControllers();
        InitializeBallPool(); // オブジェクトプールを初期化
    }

    void InitializeBallPool()
    {
        ballPool = new List<GameObject>();
        if (ballPrefab == null) { Debug.LogError("Ball Prefabが設定されていません！"); return; }

        for (int i = 0; i < ballPoolSize; i++)
        {
            GameObject ball = Instantiate(ballPrefab, throwSpawnPoint.position, throwSpawnPoint.rotation);
            ball.SetActive(false);
            ballPool.Add(ball);
            ThrowableBall ballScript = ball.GetComponent<ThrowableBall>();
            if (ballScript != null) ballScript.targetScript = targetScript;
        }
    }

    void InitializeControllers()
    {
        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0) rightController = rightHandDevices[0];

        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        if (leftHandDevices.Count > 0) leftController = leftHandDevices[0];
    }

    void Update()
    {
        if (!rightController.isValid || !leftController.isValid)
        {
            InitializeControllers();
        }

        bool inputPressed = IsPrimaryButtonDown(); // 「押された瞬間」を検知

        switch (currentState)
        {
            case GameState.Idle:
                // プレイヤーが近づくのを待つ (OnTriggerEnterで CanThrow に遷移)
                break;

            case GameState.CanThrow:
                // プレイヤーが範囲内で、入力待ち
                if (inputPressed)
                {
                    StartAiming();
                }
                break;

            case GameState.Aiming:
                // タイミング計測中にAボタンが押された
                if (inputPressed)
                {
                    StopAimingAndThrow();
                }
                break;

            case GameState.Throwed:
                // 投擲後。リセット待ち (コルーチンで処理)
                break;
        }
    }

    void LateUpdate()
    {
        // 次のフレームのために、現在のボタン状態を保存
        bool rightPressed = false;
        if (rightController.isValid) rightController.TryGetFeatureValue(CommonUsages.primaryButton, out rightPressed);
        wasRightPrimaryPressed = rightPressed;

        bool leftPressed = false;
        if (leftController.isValid) leftController.TryGetFeatureValue(CommonUsages.primaryButton, out leftPressed);
        wasLeftPrimaryPressed = leftPressed;
    }

    // Aボタン（PrimaryButton または Mキー）が「押された瞬間」を検知
    bool IsPrimaryButtonDown()
    {
        if (Input.GetKeyDown(KeyCode.M)) return true;

        bool rightPressed = false;
        bool isRightButtonDown = false;
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.primaryButton, out rightPressed))
        {
            if (rightPressed && !wasRightPrimaryPressed) isRightButtonDown = true;
        }

        bool leftPressed = false;
        bool isLeftButtonDown = false;
        if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.primaryButton, out leftPressed))
        {
            if (leftPressed && !wasLeftPrimaryPressed) isLeftButtonDown = true;
        }
        return isRightButtonDown || isLeftButtonDown;
    }

    // --- Trigger (Player Area) Handling ---

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInArea = true; // プレイヤーが範囲内にいる
            if (currentState == GameState.Idle)
            {
                currentState = GameState.CanThrow;

                promptText.text = message;
                promptCanvas.SetActive(true);

                allScoreCanvas.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInArea = false; // プレイヤーが範囲外に出た

            if (currentState == GameState.CanThrow || currentState == GameState.Aiming)// Throwed (クールダウン中) の場合は、コルーチンが Idle に戻す
            {
                currentState = GameState.Idle;
                promptCanvas.SetActive(false);

                // もしエイム中だったらカーソルも止める
                if (aimingCoroutine != null)
                {
                    StopCoroutine(aimingCoroutine);
                    aimingCoroutine = null;
                }
                if (cursorCanvas != null) cursorCanvas.SetActive(false);

                allScoreCanvas.SetActive(false);
            }
        }
    }

    // --- Game Logic ---

    // 1回目のAボタン：エイム開始
    void StartAiming()
    {
        currentState = GameState.Aiming;
        promptCanvas.SetActive(false);
        if (cursorCanvas != null) cursorCanvas.SetActive(true);

        // カーソルを動かすコルーチンを開始
        aimingCoroutine = StartCoroutine(AimingCursorCoroutine());
    }

    // 投球カーソル（スケール）をアニメーションさせるコルーチン
    private IEnumerator AimingCursorCoroutine()
    {
        float elapsedTime = 0f;
        float animSpeed = 1.0f / aimDuration; // 1秒間に進むスケール値

        while (true) // ずっとループ
        {
            elapsedTime += Time.deltaTime;
            // Mathf.Repeat を使うと 0 -> 1 をきれいにループできる
            float scaleUp = Mathf.Repeat(elapsedTime * animSpeed, 1.0f);

            // 1.0f から引くことで、ループを 1 -> 0 に反転させる
            currentAimScale = 1.0f - scaleUp;

            if (cursorImage != null)
            {
                cursorImage.localScale = new Vector3(currentAimScale, currentAimScale, 1f);
            }

            yield return null;
        }
    }

    // 2回目のAボタン：投擲
    void StopAimingAndThrow()
    {
        if (aimingCoroutine != null)
        {
            StopCoroutine(aimingCoroutine);
            aimingCoroutine = null;
        }

        currentState = GameState.Throwed; // 状態を Throwed に変更

        promptCanvas.SetActive(false);

        if (cursorCanvas != null) cursorCanvas.SetActive(false);

        // --- タイミング判定 ---
        // 停止した瞬間のスケール値(currentAimScale)で判定
        ThrowQuality quality = DetermineQuality(currentAimScale);
        Vector3 aimTargetPosition = GetAimTarget(quality);

        // --- スコアを Target に即座に送信 ---
        // (ボールが飛んでいる間にスコアを表示する)
        int score = 10;
        string qualityText = "Bad";
        AudioClip clip = targetScript.hitBadSound; // 音はTargetから取得

        switch (quality)
        {
            case ThrowQuality.Excellent:
                score = 200;
                qualityText = "Excellent!";
                clip = targetScript.hitExcellentSound;
                break;
            case ThrowQuality.Good:
                score = 100;
                qualityText = "Good";
                clip = targetScript.hitGoodSound;
                break;
            case ThrowQuality.Normal:
                score = 50;
                qualityText = "Normal";
                clip = targetScript.hitNormalSound;
                break;
        }


        // --- ボールをプールから取得して発射 ---
        GameObject ball = GetBallFromPool();
        if (ball != null && throwSpawnPoint != null)
        {
            ball.transform.position = throwSpawnPoint.position;
            ball.transform.rotation = throwSpawnPoint.rotation;
            ball.SetActive(true);

            ThrowableBall ballScript = ball.GetComponent<ThrowableBall>();
            if (ballScript != null)
            {
                ballScript.ThrowToTarget_Arc(aimTargetPosition, throwDuration, throwArcHeight, score, qualityText, clip);
            }
        }


        // リセット（またはクールダウン）コルーチンを開始
        StartCoroutine(ThrowResetCoroutine());
    }

    // --- タイミングと精度の計算 ---

    // スケール値から投擲の質を決定
    ThrowQuality DetermineQuality(float scaleValue)
    {
        // 0f以上~0.15f未満 (excellentRange.x <= scaleValue < excellentRange.y)
        if (scaleValue >= excellentRange.x && scaleValue < excellentRange.y)
        {
            return ThrowQuality.Excellent;
        }

        // 0.15f以上~0.35f未満 (goodRange.x <= scaleValue < goodRange.y)
        if (scaleValue >= goodRange.x && scaleValue < goodRange.y)
        {
            return ThrowQuality.Good;
        }

        // 0.6以上~0.9未満 (normalRange.x <= scaleValue < normalRange.y)
        if (scaleValue >= normalRange.x && scaleValue < normalRange.y)
        {
            return ThrowQuality.Normal;
        }

        // 上記以外は Bad
        return ThrowQuality.Bad;
    }

    Vector3 GetAimTarget(ThrowQuality quality)
    {
        // デフォルトまたはExcellentの場合
        if (quality == ThrowQuality.Excellent && excelentTransform != null)
        {
            return excelentTransform.position;
        }
        if (quality == ThrowQuality.Good && goodTransform != null)
        {
            return goodTransform.position;
        }
        if (quality == ThrowQuality.Normal && NormalTransform != null)
        {
            return NormalTransform.position;
        }
        if (quality == ThrowQuality.Bad && badTransform != null)
        {
            return badTransform.position;
        }

        // もしどれかのTransformが設定されていなかった場合の予備
        Debug.LogWarning($"対応する {quality} のTransformが設定されていません。Excellentの位置を使います。");
        return excelentTransform != null ? excelentTransform.position : Vector3.zero;
    }


    GameObject GetBallFromPool()
    {
        foreach (GameObject ball in ballPool)
        {
            if (!ball.activeInHierarchy)
            {
                return ball;
            }
        }
        // プールが枯渇した場合の保険
        GameObject newBall = Instantiate(ballPrefab, throwSpawnPoint.position, throwSpawnPoint.rotation);
        ballPool.Add(newBall);
        ThrowableBall ballScript = newBall.GetComponent<ThrowableBall>();
        if (ballScript != null) ballScript.targetScript = targetScript;
        return newBall;
    }

    // --- コルーチン ---



    // 投擲後のリセット処理（クールダウン）
    private IEnumerator ThrowResetCoroutine()
    {
        // ユーザーの仕様（ステップ4）では「ボールが非表示になったら」だが、
        // 確実性のために固定のクールダウン時間を設ける
        yield return new WaitForSeconds(throwCooldown);

        if (currentState == GameState.Throwed)
        {
            // プレイヤーがまだエリア内にいるか確認
            if (playerIsInArea)
            {
                currentState = GameState.CanThrow;
                promptCanvas.SetActive(true);
            }
            else
            {
                currentState = GameState.Idle; // 範囲外に出ていた場合
            }
        }
    }
}