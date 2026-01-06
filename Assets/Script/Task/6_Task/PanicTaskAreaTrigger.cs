using UnityEngine;

/// <summary>
/// プレイヤーが特定のエリア（開始・終了地点）に入ったことを検知するクラス。
/// </summary>
public class PanicTaskAreaTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        StartPhase1, // 第1エリア開始
        EndPhase1,   // 第1エリア終了（兼 中間地点）
        StartPhase2, // 第2エリア開始
        EndPhase2    // 第2エリア終了（タスク完了）
    }

    public TriggerType triggerType;
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (PanicTaskManager.Instance != null)
            {
                PanicTaskManager.Instance.OnAreaTriggerEnter(triggerType);
            }

            // 1回反応したら無効化（必要に応じて）
            gameObject.SetActive(false);
        }
    }
}