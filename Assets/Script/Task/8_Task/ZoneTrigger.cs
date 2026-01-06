using UnityEngine;

/// <summary>
/// プレイヤーがこのエリアに入った/出たことを GregariousnessTracker に通知する。
/// `Collider` (Is Trigger = true) がアタッチされている必要がある。
/// </summary>
public class ZoneTrigger : MonoBehaviour
{
    [Header("このゾーンのタイプ")]
    [Tooltip("このエリアが「集団」か「個人」かを設定する")]
    public PlayerLocation zoneType; //  Inspectorで Group または Solo を選択

    private GregariousnessTracker tracker;

    void Start()
    {
        // シングルトンのTrackerインスタンスを取得
        tracker = GregariousnessTracker.Instance;
        if (tracker == null)
        {
            Debug.LogError("シーンに GregariousnessTracker が見つかりません！");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤー (Rigidbody と "Player" タグが必要) が入ってきた
        if (other.CompareTag("Player") && tracker != null)
        {
            tracker.OnPlayerEnterZone(zoneType);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // プレイヤーが出て行った
        if (other.CompareTag("Player") && tracker != null)
        {
            tracker.OnPlayerLeaveZone(zoneType);
        }
    }
}
