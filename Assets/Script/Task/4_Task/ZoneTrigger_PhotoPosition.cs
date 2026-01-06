using UnityEngine;

/// <summary>
/// プレイヤーがこのエリアに入った/出たことを Task_PhotoPosition_N4 に通知するクラス。
/// Is Trigger = true の Collider が必要。
/// </summary>
public class ZoneTrigger_PhotoPosition : MonoBehaviour
{
    [Tooltip("このゾーンの得点タイプ（0～3）")]
    public int zoneScoreType;

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーが入ってきた場合
        if (other.CompareTag("Player"))
        {
            if (Task_PhotoPosition_N4.Instance != null)
            {
                Task_PhotoPosition_N4.Instance.OnPlayerEnterZone(zoneScoreType);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // プレイヤーが出て行った場合
        if (other.CompareTag("Player"))
        {
            if (Task_PhotoPosition_N4.Instance != null)
            {
                Task_PhotoPosition_N4.Instance.OnPlayerExitZone(zoneScoreType);
            }
        }
    }
}