using UnityEngine;
[RequireComponent(typeof(BoxCollider))]
public class TriggerEvent_Task_ActiveLevelManager : MonoBehaviour
{
    public Task_ActiveLevelManager task_ActiveLevelManager;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            // 岩を転がす
            task_ActiveLevelManager.RollRocks();

            this.gameObject.SetActive(false);
        }
    }
}
