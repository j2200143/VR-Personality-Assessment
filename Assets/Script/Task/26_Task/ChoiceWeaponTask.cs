using UnityEngine;

/// <summary>
/// C6（慎重さ）を測定する「武器選び」タスクを管理するクラス。
/// プレイヤーが説明書を読んだか、どの武器を選んだかを記録し、性格スコアを送信する。
/// </summary>
public class ChoiceWeaponTask : MonoBehaviour
{
    [Header("ローリスク武器")]
    public GameObject lowRiskWeapon;
    [Header("ハイリスク武器")]
    public GameObject highRiskWeapon;
    [Header("性能マニュアルパネル")]
    public GameObject[] manualPanelArray;
    [Header("タスク終了後に非表示するオブジェクト")]
    public GameObject[] offObjectArray;
    [Header("タスク終了後に表示するオブジェクト")]
    public GameObject nextStoryObject;

    public enum WeaponChoice
    {
        LowRiskWeapon = 0,
        HighRiskWeappon = 1
    }

    // 説明書を読んだかどうかを記録するフラグ
    private bool hasReadLowRiskManual = false;
    private bool hasReadHighRiskManual = false;

    // タスクが完了したかどうか（スコアの二重加算を防ぐ）
    private bool isTaskCompleted = false;

    /// <summary>
    /// 説明書のパネルを開いたときに呼び出されるメソッド。（UnityのButtonイベントなどから設定）
    /// </summary>
    /// <param name="weaponType">どちらの武器の説明書か</param>
    public void OnReadManual(int weaponType)
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundClick, AudioManager.Instance.Normal);
        //説明が書かれたパネルを表示
        manualPanelArray[weaponType].SetActive(true);

        // (int)にキャストしたenumを引数として受け取る
        WeaponChoice choice = (WeaponChoice)weaponType;

        // switch文はenumを直接比較できるため、よりシンプルで安全
        switch (choice)
        {
            case WeaponChoice.LowRiskWeapon:
                hasReadLowRiskManual = true;
                Debug.Log("ローリスクウェポンの説明書を読みました。");
                break;
            case WeaponChoice.HighRiskWeappon:
                hasReadHighRiskManual = true;
                Debug.Log("ハイリスクウェポンの説明書を読みました。");
                break;
        }
    }


    /// <summary>
    /// 最終的に武器を選んだときに呼び出されるメソッド。（UnityのButtonイベントなどから設定）
    /// </summary>
    /// <param name="weaponType">どちらの武器を選んだか</param>
    public void OnChooseWeapon(int weaponType)
    {
        if (!isTaskCompleted)
        {
            WeaponChoice finalChoice = (WeaponChoice)weaponType;
            int score = 0;

            //説明パネル・選択ボタンなどを非表示にする
            for (int i = 0; i < offObjectArray.Length; i++)
            {
                offObjectArray[i].SetActive(false);
            }

            //選択した武器を非表示にする
            AudioManager.Instance.PlaySound(AudioManager.Instance.soundWeapon, AudioManager.Instance.Normal);
            if (finalChoice == WeaponChoice.HighRiskWeappon)
            {
                highRiskWeapon.SetActive(false);
            }
            else if (finalChoice == WeaponChoice.LowRiskWeapon)
            {
                lowRiskWeapon.SetActive(false);
            }

            // 行動に基づいてC6のスコアを決定する
            //説明を読まずに武器を選択した場合は最低評価
            if (hasReadLowRiskManual == false || hasReadHighRiskManual == false)
            {
                score = 0;
            }
            else
            {
                //説明書を読んでハイリスクの武器を選択した場合は中間評価
                if (finalChoice == WeaponChoice.HighRiskWeappon)
                {
                    score = (int)((float)PersonalityManager.TASK_MAX_SCORE / 2);
                }
                else//説明書を読んでローリスクの武器を選択した場合は最高評価
                {
                    score = PersonalityManager.TASK_MAX_SCORE;
                }
            }

            // PersonalityManagerにスコアを送信
            PersonalityManager.Instance.AddFacetScore(PersonalityFacet.C6_Cautiousness, score);
            isTaskCompleted = true;

            //タスク終了時にメッセージを表示してシーン遷移する
            StoryManager.Instance.StartDialogue(false, StoryManager.Instance.isStoryVersion);

            //タスクの終了とともに次のタスクへ移動しない場合
            //次のタスクに進むためのオブジェクトを表示する
            //nextStoryObject.SetActive(true);
        }
    }
}
