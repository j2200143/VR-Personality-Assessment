using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// モード切替
/// </summary>
public class ObjectSetByMode : MonoBehaviour
{
    [Header("天の声テキスト表示")]
    public GameObject vrCanvas;
    public Text vrMessageText;
    public GameObject pcCanvas;
    public Text pcMessageText;

    [Header("デバック")]
    public GameObject XRDeviceSimulator;

    void Start()
    {
        // StoryManagerが存在し、シングルトン経由でアクセスできる前提
        if (StoryManager.Instance != null)
        {
            // マネージャーのモード判定を見て、適切な方を渡す
            if (StoryManager.Instance.isPCMode)
            {
                StoryManager.Instance.RegisterSceneUI(pcCanvas, pcMessageText);

                // 不要な方を非表示
                if (vrCanvas != null) vrCanvas.SetActive(false);
                if (pcCanvas != null) pcCanvas.SetActive(true);

                //PCModeならデバイスシミュレーターは非表示
                if (XRDeviceSimulator != null)
                    XRDeviceSimulator.SetActive(false);
            }
            else
            {
                StoryManager.Instance.RegisterSceneUI(vrCanvas, vrMessageText);

                // 不要な方を非表示
                if (pcCanvas != null) pcCanvas.SetActive(false);
                if (vrCanvas != null) vrCanvas.SetActive(true);

                //PCモードではないかつPC上でVRをシミュレートするなら
                if (StoryManager.Instance.isEmulatingVR && XRDeviceSimulator != null)
                    XRDeviceSimulator.SetActive(true);
            }
        }
    }
}