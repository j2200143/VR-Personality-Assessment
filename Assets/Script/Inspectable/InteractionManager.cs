using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR;
using System.Collections;
using UnityEngine.EventSystems;
/// <summary>
/// Inspectableの物体を制御するためのクラス。
/// </summary>
/// 
public class InteractionManager : MonoBehaviour
{
    [Header("調べる距離")]
    public float interactionDistance = 6f;
    [Header("コントローラから参照")]
    public LineRenderer rightRayLine;//playerオブジェクトの子オブジェクトのcontrollerから設定
    public LineRenderer leftRayLine;
    public Transform rightHandAnchor;
    public Transform leftHandAnchor;

    [Header("PCモード設定")]
    [Tooltip("画面中央に表示する照準画像")]
    public Image crosshairImage;
    [Tooltip("Camera.mainを取得またはアサイン")]
    [SerializeField] private Camera mainCamera;
    private GameObject currentUiTarget; // 現在狙っているUI
    private PointerEventData pointerEventData;
    private List<RaycastResult> raycastResults;

    private GameObject currentPointedAtObject = null;
    private IInteractable currentInteractableComponent = null;
    private GameObject currentInspectableObjectCanvas = null;

    private IInteractable previousInteractableComponent = null;

    [Header("現在アクティブなインタラクションの数")]
    public int activeInteractionCount = 0;

    [System.NonSerialized]//NPC.csに参照渡すためpublic  
    public InputDevice rightController;
    public InputDevice leftController;

    //実行中のコルーチンを保持するための変数を追加
    private Coroutine releaseCoroutine = null;

    private bool isInteractPressedPrev = false; // インタラクトボタンの前回の状態
    private bool isCancelPressedPrev = false;   // キャンセルボタンの前回の状態

    public static InteractionManager Instance { get; private set; }

    void Awake()
    {
        //インスタンス化
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        InitializeControllers();
    }
    void Start()
    {
        // PCモードならレイのオブジェクト自体を非表示にする
        //Buttonにインタラクトできるように設定
        if (StoryManager.Instance.isPCMode)
        {
            if (rightRayLine != null) rightRayLine.gameObject.SetActive(false);
            if (leftRayLine != null) leftRayLine.gameObject.SetActive(false);

            if (EventSystem.current != null)
            {
                pointerEventData = new PointerEventData(EventSystem.current);
            }
            else
            {
                Debug.LogError("EventSystemがシーンに存在しません。UI操作にはEventSystemが必要です。");
            }
            raycastResults = new List<RaycastResult>();

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }
        else
        {
            // PCモードでないなら通常通り初期化
            InitRayLine(rightRayLine);
            InitRayLine(leftRayLine);
        }

        // PCモードならクロスヘアを表示、VRなら非表示
        if (crosshairImage != null)
        {
            if (StoryManager.Instance.isPCMode)
            {
                crosshairImage.gameObject.SetActive(true);
                // 必要ならカーソルをロックして消す
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                crosshairImage.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (StoryManager.Instance.isExcuting || StoryManager.Instance.isMiddleExcuting)
        {
            return;
        }

        // PCモード時のカーソルロック制御
        if (StoryManager.Instance.isPCMode)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else
        {
            if (rightController == null || leftController == null)
            {
                InitializeControllers();
            }
        }

        IInteractable activeInteractable = null;

        //PCモード
        if (StoryManager.Instance.isPCMode)
        {
            // 1. まずUIの判定を行う (ボタンの上にいるか？)
            bool isHoveringUI = HandlePCRaycastUI();

            // 2. UIの上にいなければ、3Dオブジェクトの判定を行う
            if (!isHoveringUI)
            {
                activeInteractable = HandlePCRaycasting();

                // UIから外れたのでUIターゲットはクリアしておく
                if (currentUiTarget != null)
                {
                    ExecuteEvents.Execute(currentUiTarget, pointerEventData, ExecuteEvents.pointerExitHandler);
                    currentUiTarget = null;
                }
            }
        }
        else //VRモード
        {
            IInteractable rightHit = HandleControllerRaycasting(rightHandAnchor, rightController, rightRayLine);
            IInteractable leftHit = HandleControllerRaycasting(leftHandAnchor, leftController, leftRayLine);

            if (rightHit != null)
            {
                activeInteractable = rightHit;
                if (leftRayLine != null && leftHit == null) leftRayLine.enabled = false;
            }
            else if (leftHit != null)
            {
                activeInteractable = leftHit;
            }
        }

        // --- 共通: 3Dインタラクト対象の更新 ---
        if (activeInteractable != null)
        {
            if (releaseCoroutine != null)
            {
                StopCoroutine(releaseCoroutine);
                releaseCoroutine = null;
            }

            currentInteractableComponent = activeInteractable;
            currentPointedAtObject = (activeInteractable as MonoBehaviour).gameObject;

            if (!currentInteractableComponent.CheckExcute() && activeInteractionCount == 0)
            {
                currentInteractableComponent.ShowCanvas();
                currentInspectableObjectCanvas = currentInteractableComponent.GetInspectableCanvas();

                if (previousInteractableComponent != null && previousInteractableComponent != currentInteractableComponent)
                {
                    previousInteractableComponent.SetEnd();
                }
                previousInteractableComponent = currentInteractableComponent;
            }
        }
        else
        {
            if (currentInteractableComponent != null)
            {
                if (releaseCoroutine == null)
                {
                    releaseCoroutine = StartCoroutine(ReleaseAutoObject());
                }
            }
        }

        HandleInput();
        CancelAction();
    }
    void InitRayLine(LineRenderer line)
    {
        if (line != null)
        {
            line.enabled = false;
            line.startWidth = 0.005f;
            line.endWidth = 0.005f;
            line.positionCount = 2;
        }
    }
    //UI用のレイキャスト処理 (PC) ---
    //戻り値: UIに当たっていれば true
    bool HandlePCRaycastUI()
    {
        if (EventSystem.current == null) return false;

        // 画面中央をポインタ位置とする
        pointerEventData.position = new Vector2(Screen.width / 2, Screen.height / 2);
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        if (raycastResults.Count > 0)
        {
            GameObject hitObject = raycastResults[0].gameObject;
            // クリック可能なハンドラを持つ親を探す（Buttonコンポーネント等）
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);

            if (currentUiTarget != handler)
            {
                // 前のターゲットから離れる
                if (currentUiTarget != null)
                    ExecuteEvents.Execute(currentUiTarget, pointerEventData, ExecuteEvents.pointerExitHandler);

                currentUiTarget = handler;

                // 新しいターゲットに乗る
                if (currentUiTarget != null)
                    ExecuteEvents.Execute(currentUiTarget, pointerEventData, ExecuteEvents.pointerEnterHandler);
            }
            return true; // UIにヒットしている
        }
        else
        {
            // 何も当たっていない場合
            if (currentUiTarget != null)
            {
                ExecuteEvents.Execute(currentUiTarget, pointerEventData, ExecuteEvents.pointerExitHandler);
                currentUiTarget = null;
            }
            return false;
        }
    }

    void InitializeControllers()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0) rightController = devices[0];
        devices.Clear();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);
        if (devices.Count > 0) leftController = devices[0];
    }

    IInteractable HandleControllerRaycasting(Transform handAnchor, InputDevice controller, LineRenderer rayLine)
    {
        if (handAnchor == null) return null;

        Vector3 rayOrigin = handAnchor.position;
        Vector3 rayDirection = handAnchor.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, interactionDistance))
        {
            if (rayLine != null)
            {
                rayLine.enabled = true; // VRモード時は表示
                rayLine.SetPosition(0, rayOrigin);
                rayLine.SetPosition(1, hit.point);
            }

            // 当たったものからコンポーネント取得
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                return interactable; // インタラクト可能なものを返す
            }
            else
            {
                return null; // 壁などに当たった
            }
        }
        else
        {
            if (rayLine != null) rayLine.enabled = false;
            return null; // 何にも当たらなかった
        }
    }

    void HandleInput()
    {
        // 1. 現在のインタラクト入力状態を取得
        bool isCurrentInteractPressed = false;

        // PC入力 (Nキー または 左クリック)
        if (StoryManager.Instance.isPCMode)
        {
            if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.N))
            {
                isCurrentInteractPressed = true;
            }
        }

        // VR入力 (Aボタン / Primary Button)
        bool rightPressed = false, leftPressed = false;
        if (!StoryManager.Instance.isPCMode && rightController != null && leftController != null)
        {
            // PrimaryButton (A/X) に変更
            if ((rightController.TryGetFeatureValue(CommonUsages.primaryButton, out rightPressed) && rightPressed) ||
                (leftController.TryGetFeatureValue(CommonUsages.primaryButton, out leftPressed) && leftPressed))
            {
                isCurrentInteractPressed = true;
            }
        }

        // デバッグ用キー (N) は常に有効にする場合
        if (Input.GetKey(KeyCode.N)) isCurrentInteractPressed = true;


        // 2. 「押された瞬間」だけ実行 (Down検知)
        if (isCurrentInteractPressed && !isInteractPressedPrev)
        {
            if (currentInteractableComponent != null && activeInteractionCount == 0)
            {
                currentInteractableComponent.Interact();
            }
        }

        // 3. 状態更新
        isInteractPressedPrev = isCurrentInteractPressed;
    }    //PC用レイキャスト処理
    IInteractable HandlePCRaycasting()
    {
        if (Camera.main == null) return null;

        // 画面中央（Viewportの0.5, 0.5）からレイを飛ばす
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            // ヒットしたオブジェクトがIInteractableを持っているか
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            return interactable;
        }
        return null;
    }

    void CancelAction()
    {
        // 1. 現在のキャンセル入力状態を取得
        bool isCurrentCancelPressed = false;

        // PC入力 (Esc, 右クリック, Backspace)
        if (StoryManager.Instance.isPCMode)
        {
            if (Input.GetKey(KeyCode.Escape) || Input.GetMouseButton(1) || Input.GetKey(KeyCode.Backspace))
            {
                isCurrentCancelPressed = true;
            }
        }

        // VR入力 (B/Yボタン / Secondary Button)
        bool rightSecondary = false, leftSecondary = false;
        if (!StoryManager.Instance.isPCMode && rightController != null && leftController != null)
        {
            if ((rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondary) && rightSecondary) ||
                (leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out leftSecondary) && leftSecondary))
            {
                isCurrentCancelPressed = true;
            }
        }

        // デバッグ用キー (B)
        if (Input.GetKey(KeyCode.B)) isCurrentCancelPressed = true;


        // 2. 「押された瞬間」だけ実行 (Down検知)
        if (isCurrentCancelPressed && !isCancelPressedPrev)
        {
            if (currentInteractableComponent != null && !currentInteractableComponent.CheckExcute())
            {
                HideInteractionUI();
                AudioManager.Instance.PlaySound(AudioManager.Instance.soundCancel, AudioManager.Instance.Normal);
            }
        }

        // 3. 状態更新
        isCancelPressedPrev = isCurrentCancelPressed;
    }
    IEnumerator ReleaseAutoObject()
    {
        yield return new WaitForSeconds(2f);
        HideInteractionUI();
        releaseCoroutine = null;
    }

    void HideInteractionUI()
    {
        if (currentInteractableComponent != null)
        {
            currentInteractableComponent.SetEnd();
        }
        currentPointedAtObject = null;
        currentInteractableComponent = null;
    }
}

// インタラクト可能オブジェクトのためのインターフェース
public interface IInteractable
{
    void ShowCanvas();//idle状態のときに表示する
    void Interact();//トリガーを押すことによって呼び出される
    bool CheckExcute();//アイドル状態か確認
    GameObject GetInspectableCanvas();//参照中のInspectableObjectを取得
    void SetEnd();//Idle状態にしてInspectableObjectCanvasをSetActive(false)にする
}
