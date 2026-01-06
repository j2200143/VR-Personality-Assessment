using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // SceneManagerのために必要
using System.Collections.Generic;
/// <summary>
/// Scene「ChoiceTaskScene」用の設定.デバッグ想定
/// </summary>
public class ChoiceTaskSceneManager : MonoBehaviour
{

    [Tooltip("タスク一覧")]
    public List<TaskSectionSO> taskSectionSOList = new List<TaskSectionSO>();
    [Tooltip("シーン遷移用:taskSectionSOListのインデックスと対応")]
    public Button[] moveSceneButtonArray;
    [Tooltip("ボタンテキスト:moveSceneButtonArrayと対応")]
    public Text[] moveSceneButtonTextArray;
    [Tooltip("タスク終了用")]
    public Button endTaskButton;
    [Tooltip("タスク終了用ボタンのテキスト")]
    public Text endTaskButtonText;
    [Tooltip("//測定結果シーン")]
    public TaskSectionSO endTaskSectionSO;

    //実行済みのタスクシーンへの移動を消す用
    private static List<int> didTaskList = new List<int>();

    void Start()
    {
        ShowMoveSceneButton();
        SetMoveSceneButton();
        if (endTaskButton != null)
        {
            endTaskButton.onClick.AddListener(MoveEndScene);
            if (endTaskButtonText != null)
            {
                endTaskButtonText.text = "測定終了する";
            }
        }
    }

    //ボタンにシーン移動関数アタッチ
    private void SetMoveSceneButton()
    {
        if (taskSectionSOList.Count == moveSceneButtonArray.Length)
        {
            for (int i = 0; i < moveSceneButtonArray.Length; i++)
            {
                if (moveSceneButtonArray[i].gameObject.activeSelf)
                {
                    int index = i;
                    moveSceneButtonArray[i].onClick.AddListener(() => MoveScene(index));
                    moveSceneButtonTextArray[i].text = taskSectionSOList[index].taskNum.ToString();
                }
            }
        }
        else
        {
            Debug.Log("全タスクの数とシーン移動の全ボタンの数が一致していません");
        }
    }
    //シーン移動：各ボタンにアタッチ
    private void MoveScene(int index)
    {
        //シーン開始時メッセージ表示用
        StoryManager.Instance.thisSceneSO = taskSectionSOList[index];
        //実行したタスクを記録
        didTaskList.Add(index);
        string sceneName = taskSectionSOList[index].sceneName;
        SceneFader.Instance.LoadSceneWithFade(sceneName);
    }
    //実行していないタスクシーンへのボタン表示
    private void ShowMoveSceneButton()
    {
        for (int i = 0; i < didTaskList.Count; i++)
        {
            moveSceneButtonArray[didTaskList[i]].gameObject.SetActive(false);
        }
    }

    //測定結果表示シーンに移動
    private void MoveEndScene()
    {
        //シーン開始時メッセージ表示用
        StoryManager.Instance.thisSceneSO = endTaskSectionSO;
        string sceneName = endTaskSectionSO.sceneName;
        SceneFader.Instance.LoadSceneWithFade(sceneName);
        Debug.Log(sceneName);
    }
}
