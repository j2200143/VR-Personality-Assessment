
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// オブジェクトをドラッグ＆ドロップできるようにするスクリプト。
/// World Space / Overlay 両対応 + PCモード(FPS操作)対応版
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    // ドロップされた後、このアイテムの親となるTransform
    [HideInInspector]
    public Transform parentAfterDrag;

    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private RectTransform rootCanvasRect;
    private RectTransform myRectTransform;

    // ドラッグ時のオフセット
    private Vector3 dragOffset;

    // PCモード用のドラッグフラグ
    private bool isPCDragging = false;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        myRectTransform = GetComponent<RectTransform>();

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            rootCanvasRect = rootCanvas.GetComponent<RectTransform>();
        }

        parentAfterDrag = transform.parent;
    }

    void Update()
    {
        // PCモードかつドラッグ中なら、毎フレーム位置を更新（視点追従）
        if (isPCDragging && StoryManager.Instance.isPCMode)
        {
            UpdatePCDragPosition();
        }
    }

    // --- PCモード用 (Pointer Down/Up) ---

    public void OnPointerDown(PointerEventData eventData)
    {
        // PCモードなら、クリックした瞬間にドラッグ開始とみなす
        if (StoryManager.Instance.isPCMode)
        {
            isPCDragging = true;

            // OnBeginDrag相当の処理
            parentAfterDrag = transform.parent;
            if (rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform);
            }
            canvasGroup.blocksRaycasts = false;

            // オフセット計算
            CalculateDragOffset(new Vector2(Screen.width / 2, Screen.height / 2), Camera.main);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPCDragging)
        {
            isPCDragging = false;

            // PCモードではOnDropが自動で呼ばれないため、手動でドロップ先を探す
            GameObject dropTarget = FindDropTargetPC();

            if (dropTarget != null)
            {
                // ドロップ先が見つかったら、そちらのOnDropを呼ぶ
                DropSlot slot = dropTarget.GetComponent<DropSlot>();
                if (slot != null)
                {
                    // 擬似的なPointerEventDataを作成して渡す
                    PointerEventData ped = new PointerEventData(EventSystem.current);
                    ped.pointerDrag = this.gameObject;
                    slot.OnDrop(ped);
                }
            }

            // OnEndDrag相当の処理（親戻しなど）
            // ※OnDropで親が変わっていれば、parentAfterDragは更新されている
            transform.SetParent(parentAfterDrag);
            canvasGroup.blocksRaycasts = true;
            transform.localPosition = Vector3.zero;
        }
    }

    // PCモード用の位置更新処理
    private void UpdatePCDragPosition()
    {
        if (rootCanvasRect == null || Camera.main == null) return;

        Vector3 worldPoint;
        // 画面中央(Screen.width/2, Screen.height/2) を使って位置計算
        Vector2 centerScreen = new Vector2(Screen.width / 2, Screen.height / 2);

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvasRect,
            centerScreen,
            Camera.main,
            out worldPoint))
        {
            transform.position = worldPoint + dragOffset;
        }
    }

    // PCモード用のドロップ先検索（Raycast）
    private GameObject FindDropTargetPC()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = new Vector2(Screen.width / 2, Screen.height / 2);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            // DropSlotコンポーネントを持つオブジェクトを探す
            // (自分自身はblocksRaycasts=falseなので無視されるはず)
            DropSlot slot = result.gameObject.GetComponent<DropSlot>();
            if (slot != null)
            {
                return result.gameObject;
            }
        }
        return null;
    }

    // --- 共通処理 ---

    private void CalculateDragOffset(Vector2 screenPos, Camera cam)
    {
        Vector3 worldPoint;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvasRect,
            screenPos,
            cam,
            out worldPoint
        ))
        {
            dragOffset = transform.position - worldPoint;
        }
        else
        {
            dragOffset = Vector3.zero;
        }
    }

    // --- VR / マウスカーソルモード用 (IDragHandler) ---
    // PCモード(Locked Cursor)ではこれらは呼ばれないが、VR用に残しておく

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (StoryManager.Instance.isPCMode) return; // PCモードはOnPointerDownで処理済み

        parentAfterDrag = transform.parent;
        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform);
        }
        canvasGroup.blocksRaycasts = false;

        Camera cam = eventData.pressEventCamera;
        if (cam == null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = Camera.main;

        CalculateDragOffset(eventData.position, cam);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (StoryManager.Instance.isPCMode) return; // PCモードはUpdateで処理済み
        if (rootCanvasRect == null) return;

        Camera cam = eventData.pressEventCamera;
        if (cam == null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = Camera.main;

        Vector3 worldPoint;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvasRect,
            eventData.position,
            cam,
            out worldPoint))
        {
            transform.position = worldPoint + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (StoryManager.Instance.isPCMode) return; // PCモードはOnPointerUpで処理済み

        transform.SetParent(parentAfterDrag);
        canvasGroup.blocksRaycasts = true;
        transform.localPosition = Vector3.zero;
    }
}