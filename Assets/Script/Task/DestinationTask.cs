using UnityEngine;
using System.Collections.Generic;

// [System.Serializable] を付けると、Inspectorに表示できるようになる
// このクラスは「1つの選択肢に対するメッセージのセット（配列）」を保持する
[System.Serializable]
public class MessageSet
{
    public string[] messages;
}


/// <summary>
/// 複数の目的地がある場合に行き先を決める際のクラス
/// </summary>
public class DestinationTask : MonoBehaviour
{
    [Header("目的地を示しているオブジェクト")]
    public GameObject[] destinationCanvas_Effect;
    [Header("測定するファセット")]
    public PersonalityFacet personalityFacet;
    [Header("加えるスコア")]
    public int addScore = PersonalityManager.TASK_MAX_SCORE;
    [Header("何番目の選択肢を選んだ場合にスコアを加えるか")]
    public int scoreIndex = 0;
    [Header("選択肢を選んだ際に表示するメッセージ")]
    public List<MessageSet> messages = new List<MessageSet>();

    [Header("選択肢を選んだ場合に非アクティブにするオブジェクト:必要に応じて")]
    public GameObject invisibleObject;

    //行き先（選択肢）を選んだ際の関数
    public void Decide(int choiceIndex)
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundInspectable, AudioManager.Instance.Normal);

        for (int i = 0; i < destinationCanvas_Effect.Length; i++)
            destinationCanvas_Effect[i].SetActive(false);

        //スコアを加算する場合の選択肢を選んだ場合
        if (choiceIndex == scoreIndex)
        {
            PersonalityManager.Instance.AddFacetScore(personalityFacet, addScore);
        }

        // messages[choiceIndex] は MessageSet クラスのインスタンスなので、
        // その中の .messages (string[]配列) を渡します。
        StoryManager.Instance.StartMiddleDialogue(messages[choiceIndex].messages);

        //選択肢を選んだ後に非表示にしたいオブジェクトがあるなら
        if (invisibleObject != null)
            invisibleObject.SetActive(false);
    }
}