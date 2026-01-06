using UnityEngine;
using DG.Tweening;

public class WisdomTrialEventManager : MonoBehaviour
{
    [Header("評価するファセット")]
    public PersonalityFacet personalityFacet = PersonalityFacet.O5_Intellect;
    [Tooltip("追加で解く問題数")]
    public int questionNum = 2;

    [Header("0番用")]
    public Transform chestCover_0;
    public GameObject[] hideObjectArray_0;
    public GameObject[] showObjectArray_0;
    [Tooltip("Chestが開いたときのX軸の角度")]
    public float targetCoverRotationX = -110f;
    [Tooltip("Chest開くまでの時間（秒）")]
    public float coverAnimDuration = 1.5f;
    [Tooltip("プレイヤーに表示するメッセージ")]
    public string[] messages_0;

    [Header("1番用")]
    public GameObject hideObject_1;
    [Header("2番用")]
    public GameObject hideObject_2;

    [Header("残りの問題数に応じてプレイヤーに表示するメッセージ")]
    public string[] remainingOneQuestion = { "すごいですね、よく解きました", "残りの暗号はあと一つです" };
    public string[] remainingNoQuestion = { "すごいです！全ての暗号を解き終わりました", "ワープゾーンで戻りましょう" };

    //何個解いたか確認
    private int count = 0;

    public static WisdomTrialEventManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public void Event(int questionID)
    {
        switch (questionID)
        {
            case 0:
                for (int i = 0; i < hideObjectArray_0.Length; i++)
                {
                    hideObjectArray_0[i].SetActive(false);
                }


                // --- アニメーションのシーケンス（順序）を作成 ---
                Sequence seq = DOTween.Sequence();

                // カバーのアニメーション (回転)
                // 現在の角度から、指定したX角度へ回転させる
                // LocalRotateにすることで、親オブジェクトが回転していても正しく動きます
                seq.Append(chestCover_0.DOLocalRotate(new Vector3(targetCoverRotationX, 0, 0), coverAnimDuration));

                // 終了処理
                seq.OnComplete(() =>
                {
                    //プレイヤーにメッセージを表示
                    StoryManager.Instance.StartMiddleDialogue(messages_0);

                    //表示したいオブジェクトの表示
                    for (int i = 0; i < showObjectArray_0.Length; i++)
                    {
                        showObjectArray_0[i].SetActive(true);
                    }
                });
                break;
            case 1:
                hideObject_1.SetActive(false);

                //score追加
                PersonalityManager.Instance.AddFacetScore(personalityFacet, PersonalityManager.TASK_MAX_SCORE / questionNum);

                //プレイヤーにメッセージを表示
                count++;
                if (count == (questionNum - 1))
                    StoryManager.Instance.StartMiddleDialogue(remainingOneQuestion);
                else if (count == questionNum)
                    StoryManager.Instance.StartMiddleDialogue(remainingNoQuestion);

                break;
            case 2:
                hideObject_2.SetActive(false);

                //score追加
                PersonalityManager.Instance.AddFacetScore(personalityFacet, PersonalityManager.TASK_MAX_SCORE / questionNum);

                //プレイヤーにメッセージを表示
                count++;
                if (count == (questionNum - 1))
                    StoryManager.Instance.StartMiddleDialogue(remainingOneQuestion);
                else if (count == questionNum)
                    StoryManager.Instance.StartMiddleDialogue(remainingNoQuestion);
                break;
            default:
                Debug.Log($"{questionID}番実行:設定されていない問題番号です");
                break;
        }
    }
}
