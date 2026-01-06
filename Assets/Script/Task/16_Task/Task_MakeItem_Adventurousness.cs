using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening; // DOTweenを使用
using System.Collections;

public class Task_MakeItem_Adventurousness : MonoBehaviour
{
    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.O4_Adventurousness;

    [Header("テーブル上のアイテム")]
    [Tooltip("テーブルの上に置いてあるアイテムたち indexはアイテムIDと対応している")]
    public Button[] choiceItemButtonArray;

    [Tooltip("移動地点/middleを通ってendへ向かう")]
    public Transform[] middleTransform;
    public Transform[] endTransform; // 壺の中

    [Header("レシピ・アイテムデータ")]
    public List<Recipe> recipeList;
    public List<Item_MakeItem_Adventurousness> itemList;

    [Header("プレイヤーに提示するレシピUI")]
    public GameObject recipeObject; // レシピが記されている親オブジェクト
    public GameObject[] nowProcedureObject; // 現在の行程を示すパネル
    public Image[] firstProcedureItemImageArray;
    public Image[] secondProcedureItemImageArray;
    public Image lastProcedureItemImage; // 基本のアイテム
    public Image lastProcedureChangeItemImage; // 未知のアイテム
    public Transform[] showRecipePositionArray; // レシピ表を表示する位置

    [System.Serializable]
    public class MessageGroup
    {
        [TextArea(2, 3)]
        public string[] messages;
    }
    [Header("レシピ切り替え時に表示するメッセージ")]
    public MessageGroup[] switchMessage = new MessageGroup[]
{
    new MessageGroup
    {
        messages = new string[] { "一つ目のポーションは無事に完成しましたね", "右側に用意してあるポーションも完成させてください" }
    }
};

    [Header("フェーズ3（最終選択）用UI")]
    public GameObject choicePanel; // 親パネル
    [Tooltip("行動B：未知のレシピで調合を行う")]
    public Button newActiveButton; // 行動B：未知のレシピで調合を行う
    [Tooltip("行動A：帰る")]
    public Button goHomeButton;
    [Header("フェーズ3（最終選択）後に表示するメッセージ")]
    public string[] yesMessage = { "あ、すみません", "アイテムの準備を済ませていませんでした", "申し訳ないですが調合はせずに帰りましょう" };
    public string[] noMessage = { "お疲れさまでした" };

    [Header("エフェクト")]
    [Tooltip("物体が落ちた時のエフェクト（泡など）")]
    public GameObject[] dropObjectEffect;
    [Tooltip("完成時の煙")]
    public GameObject[] smokeEffect;
    [Tooltip("完成時の物体")]
    public GameObject[] resultObjectArray;

    [Header("効果音")]
    public AudioClip audioClip_DropObject;
    public AudioClip audioClip_Smoke;

    // 現在取り組んでいるレシピ
    private int nowRecipeIndex = 0;
    // 現在取り組んでいる工程
    private int nowProcedureIndex = 0;
    // 未知のアイテムを選んだ回数（フェーズ1,2用）
    private int choiceUnknownItemCount = 0;
    // アニメーション中などの操作ブロック用
    private bool isProcessing = false;
    //現在の工程のアイテム何個投入したか
    private int nowDropCountOfStep = 0;

    void Start()
    {
        // アイテム選択ボタンのイベント登録
        for (int i = 0; i < choiceItemButtonArray.Length; i++)
        {
            int index = i; // クロージャ対策
            choiceItemButtonArray[i].onClick.AddListener(() => ChoiceItem(index));
        }

        // フェーズ3のボタンイベント登録
        if (newActiveButton != null)
            newActiveButton.onClick.AddListener(() => OnPhase3Choice(true));

        if (goHomeButton != null)
            goHomeButton.onClick.AddListener(() => OnPhase3Choice(false));

        // フェーズ3パネルは最初は隠す
        if (choicePanel != null)
            choicePanel.SetActive(false);

        //エフェクト非表示にする
        for (int i = 0; i < dropObjectEffect.Length; i++)
        {
            dropObjectEffect[i].SetActive(false);
        }
        for (int i = 0; i < smokeEffect.Length; i++)
        {
            smokeEffect[i].SetActive(false);
        }

        // 最初のレシピを表示
        ShowRecipe(nowRecipeIndex);
    }

    // レシピ表を更新する
    private void ShowRecipe(int recipeIndex)
    {
        // 工程リセット
        nowProcedureIndex = 0;

        // 進行中パネルをリセット
        for (int i = 0; i < nowProcedureObject.Length; i++)
        {
            nowProcedureObject[i].SetActive(true);
        }
        nowProcedureObject[nowProcedureIndex].SetActive(false);

        // レシピ表の位置更新
        if (showRecipePositionArray.Length > recipeIndex)
        {
            recipeObject.transform.SetPositionAndRotation(showRecipePositionArray[recipeIndex].position, showRecipePositionArray[recipeIndex].rotation);
        }

        Recipe recipe = recipeList[recipeIndex];

        // アイコン更新
        UpdateProcedureImages(firstProcedureItemImageArray, recipe.steps, 0);
        UpdateProcedureImages(secondProcedureItemImageArray, recipe.steps, 1);

        // 最後の選択肢のアイコン更新
        if (itemList.Count > recipe.lastProcedureItemID)
            lastProcedureItemImage.sprite = itemList[recipe.lastProcedureItemID].itemIcon;

        if (itemList.Count > recipe.lastProcedureUnknownItemID)
            lastProcedureChangeItemImage.sprite = itemList[recipe.lastProcedureUnknownItemID].itemIcon;


    }

    // 手順の画像を更新するヘルパー関数
    private void UpdateProcedureImages(Image[] images, List<RecipeStep> steps, int stepIndex)
    {
        if (stepIndex >= steps.Count) return;

        for (int i = 0; i < images.Length; i++)
        {
            if (i < steps[stepIndex].validItemIDs.Count)
            {
                int itemId = steps[stepIndex].validItemIDs[i];
                images[i].gameObject.SetActive(true);
                // 変更箇所：itemListを使用
                images[i].sprite = itemList[itemId].itemIcon;
            }
            else
            {
                images[i].gameObject.SetActive(false);
            }
        }
    }

    // 調合壺に入れるアイテムを選ぶ
    public void ChoiceItem(int itemID)
    {
        if (isProcessing) return; // アニメーション中は操作無効
        if (nowRecipeIndex >= recipeList.Count) return; // 全レシピ終了後は操作無効

        Recipe nowRecipe = recipeList[nowRecipeIndex];

        // 最後の手順かどうか判定
        bool isLastStep = nowProcedureIndex >= nowRecipe.steps.Count;

        if (!isLastStep)
        {
            // 通常手順：正しいアイテムか判定
            if (nowRecipe.steps[nowProcedureIndex].validItemIDs.Contains(itemID))
            {
                // 正解
                StartCoroutine(ProcessItemDrop(itemID, false));
                //二度目の入力を防ぐために非表示にする
                choiceItemButtonArray[itemID].gameObject.SetActive(false);
            }
            else
            {
                // 不正解
                AudioManager.Instance.PlaySound(AudioManager.Instance.soundMiss, 1f);
                Debug.Log("Wrong Item!");
            }
        }
        else
        {
            // 最後の手順：基本か未知かの選択
            bool isUnknown = (itemID == nowRecipe.lastProcedureUnknownItemID);
            bool isStandard = (itemID == nowRecipe.lastProcedureItemID);

            if (isUnknown || isStandard)
            {
                // どちらかを選んだ場合
                StartCoroutine(ProcessItemDrop(itemID, true, isUnknown));
                //二度目の入力を防ぐために非表示にする
                choiceItemButtonArray[itemID].gameObject.SetActive(false);
            }
            else
            {
                // 関係ないアイテムを選んだ場合
                AudioManager.Instance.PlaySound(AudioManager.Instance.soundMiss, 1f);
                Debug.Log("Wrong Item for Last Step!");
            }
        }

    }

    // アイテム投下アニメーションと進行処理
    private IEnumerator ProcessItemDrop(int itemID, bool isRecipeFinish, bool isUnknownChoice = false)
    {
        isProcessing = true;
        GameObject targetItem = itemList[itemID].itemObject;

        // アイテム移動アニメーション (DOTween)
        Sequence seq = DOTween.Sequence();
        seq.Append(targetItem.transform.DOMove(middleTransform[nowRecipeIndex].position, 0.5f));
        seq.Append(targetItem.transform.DOMove(endTransform[nowRecipeIndex].position, 0.5f));

        yield return seq.WaitForCompletion();

        // 投入後のエフェクト
        if (dropObjectEffect[nowRecipeIndex] != null)
        {
            dropObjectEffect[nowRecipeIndex].SetActive(true);
        }
        AudioManager.Instance.PlaySound(audioClip_DropObject, 1f);

        // アイテムを非表示にする
        targetItem.SetActive(false);

        if (!isRecipeFinish)//最終工程以外
        {
            nowDropCountOfStep++;
            if (nowDropCountOfStep >= recipeList[nowRecipeIndex].steps[nowProcedureIndex].validItemIDs.Count)//次の工程に移る条件が整えば
            {
                // 手順完了表示
                nowProcedureObject[nowProcedureIndex].SetActive(true);

                nowProcedureIndex++;

                nowProcedureObject[nowProcedureIndex].SetActive(false);

                nowDropCountOfStep = 0;
            }
        }
        else
        {
            // レシピ完了時の処理

            if (isUnknownChoice)
            {
                choiceUnknownItemCount++;
            }

            // 完成エフェクト
            if (smokeEffect[nowRecipeIndex] != null)
            {
                smokeEffect[nowRecipeIndex].SetActive(true);
            }
            AudioManager.Instance.PlaySound(audioClip_Smoke, AudioManager.Instance.Normal);

            yield return new WaitForSeconds(1.0f); // 余韻

            //完成品表示
            resultObjectArray[nowRecipeIndex].SetActive(true);

            // 次のレシピへ、または終了へ
            nowRecipeIndex++;



            if (nowRecipeIndex < recipeList.Count)
            {
                // 次のレシピへ更新
                ShowRecipe(nowRecipeIndex);
                //メッセージ表示
                StoryManager.Instance.StartMiddleDialogue(switchMessage[nowRecipeIndex - 1].messages);
            }
            else
            {
                //メッセージ表示してからフェーズ3開始
                StoryManager.Instance.StartMiddleDialogue(switchMessage[nowRecipeIndex - 1].messages, () =>
                {
                    StartChoice();
                });
            }
        }

        isProcessing = false;
    }

    // フェーズ3：最終選択の開始
    private void StartChoice()
    {
        // 天の声「仕事はこれで終わりです...」のUI表示
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }
    }

    // フェーズ3の選択処理
    private void OnPhase3Choice(bool chooseUnknownRecipe)
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);

        CalculateScore(chooseUnknownRecipe);
    }

    // スコア計算と評価
    private void CalculateScore(bool didChooseUnknownRecipe)
    {
        int firstStepScore = (int)(float)PersonalityManager.TASK_MAX_SCORE / 4;
        int score = 0;
        bool isPhase3Unknown = didChooseUnknownRecipe; // 行動B

        if (!isPhase3Unknown) // フェーズ3で行動A（帰る）
        {
            if (choiceUnknownItemCount == 0)
            {
                // 0点: 2回とも基本 (Unknown=0) かつ 帰る
                score = 0;
            }
            else if (choiceUnknownItemCount == 1)
            {
                // 1点: 1回だけ行動B (Unknown=1) かつ 帰る
                score = firstStepScore;
            }
            else if (choiceUnknownItemCount == 2)
            {
                // 2点: 2回とも行動B (Unknown=2) だが 帰る
                score = firstStepScore * 2;
            }
        }
        else // フェーズ3で行動B（未知のレシピを行う）
        {
            if (choiceUnknownItemCount < 2)
            {
                // 3点: フェーズ3でBだが、フェーズ1・2のどちらかはA (Unknown < 2)
                score = firstStepScore * 3;
            }
            else if (choiceUnknownItemCount == 2)
            {
                // 4点: フェーズ1・2でBを選び続け (Unknown=2)、フェーズ3でもB
                score = firstStepScore * 4;
            }
        }

        Debug.Log($"スコア: {score} (未知のアイテムを選んだ回数: {choiceUnknownItemCount}, フェーズ3に挑んだかどうか: {isPhase3Unknown})");

        //スコアを送信する
        PersonalityManager.Instance.AddFacetScore(personalityFacet, score);

        string[] showMessage;
        if (isPhase3Unknown)
        {
            showMessage = yesMessage;
        }
        else
        {
            showMessage = noMessage;
        }
        StoryManager.Instance.StartMiddleDialogue(showMessage, () =>
        {
            StoryManager.Instance.MoveNextScene();
        });
    }
}

// Inspectorでリストのリストを表示するためのラッパークラス
[System.Serializable]
public class RecipeStep
{
    public List<int> validItemIDs; // この工程で入れてよいアイテムIDリスト
}

[System.Serializable]
public class Recipe
{
    // 入れるアイテムリスト (RecipeStepのリストに変更)
    public List<RecipeStep> steps;

    // 最後の手順のアイテムID
    public int lastProcedureItemID; // 基本アイテム
    public int lastProcedureUnknownItemID; // 未知アイテム
}

// アイテムの設定
[System.Serializable]
public class Item_MakeItem_Adventurousness
{
    [Tooltip("")]
    public int itemID;
    [Tooltip("レシピ書に映すアイコン")]
    public Sprite itemIcon;
    [Tooltip("実物")]
    public GameObject itemObject;
}