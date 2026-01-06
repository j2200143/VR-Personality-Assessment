using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.C2_Orderliness;

    [Header("インベントリ参照")]
    public GameObject[] beforeInventory, afterInventory; // horizonLayoutグループとか設定,Mass.csも付ける
    public GameObject inventoryTopObject, heldObject, adviceObject;//・全てのオブジェクトの親オブジェクト・一時的なアイテム表示場所・助言用テキストのオブジェクト
    public Text adviceText;//助言用メッセージを表示するテキスト
    public int itemNum = 6; // 合計何個アイテムがあるか。チェックスコア用

    [Header("タスク終了後に非表示にするオブジェクト")]
    public GameObject[] warnigStoryObjectArray;
    [Header("タスク終了後に表示するオブジェクト")]
    public GameObject nextSceneObject;
    [Header("タスク終了後に表示するメッセージ")]
    public string[] endMessage = { "整理できましたね。扉から外に出ましょう" };

    [Header("スタートボタン")]
    public Button startButton;

    [Header("操作説明")]
    public Text manualText;
    public string manual = "移動させたいアイテムにコントローラーを向けて、トリガーを引こう。その後移動させたい場所にコントローラーを向けてトリガーを引けば移動できるよ";

    // --- 状態管理変数の変更 ---
    private GameObject heldItemObject = null; // 現在掴んでいるアイテム
    private Mass originalSlot = null;         // アイテムを掴んだ元のスロット


    void Start()
    {
        // 初期化
        if (inventoryTopObject != null)
        {
            inventoryTopObject.SetActive(false);
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(StartTask);
        }

        if (manualText != null)
        {
            manualText.text = manual;
        }

        for (int i = 0; i < beforeInventory.Length; i++)
        {
            Button btn = beforeInventory[i].GetComponent<Button>();
            int index = i; // クロージャ用の一時変数
            btn.onClick.AddListener(() => ClickBeforeMass(index));
        }
        for (int i = 0; i < afterInventory.Length; i++)
        {
            Button btn = afterInventory[i].GetComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() => ClickAfterMass(index));
        }

        if (nextSceneObject != null)
        {
            nextSceneObject.SetActive(false);
        }
    }


    //アイテムの移動の処理
    //ボタンでタスクを始める場合
    public void StartTask()
    {
        startButton.gameObject.SetActive(false);

        inventoryTopObject.SetActive(true);

        AudioManager.Instance.PlaySound(AudioManager.Instance.soundInventory, AudioManager.Instance.Normal);
    }
    // アイテム移動処理
    private void HandleItemInteraction(Mass targetSlot, GameObject slotObject)
    {
        if (targetSlot == null) return;

        // --- アイテムを掴む処理 ---
        if (heldItemObject == null)
        {
            // マスにアイテムがないなら何もしない
            if (!targetSlot.isChild) return;

            originalSlot = targetSlot;

            // アイテムを取得（slotObjectの子要素から取得）
            heldItemObject = slotObject.transform.GetChild(0).gameObject;

            // 視覚的に掴んでいるように見せる
            heldItemObject.transform.SetParent(heldObject.transform, false);

            AudioManager.Instance.PlaySound(AudioManager.Instance.soundClick, AudioManager.Instance.Normal);
        }
        // --- アイテムを置く処理 ---
        else
        {
            // 既にアイテムがあるマスには置けない
            if (targetSlot.isChild) return;

            AudioManager.Instance.PlaySound(AudioManager.Instance.soundClick, AudioManager.Instance.Normal);

            // 新しいスロットに配置
            heldItemObject.transform.SetParent(slotObject.transform, false);
            targetSlot.isChild = true;

            // 元のスロットの状態を更新
            if (originalSlot != null)
            {
                originalSlot.isChild = false;
            }

            // 手持ち状態をリセット
            heldItemObject = null;
            originalSlot = null;
        }
    }

    // 移動前のインベントリボタンにアタッチ
    public void ClickBeforeMass(int index)
    {
        // Before用のデータとオブジェクトを渡す
        HandleItemInteraction(beforeInventory[index].GetComponent<Mass>(), beforeInventory[index]);
    }
    //移動後のアイテムインベントリにアタッチ
    public void ClickAfterMass(int index)
    {
        HandleItemInteraction(afterInventory[index].GetComponent<Mass>(), afterInventory[index]);
    }


    //スコア確認において全てのアイテムが移動されていない場合発動する
    private IEnumerator Warning(string message)
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundMiss, AudioManager.Instance.Half);
        adviceObject.SetActive(true);
        adviceText.text = message;
        yield return new WaitForSeconds(3f);
        adviceObject.SetActive(false);
        adviceText.text = "";
    }

    //移動結果を確認し、スコア測定
    public void ScoreCheck()
    {
        Debug.Log("ScoreCheck()");

        bool isOK = true;
        //全てのアイテムを移動させているかチェック
        for (int i = 0; i < beforeInventory.Length; i++)
        {
            if (beforeInventory[i].transform.childCount > 0)
            {
                isOK = false;
                break;
            }
        }

        if (isOK)
        {
            //左上に敷き詰めているかの評価
            int checkCount = 0;
            for (int i = 0; i < itemNum; i++)
            {
                if (afterInventory[i].transform.childCount > 0)
                {
                    checkCount++;
                }
            }
            if (checkCount == itemNum)
            {
                Debug.Log("左上詰めのスコアを追加");
                PersonalityManager.Instance.AddFacetScore(personalityFacet, PersonalityManager.TASK_MAX_SCORE / 2);
            }
            checkCount = 0;
            //Item種類別にまとめているかの評価、アイテムをみつけたらそこからindex増やして種類同じならitemCount++.checkCountが種類と同じ数なるならok
            //（総アイテム数/種類　＝ 2なので 見付けたときチェックするのは1個となりだけでいい。全種類同じ数準備するからこれでよし）
            ItemType checkItemType;
            for (int i = 0; i < afterInventory.Length - 2; i++)
            {
                if (afterInventory[i].transform.childCount > 0)
                {
                    checkItemType = afterInventory[i].transform.GetChild(0).gameObject.GetComponent<Item>().type;

                    if (afterInventory[i + 1].transform.childCount > 0 && checkItemType == afterInventory[i + 1].transform.GetChild(0).gameObject.GetComponent<Item>().type)//1個となりチェックして同じならば
                    {
                        checkCount++;
                        i++;//今回総数6で3種類だから連続で確認するのは2個。となり確認して同じなら次の判定飛ばしていいから。
                    }
                }
            }
            if (checkCount == System.Enum.GetNames(typeof(ItemType)).Length)
            {
                Debug.Log("種類別のスコアを追加");
                PersonalityManager.Instance.AddFacetScore(personalityFacet, PersonalityManager.TASK_MAX_SCORE / 2);
            }
            inventoryTopObject.SetActive(false);

            //タスク終了後の処理
            AudioManager.Instance.PlaySound(AudioManager.Instance.soundInventory, AudioManager.Instance.Normal);

            StoryManager.Instance.StartMiddleDialogue(endMessage);
            if (nextSceneObject != null)
            {
                nextSceneObject.SetActive(true);
            }
            for (int i = 0; i < warnigStoryObjectArray.Length; i++)
            {
                warnigStoryObjectArray[i].SetActive(false);
            }
        }
        else
        {
            StartCoroutine(Warning("全てのアイテムを移動させてください"));
        }
    }


}
