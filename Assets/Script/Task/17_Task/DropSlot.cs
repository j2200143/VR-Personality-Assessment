using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// DraggableItemをドロップできる「スロット」にするためのスクリプト。
/// 回答スロット (answerArray) と、並び替え前のエリア (beforeSortGridObject) の両方にアタッチします。
/// </summary>
public class DropSlot : MonoBehaviour, IDropHandler
{
    /// <summary>
    /// オブジェクトがこのスロットにドロップされた時の処理
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        // ドロップされたオブジェクト（DraggableItem）を取得
        GameObject droppedObject = eventData.pointerDrag;
        DraggableItem item = droppedObject.GetComponent<DraggableItem>();

        // このスロットがすでに別のアイテムを持っているか？（スワップ処理）
        if (transform.childCount > 0)
        {
            // すでにスロットに入っているアイテムを取得
            Transform itemInSlot = transform.GetChild(0);
            DraggableItem scriptInSlot = itemInSlot.GetComponent<DraggableItem>();

            // 古いアイテムを、ドラッグしてきたアイテムの元の場所に戻す
            scriptInSlot.parentAfterDrag = item.parentAfterDrag;
            itemInSlot.SetParent(item.parentAfterDrag);
        }

        // ドラッグしてきたアイテムの新しい親（=ドロップ先）を、このスロットに設定
        item.parentAfterDrag = this.transform;

        //ドロップ音
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundDrag, 1f);
    }
}