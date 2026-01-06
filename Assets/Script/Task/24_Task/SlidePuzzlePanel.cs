using UnityEngine;
using UnityEngine.UI; // Button を使うために必要

/// <summary>
/// パズルの各パネル（ピース）にアタッチするスクリプト。
/// </summary>
[RequireComponent(typeof(Button))]
public class SlidePuzzlePanel : MonoBehaviour
{
    [Header("正解の番号")]
    [Tooltip("このパネルが正解時に収まるべきスロットの番号 (0〜7)")]
    public int correctID; 
    [HideInInspector]
    public SlidePuzzleManager manager;//SlidePuzzleManagerで設定

    private Button _button;

    void Awake()
    {
        // 自分のButtonコンポーネントにOnClickイベントを登録
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnPanelClick);
    }

    private void OnPanelClick()
    {
        // Managerが設定されていれば、自分がクリックされたことを伝える
        if (manager != null)
        {
            manager.OnPanelClicked(this);
        }
    }
}
