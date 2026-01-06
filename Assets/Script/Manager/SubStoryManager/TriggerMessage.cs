using UnityEngine;
/// <summary>
/// プレイヤーを検知し、プレイヤーにメッセージを表示/Scene移動するスクリプト
/// </summary>
[RequireComponent(typeof(Collider))]
public class TriggerMessage : MonoBehaviour
{
    [Header("表示するメッセージ")]
    public string[] messages;
    [Header("メッセージを表示した後にSceneを移動するならチェック")]
    public bool isMoveScene = false;
    // 二重反応防止フラグ
    private bool hasTriggered = false;

    /// <summary>
    /// プレイヤーがトリガーに接触した瞬間に呼ばれる
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // すでに判定済みなら何もしない
        if (hasTriggered) return;

        // プレイヤーかどうか判定 ("Player"タグがついている前提)
        if (other.CompareTag("Player"))
        {
            if (isMoveScene)
            {
                StoryManager.Instance.StartMiddleDialogue(messages, () =>
                {
                    StoryManager.Instance.MoveNextScene();
                });
            }
            else
            {
                StoryManager.Instance.StartMiddleDialogue(messages);
            }
            hasTriggered = true;
        }
    }
}
