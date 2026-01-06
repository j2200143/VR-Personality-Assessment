using UnityEngine;

/// <summary>
/// 障害物にアタッチし、プレイヤーとの衝突を検知するクラス。
/// </summary>
public class ObstacleCollisionDetector : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("連続して衝突判定されるのを防ぐ時間（秒）")]
    public float hitCooldown = 2.0f;

    private float lastHitTime = -1f;

    // 衝突検知（ColliderがTriggerでない場合）
    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    // 衝突検知（ColliderがTriggerの場合）
    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    private void HandleCollision(GameObject target)
    {
        // プレイヤーかどうか判定
        if (target.CompareTag("Player"))
        {
            // クールダウンチェック
            if (Time.time - lastHitTime < hitCooldown) return;

            lastHitTime = Time.time;

            // マネージャーに衝突を報告
            if (PanicTaskManager.Instance != null)
            {
                PanicTaskManager.Instance.AddErrorCount();
                Debug.Log($"障害物に衝突しました: {gameObject.name}");
            }
        }
    }
}