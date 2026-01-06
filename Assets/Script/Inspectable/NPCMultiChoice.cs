using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 複数の選択肢を用いる場合に使用
/// </summary>
public class NPCMultiChoice : MonoBehaviour
{
    [System.Serializable]
    public class ChoiceOptionData
    {
        [Tooltip("ボタンに表示するテキスト")]
        public string buttonText;
        [Tooltip("この選択肢を選んだ時の加算スコア")]
        public int score;
        [Tooltip("会話を続ける（次のステージへ進む）か？ Falseなら会話終了")]
        public bool isContinue;
        [Tooltip("続ける場合、次はどのIndexの会話データを使うか")]
        public int nextStageIndex;
        [Tooltip("終了する場合の捨て台詞（空欄ならデフォルト）")]
        public string endMessage;
    }

    [System.Serializable]
    public class DialogueStage
    {
        [Tooltip("この段階でのNPCのセリフ")]
        [TextArea(2, 10)]
        public string[] messages;
        [Tooltip("この段階で表示する選択肢")]
        public ChoiceOptionData[] choices;
    }

    [Tooltip("会話ステージのリスト（Index 0 が最初の会話）")]
    public List<DialogueStage> dialogueStages;
}