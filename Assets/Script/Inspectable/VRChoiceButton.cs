using UnityEngine;

/// <summary>
/// NPCの選択肢ボタンをVRコントローラーで操作可能にするためのクラス。Buttonが使えないと思っていたためこのクラスは存在する。
/// IInteractableを実装し、InteractionManagerから直接操作できる。Interactしか使わない
/// </summary>
public class VRChoiceButton : MonoBehaviour, IInteractable
{
    [Header("このボタンが属する親のNPCスクリプト")]
    public NPC npc;
    [Header("このボタンが何番目の選択肢かを示すインデックス（0~3）")]
    public int choiceIndex;

    #region IInteractableの実装
    public void ShowCanvas()
    {
    }
    public void Interact()
    {
        // NPCスクリプトのOnChoiceSelectedメソッドを、設定されたインデックスで呼び出す
        if (npc != null)
        {
            npc.OnChoiceSelected(choiceIndex);
        }
    }
    public bool CheckExcute()
    {
        return true;
    }
    public GameObject GetInspectableCanvas()
    {
        return null;
    }
    public void SetEnd()
    {
    }
    #endregion


}