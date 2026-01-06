using UnityEngine;
using System.Collections;
/// <summary>
/// カウントによってタスクの終了判定やイベントを追加する
/// NPCやInspectableObjectにアタッチ
/// </summary>
public class CountShowNextStory : MonoBehaviour
{
    [Header("TaskSectionSOのエンドメッセージを表示してシーン移動したい場合設定")]
    [Tooltip("次のScene(タスク)に進むためのオブジェクト:CountPlas()")]
    public GameObject nextStoryObject;//プレイヤーが近づくと次のタスクに進む
    [Tooltip("表示・非表示するオブジェクト、")]
    public GameObject[] showObjectArrayStory, hideObjectArrayStory;
    [Tooltip("何回到達したらタスクを終了するかの値")]
    public int targetCount;
    [Tooltip("回数到達してからメッセージを表示するまでの時間")]
    public float timeOfShowMessage = 2f;

    [Header("オブジェクトを表示・非表示・シーン移動するなら設定")]
    [Tooltip("カウントが進んだ際に表示するオブジェクト:CountPlasForShowObject()")]
    public GameObject[] showObjectArray;
    [Tooltip("カウントが進んだ際に非表示するオブジェクト:CountPlasForShowObject()")]
    public GameObject[] hideObjectArray;
    [Tooltip("オブジェクトを表示した際にプレイヤーに表示するメッセージ")]
    public string[] toPlayerMessages;
    [Tooltip("到達したらオブジェクトを表示する値")]
    public int targetShowCount;
    [Tooltip("回数到達してからオブジェクトを表示するまでの時間")]
    public float timeOfShowObject = 1.5f;
    [Tooltip("到達した場合に次のシーンに遷移する場合")]
    public bool isMoveNextScene = false;

    private int count = 0, countObject = 0;

    void Start()
    {
        for (int i = 0; i < showObjectArray.Length; i++)
        {
            showObjectArray[i].SetActive(false);
        }
        for (int i = 0; i < hideObjectArray.Length; i++)
        {
            hideObjectArray[i].SetActive(true);
        }
    }

    //この関数使用していないかも
    // TaskSectionSOのエンドメッセージを表示してシーン移動したい場合。
    public void CountPlas()
    {
        count++;
        if (count == targetCount)
        {
            StartCoroutine(ShowNextStoryAndDialogue());
        }
    }
    private IEnumerator ShowNextStoryAndDialogue()
    {
        // 次のストーリーオブジェクトを表示
        if (nextStoryObject != null)
        {
            nextStoryObject.SetActive(true);
        }
        //他のオブジェクト
        for (int i = 0; i < showObjectArrayStory.Length; i++)
        {
            showObjectArrayStory[i].SetActive(true);
        }
        for (int i = 0; i < hideObjectArrayStory.Length; i++)
        {
            hideObjectArrayStory[i].SetActive(false);
        }

        // 1秒待機
        yield return new WaitForSeconds(timeOfShowMessage);

        // タスク終了後のメッセージ表示を開始
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.StartDialogue(false, StoryManager.Instance.isStoryVersion);//これ読み終わったら自動で遷移するように途中で変更したため無意義化もこの関数
        }
    }

    // オブジェクトを表示したい場合。NPCではこのクラスをアタッチしている場合自動で実行
    public void CountPlasForShowObject()
    {
        countObject++;
        if (countObject == targetShowCount)
        {
            StartCoroutine(ShowObject());
        }
    }
    private IEnumerator ShowObject()
    {
        // 1秒待機
        yield return new WaitForSeconds(timeOfShowObject);

        //オブジェクトを表示
        for (int i = 0; i < showObjectArray.Length; i++)
        {
            showObjectArray[i].SetActive(true);
        }
        //オブジェクトを非表示
        for (int i = 0; i < hideObjectArray.Length; i++)
        {
            hideObjectArray[i].SetActive(false);
        }

        //プレイヤーにメッセージを表示
        if (isMoveNextScene)
        {
            StoryManager.Instance.StartMiddleDialogue(toPlayerMessages, () =>
            {
                StoryManager.Instance.MoveNextScene();
            });
        }
        else
        {
            StoryManager.Instance.StartMiddleDialogue(toPlayerMessages);
        }

    }
}
