using UnityEngine;
using UnityEngine.UI;

public class CatchButterFly : MonoBehaviour
{
    [Header("UI設定")]
    public GameObject suggestCanvas;
    public Text suggestText;
    private string suggestMessage = "トリガーボタンでキャッチ";

    // 内部変数
    private ButterFlyManager manager;
    private bool isCaught = false;
    private bool isEntered = false;

    void Update()
    {
        if (isEntered)
        {
            if (Camera.main != null)//Textをプレイヤーの方に向ける
            {
                suggestCanvas.transform.LookAt(Camera.main.transform.position);//Canvasごと傾くので壁などにめりこむ
                suggestCanvas.transform.forward = -suggestCanvas.transform.forward;
            }
        }
    }


    // マネージャーから呼ばれる初期設定
    public void Setup(ButterFlyManager mgr)
    {
        this.manager = mgr;

        //PCモード
        if (StoryManager.Instance.isPCMode)
        {
            suggestMessage = "左クリックでキャッチ";
        }
        if (suggestText != null) suggestText.text = suggestMessage;
        if (suggestCanvas != null) suggestCanvas.SetActive(false);
    }

    // プレイヤーが範囲に入った -> マネージャーに「私、捕まえられます」と自己申告
    private void OnTriggerEnter(Collider other)
    {
        if (isCaught) return;

        if (other.CompareTag("Player"))
        {
            if (suggestCanvas != null) suggestCanvas.SetActive(true);

            if (manager != null)
            {
                manager.RegisterReachable(this);
            }

            isEntered = true;
        }
    }

    // プレイヤーが範囲から出た -> 登録解除
    private void OnTriggerExit(Collider other)
    {
        if (isCaught) return;

        if (other.CompareTag("Player"))
        {
            if (suggestCanvas != null) suggestCanvas.SetActive(false);

            if (manager != null)
            {
                manager.UnregisterReachable(this);
            }

            isEntered = false;
        }
    }

    // マネージャーから呼ばれる捕獲処理
    public void Catch()
    {
        if (isCaught) return;
        isCaught = true;

        // UIを消す
        if (suggestCanvas != null) suggestCanvas.SetActive(false);

        // エフェクトや音
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(AudioManager.Instance.soundGet, AudioManager.Instance.Half);
        }

        // マネージャーへ報告（カウントアップ用）
        manager.OnButterflyCaught();

        // 自身を非表示
        gameObject.SetActive(false);
    }
}