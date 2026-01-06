using UnityEngine;

/// <summary>
/// プレイヤーがゾーンに入ってきたときに指定したオブジェクトを表示し、
/// 出ていったときに非表示にするスクリプト。
/// コライダー（Is Trigger = On）を持つオブジェクトにアタッチ
/// </summary>
public class ShowObject_EnterZone : MonoBehaviour
{
    [Header("表示制御するオブジェクト")]
    [Tooltip("プレイヤーがエリア内にいる間だけ表示されるオブジェクト")]
    public GameObject showObject;

    // 現在表示中かどうかのフラグ
    private bool isShowed = false;

    void Start()
    {
        // 初期状態では非表示にしておく
        if (showObject != null)
        {
            showObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: ShowObject が設定されていません！");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 既に表示中なら何もしない
        if (isShowed) return;

        // プレイヤー (Tag: "Player") が入ってきたか確認
        if (other.CompareTag("Player"))
        {
            if (showObject != null)
            {
                showObject.SetActive(true);
                isShowed = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // プレイヤーが出ていったか確認
        if (other.CompareTag("Player"))
        {
            if (showObject != null)
            {
                showObject.SetActive(false);
                isShowed = false;
            }
        }
    }
}