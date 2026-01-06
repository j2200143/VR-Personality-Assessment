using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTaskSection", menuName = "SO/TaskSectionSO")]
public class TaskSectionSO : ScriptableObject
{
    [Tooltip("このタスクの番号")]
    public int taskNum;
    [Tooltip("Sceneの名前")]
    public string sceneName;
    [Header("このタスクで測定するファセット")]
    public PersonalityFacet[] personalityFacetArray;
    [Header("StoryVersion用のメッセージ")]
    [Tooltip("タスク開始時に示すメッセージ")]
    public string[] beforeTaskMessages;
    [Tooltip("タスク終了後に示すメッセージ(タスクで必要ない場合は設定しなくてよい)")]
    public string[] afterTaskMessages;
    [Header("NoStoryVersion用のメッセージ")]
    [Tooltip("タスク開始時に示すメッセージ")]
    public string[] beforeTaskManualMessages;
    [Tooltip("タスク終了後に示すメッセージ(タスクで必要ない場合は設定しなくてよい)")]
    public string[] afterTaskManualMessages;
}
