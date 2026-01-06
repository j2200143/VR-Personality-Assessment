using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class ItemShopTask : MonoBehaviour
{
    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.C1_SelfEfficacy;
    [Tooltip("必要な回復薬の数")]
    public int healItemNum = 3;
    [Tooltip("必要な解毒数の数")]
    public int poisonHealItemNum = 2;
    [Tooltip("必要なロープの数")]
    public int ropeItemNum = 1;
    [Tooltip("所持金")]
    public int gold = 100;

    [System.Serializable]
    public class MessageListWrapper
    {
        [Tooltip("各パターンのメッセージ")]
        public string[] messages;
    }
    [Tooltip("得点によって表示するメッセージ（Indexが得点に対応）")]
    public List<MessageListWrapper> endMessage;

    [Tooltip("アイテム一覧")]
    public List<Item_ItemShop> itemList;
    //アイテム情報
    [System.Serializable]
    public class Item_ItemShop
    {
        public int itemID;
        public ItemType itemType;
        public int needGold;
        public float weight;
    }
    public enum ItemType
    {
        HealItem = 0,
        PoisonHealItem = 1,
        RopeItem = 2
    }

    [Header("UI")]
    public Text nowGoldText, nowWeightNumText;
    public Text[] nowItemTypeCountText;
    public Button completeButton;
    public GameObject warningObject;//アイテムを一つも購入していないのに依頼完了ボタンを押した場合に表示

    [Header("/アイテムオブジェクト")]
    public Text[] itemNeedGoldText, itemWeightText;
    public Button[] purchaseButton;

    //プレイヤーの購入状況
    private int nowGold = 0;//現在のプレイヤーのゴールド
    private float nowWeight = 0f;
    private int[] nowCounts = new int[] { 0, 0, 0 };//プレイヤーがどのアイテムタイプのアイテムを購入したか
    private Item_ItemShop item_ItemShop;
    private bool isPurchased = false;

    void Start()
    {
        //初期化

        completeButton.onClick.AddListener(CompleteTask);
        for (int i = 0; i < purchaseButton.Length; i++)
        {
            int index = i;
            purchaseButton[i].onClick.AddListener(() => PurchaseItem(index));
        }

        for (int i = 0; i < itemList.Count; i++)
        {
            itemNeedGoldText[i].text = $"{itemList[i].needGold}G";
            itemWeightText[i].text = $"{itemList[i].weight}Kg";
        }

        nowGold = gold;
        nowGoldText.text = $"所持G:{nowGold}";
        nowWeightNumText.text = $"重量:{0}Kg";
        nowItemTypeCountText[(int)ItemType.HealItem].text = $"回復薬×{0}";
        nowItemTypeCountText[(int)ItemType.PoisonHealItem].text = $"解毒草×{0}";
        nowItemTypeCountText[(int)ItemType.RopeItem].text = $"ロープ×{0}";

        warningObject.SetActive(false);
    }

    //アイテム購入ボタンを押したときの処理
    public void PurchaseItem(int itemID)
    {
        if (itemList.Count > itemID)
        {
            isPurchased = true;
            AudioManager.Instance.PlaySound(AudioManager.Instance.soundPurchase, 1f);
            item_ItemShop = itemList[itemID];

            if (nowGold >= item_ItemShop.needGold)
            {
                //どのアイテムを購入したか
                int itemIndex = (int)item_ItemShop.itemType;
                nowCounts[itemIndex]++;
                if (itemIndex == (int)ItemType.HealItem)
                {
                    nowItemTypeCountText[(int)ItemType.HealItem].text = $"回復薬×{nowCounts[itemIndex]}";
                }
                else if (itemIndex == (int)ItemType.PoisonHealItem)
                {
                    nowItemTypeCountText[(int)ItemType.PoisonHealItem].text = $"解毒草×{nowCounts[itemIndex]}";
                }
                else
                {
                    nowItemTypeCountText[(int)ItemType.RopeItem].text = $"ロープ×{nowCounts[itemIndex]}";
                }

                //現在のゴールド更新
                nowGold -= item_ItemShop.needGold;
                nowGoldText.text = $"所持G:{nowGold}";
                //重量を更新
                nowWeight += item_ItemShop.weight;
                nowWeightNumText.text = $"重量:{nowWeight}Kg";

            }
            else
            {
                AudioManager.Instance.PlaySound(AudioManager.Instance.soundMiss, 1f);
            }
        }
    }

    //依頼完了ボタンにアタッチ
    public void CompleteTask()
    {
        if (!isPurchased)//何も購入していない場合
        {
            StartCoroutine(ShowWarnigObject());
        }
        else
        {
            int firstStepScore = (int)(float)PersonalityManager.TASK_MAX_SCORE / 4;
            int score = 0;

            // 必須アイテムが揃っているか確認
            bool hasHeal = nowCounts[(int)ItemType.HealItem] >= healItemNum;
            bool hasPoison = nowCounts[(int)ItemType.PoisonHealItem] >= poisonHealItemNum;
            bool hasRope = nowCounts[(int)ItemType.RopeItem] >= ropeItemNum;

            bool isItemsComplete = hasHeal && hasPoison && hasRope;

            if (!isItemsComplete)
            {
                // 0点（最低評価）: 必須アイテム自体が揃わなかった場合
                score = 0;
            }
            else
            {
                completeButton.gameObject.SetActive(false);
                // アイテムは揃っている場合、制約条件（重量・予算）をチェック

                // 重量制限 (10kgまで)
                if (nowWeight > 10f)
                {
                    // 1点: 必須アイテムは揃ったが、重量オーバーの場合
                    score = firstStepScore;
                }
                else
                {
                    if (nowGold >= 20)
                    {
                        // 4点（最高評価）: 残金が20ゴールド以上（最適解）
                        score = firstStepScore * 4;
                    }
                    else if (nowGold >= 10)
                    {
                        // 3点: 残金が10〜19ゴールド（準最適解）
                        score = firstStepScore * 3;
                    }
                    else
                    {
                        // 2点（中間評価）: 残金が0〜9ゴールド（ギリギリ）
                        // ※コード上では nowGold < 0 になる前に購入制限がかかっている前提
                        score = firstStepScore * 2;
                    }
                }
            }
            //得点追加
            PersonalityManager.Instance.AddFacetScore(personalityFacet, score);

            //得点に応じたメッセージを表示してからシーン移動
            // scoreをstepScoreで割ってインデックス(0~4)に変換
            int index = 0;
            if (firstStepScore > 0) index = score / firstStepScore;

            index = Mathf.Clamp(index, 0, endMessage.Count - 1);

            string[] showMessage = endMessage[index].messages;
            StoryManager.Instance.StartMiddleDialogue(showMessage, () =>
            {
                StoryManager.Instance.MoveNextScene();
            });

        }
    }
    private IEnumerator ShowWarnigObject()
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundMiss, 1f);
        warningObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        warningObject.SetActive(false);
    }
}

