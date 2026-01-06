using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class CharacterMove : MonoBehaviour
{
    [Header("参照")]
    public Camera mainCamera; // VRカメラへの参照を追加
    private Rigidbody rb;

    [Header("設定")]
    public float moveSpeed = 6.0f;
    public float rotationSpeed = 100.0f;
    [Tooltip("動きや回転を登録するための最小入力値")]
    public float inputThreshold = 0.1f;

    private InputDevice rightHandDevice;
    private InputDevice leftHandDevice;

    // 外部から「今動いているか？」を知るためのプロパティ
    public bool IsMoving { get; private set; }

    //PCモード用
    private float xRotation = 0f; // カメラの上下回転用

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody componentが見つかりません", this);
            enabled = false;
            return;
        }
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        //mainCameraが設定されていなければ、自動で検索する
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("メインカメラが見つかりません.", this);
                enabled = false;
                return;
            }
        }

        InitializeDevices();
    }

    void InitializeDevices()
    {
        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0)
        {
            rightHandDevice = rightHandDevices[0];
        }

        var leftHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        if (leftHandDevices.Count > 0)
        {
            leftHandDevice = leftHandDevices[0];
        }
    }

    void Update()
    {
        // デバイスの有効性を毎フレーム確認
        if (!rightHandDevice.isValid || !leftHandDevice.isValid)
        {
            InitializeDevices();
        }
    }


    void FixedUpdate()
    {
        if (StoryManager.Instance.isPCMode)
        {
            HandlePCMouseLook(); // 追加
        }
        else
        {
            HandleRotation(); // 既存のVR回転
        }

        HandleMovement();
    }

    void HandleRotation()
    {
        Vector2 rotateInput = Vector2.zero;
        if (rightHandDevice.isValid && rightHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 xrRotateInput))
        {
            rotateInput = xrRotateInput;
        }

        if (Mathf.Abs(rotateInput.x) > inputThreshold)
        {
            float rotationAmount = rotateInput.x * rotationSpeed * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }

    void HandleMovement()
    {
        Vector2 combinedMovementInput = Vector2.zero;

        // 左手コントローラーのスティック入力を取得
        if (leftHandDevice.isValid && leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 xrMoveInput))
        {
            if (xrMoveInput.sqrMagnitude > inputThreshold * inputThreshold)
            {
                combinedMovementInput += xrMoveInput;
            }
        }

        // キーボードの入力を取得
        float keyboardHorizontal = Input.GetAxisRaw("Horizontal");
        float keyboardVertical = Input.GetAxisRaw("Vertical");
        Vector2 keyboardMoveInput = new Vector2(keyboardHorizontal, keyboardVertical);

        if (keyboardMoveInput.sqrMagnitude > inputThreshold * inputThreshold)
        {
            combinedMovementInput += keyboardMoveInput;
        }

        // 入力の大きさが1を超えないように正規化
        if (combinedMovementInput.sqrMagnitude > 1.0f)
        {
            combinedMovementInput.Normalize();
        }

        // 入力が閾値を超えていれば移動処理を実行
        if (combinedMovementInput.sqrMagnitude > inputThreshold * inputThreshold)
        {
            // 移動方向の基準はカメラの向き(mainCamera.transform)
            Vector3 forward = mainCamera.transform.forward;
            Vector3 right = mainCamera.transform.right;

            // Y軸（高さ）の移動はさせないため、ベクトルを水平にする
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            // 入力とカメラの向きから最終的な移動方向を計算
            Vector3 desiredMoveDirection = (forward * combinedMovementInput.y + right * combinedMovementInput.x);

            // Rigidbodyの位置を更新してキャラクターを移動させる
            rb.MovePosition(rb.position + desiredMoveDirection * moveSpeed * Time.fixedDeltaTime);

            //移動中,効果音再生
            AudioManager.Instance.PlayLoopingWalkSound(AudioManager.Instance.soundWalk);

            // 移動中フラグをON
            IsMoving = true;
        }
        else
        {
            //移動中でないなら,効果音終了
            AudioManager.Instance.StopLoopingWalkSound();

            // 移動中フラグをOFF
            IsMoving = false;
        }
    }

    //マウスによる視点操作
    void HandlePCMouseLook()
    {
        // マウスの入力を取得
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.fixedDeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.fixedDeltaTime;

        // 横回転（プレイヤー本体を回転）
        Quaternion deltaRotation = Quaternion.Euler(0f, mouseX, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);

        // 縦回転（カメラのみ回転）
        // 注: mainCameraがプレイヤーの子オブジェクトである前提
        if (mainCamera != null)
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 真上・真下制限

            // カメラのローカル回転を更新
            mainCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}
