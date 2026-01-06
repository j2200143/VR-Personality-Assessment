using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class Task_RewardSharing : MonoBehaviour
{
    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.A2_Morality;

    [Header("Chest")]
    public Transform chestCover;
    public Button openChestCoverButton;
    [Tooltip("Chestが開いたときのX軸の角度")]
    public float targetCoverRotationX = -110f;
    [Tooltip("Chest開くまでの時間（秒）")]
    public float coverAnimDuration = 1.5f;
    [Tooltip("Chestを開けた後に表示するメッセージ")]
    public string[] afterOpenChestCoverMessage = { "すごい！ 10枚ほど金貨が入っています！ ", "宝箱の中の金貨を手に取り、", "NPCに渡す金貨の枚数を選びましょう" };
    [Tooltip("Chestを開けたときの効果音")]
    public AudioClip audioClip_OpenChestCover;
    [Tooltip("Chestを開けたときに表示するオブジェクト")]
    public GameObject checkButtonCanvas;

    [Header("コイン")]
    [Tooltip("NPCに渡すコイン枚数確定ボタン")]
    public Button checkButton;
    public Text checkButtonText;
    [Tooltip("Chestの中のコイン")]
    public GameObject[] goldCoinObjects;
    public Button[] goldCoinButton;
    [System.Serializable]
    public class MessageGroup
    {
        [TextArea(2, 3)]
        public string[] messages;
    }
    [Header("渡すコインの枚数を確定した後にプレイヤーに表示するメッセージ")]
    [Tooltip("Element 0:最低評価(0枚) ～ Element 4:最高評価(5枚以上) の順で設定")]
    public MessageGroup[] checkMessage = new MessageGroup[]
    {
        new MessageGroup
        {
            messages = new string[] { "随分と思い切った独り占めですね", "NPCはあなたを信じて待っていますが……" }
        },

        new MessageGroup
        {
            messages = new string[] { "約束の半分には、ほど遠い数ですね", "自分の利益を優先しましたか" }
        },

        new MessageGroup
        {
            messages = new string[] { "半分よりも、少し多めに自分のものにしましたね", "ばれない程度の得を選びましたか" }
        },

        new MessageGroup
        {
            messages = new string[] { "あと1枚で約束通りでしたね", "ほんの少しの出来心が、手元に残りましたか" }
        },

        new MessageGroup
        {
            messages = new string[] { "誰も見ていない場所でも、あなたは約束を守りましたね", "胸を張れる選択です" }
        },
    };
    [Tooltip("渡すコインの枚数を確定した後に非表示にするオブジェクト")]
    public GameObject hideWarningObject;

    [Header("リアクション表示するため")]
    public NPC npc;
    [Tooltip("Element 0:最低評価(0枚) ～ Element 4:最高評価(5枚以上) の順で設定")]
    public MessageGroup[] npcReactionMessage = new MessageGroup[]
    {
        new MessageGroup
        {
            messages = new string[] { "えっ、これだけ……？ ","いや、文句は言わねえよ","お前が嘘をつくはずないし、中身が少なかったんだな" }
        },

        new MessageGroup
        {
            messages = new string[] { "２枚かぁ。思ったより少なかったな", "運が悪かったな" }
        },

        new MessageGroup
        {
            messages = new string[] {"おっ、3枚か。悪くない稼ぎだ","お前と組んでよかったよ、ありがとう" }
        },

        new MessageGroup
        {
            messages = new string[] {"4枚！ 結構入ってたんだな！","正直に分けてくれて嬉しいよ" }
        },

        new MessageGroup
        {
            messages = new string[] {"5枚！ すごい、大漁じゃないか！","今夜はごちそうだな" }
        },
    };
    [Tooltip("NPCに設定するscript")]
    public CountShowNextStory thisCountShowNextStory;

    //NPCに渡すコインの枚数把握
    private int countGiveCoin = 0;

    void Start()
    {
        if (openChestCoverButton != null)
            openChestCoverButton.onClick.AddListener(OpenChest);

        for (int i = 0; i < goldCoinButton.Length; i++)
        {
            int index = i;
            goldCoinButton[i].onClick.AddListener(() => ClickGoldCoin(index));
        }


        if (checkButton != null)
            checkButton.onClick.AddListener(Check);
        if (checkButtonText != null)
            checkButtonText.text = $"{countGiveCoin}枚渡す";

        if (checkButtonCanvas != null)
            checkButtonCanvas.SetActive(false);
    }

    //Chestを開ける
    public void OpenChest()
    {
        //連打防止
        openChestCoverButton.gameObject.SetActive(false);

        //効果音再生
        AudioManager.Instance.PlaySound(audioClip_OpenChestCover, 0.5f);

        // --- アニメーションのシーケンス（順序）を作成 ---
        Sequence seq = DOTween.Sequence();

        // カバーのアニメーション (回転)
        // 現在の角度から、指定したX角度へ回転させる
        seq.Append(chestCover.DOLocalRotate(new Vector3(targetCoverRotationX, 0, 0), coverAnimDuration));

        // 終了処理
        seq.OnComplete(() =>
        {
            //プレイヤーにメッセージを表示
            StoryManager.Instance.StartMiddleDialogue(afterOpenChestCoverMessage);
        });

        //表示
        checkButtonCanvas.SetActive(true);
    }

    //金貨をクリック
    public void ClickGoldCoin(int index)
    {
        //重複禁止金貨非表示
        goldCoinObjects[index].gameObject.SetActive(false);

        AudioManager.Instance.PlaySound(AudioManager.Instance.soundClick, 1f);

        //枚数更新
        countGiveCoin++;
        checkButtonText.text = $"{countGiveCoin}枚渡す";
    }

    //渡す枚数確定・スコア送信
    public void Check()
    {
        //重複禁止
        checkButton.gameObject.SetActive(false);
        //NPCの元へ行けるようにする
        hideWarningObject.SetActive(false);
        //スコア送信
        int firstStepScore = PersonalityManager.TASK_MAX_SCORE / 4;
        int score = 0;
        int checkMessageIndex = 0;
        if (countGiveCoin <= 1)
        {
            score = 0;
        }
        else if (countGiveCoin == 2)
        {
            score = firstStepScore;
            checkMessageIndex = 1;
        }
        else if (countGiveCoin == 3)
        {
            score = firstStepScore * 2;
            checkMessageIndex = 2;
        }
        else if (countGiveCoin == 4)
        {
            score = firstStepScore * 3;
            checkMessageIndex = 3;
        }
        else if (countGiveCoin >= 5)
        {
            score = firstStepScore * 4;
            checkMessageIndex = 4;
        }
        PersonalityManager.Instance.AddFacetScore(personalityFacet, score);

        //プレイヤーにメッセージ表示
        StoryManager.Instance.StartMiddleDialogue(checkMessage[checkMessageIndex].messages, () =>
        {
            StoryManager.Instance.StartMiddleDialogue(new string[] { $"{npc.npcName}に報告しに行きましょう" });
        });
        //CountStoryの設置
        npc.countShowNextStory = thisCountShowNextStory;
        //NPCの体の向きを調整
        npc.transform.rotation = Quaternion.Euler(0, 90, 0);
        //NPCのメッセージを入れ替える
        npc.dialogueLines = npcReactionMessage[checkMessageIndex].messages;
    }
}
