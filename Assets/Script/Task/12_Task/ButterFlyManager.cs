using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class ButterFlyManager : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("シーン内の全ての蝶")]
    public CatchButterFly[] allButterflies;
    [Tooltip("全ての蝶を捕まえた後に非表示にするオブジェクト")]
    public GameObject obstacleObject;
    [Tooltip("タスク用")]
    public TaskManager_Cheerfulness taskManager_Cheerfulness;

    // --- 内部変数 ---
    // 「今、プレイヤーの近くにいて捕まえられる蝶」のリスト
    private List<CatchButterFly> reachableButterflies = new List<CatchButterFly>();

    private int caughtCount = 0;
    private int totalCount = 0;

    // 入力用
    private InputDevice rightController;
    private InputDevice leftController;

    void Start()
    {
        totalCount = allButterflies.Length;

        // 各蝶にマネージャーを登録
        foreach (var bf in allButterflies)
        {
            bf.Setup(this);
        }

        // コントローラー初期化
        InitializeControllers();
    }

    void Update()
    {
        // 捕まえられる蝶がいないなら入力判定もしない（軽量化）
        if (reachableButterflies.Count == 0) return;

        // 入力監視
        CheckInput();
    }

    // --- 入力処理 ---
    private void CheckInput()
    {
        if (!rightController.isValid || !leftController.isValid) InitializeControllers();

        bool isTriggerPressed = false;

        // 右手
        if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool right) && right)
            isTriggerPressed = true;

        // 左手
        if (!isTriggerPressed && leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.triggerButton, out bool left) && left)
            isTriggerPressed = true;

        // キーボード(デバッグ)vrシミュレーション
        if (Input.GetKeyDown(KeyCode.N)) isTriggerPressed = true;

        //PCモード
        if (StoryManager.Instance.isPCMode && Input.GetMouseButtonDown(0))
        {
            isTriggerPressed = true;
        }

        if (isTriggerPressed)
        {
            CatchNearestButterfly();
        }
    }

    private void InitializeControllers()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0) rightController = devices[0];

        devices.Clear();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);
        if (devices.Count > 0) leftController = devices[0];
    }

    // --- 蝶からの登録・解除 ---

    // プレイヤーが範囲に入った（蝶から呼ばれる）
    public void RegisterReachable(CatchButterFly butterfly)
    {
        if (!reachableButterflies.Contains(butterfly))
        {
            reachableButterflies.Add(butterfly);
        }
    }

    // プレイヤーが範囲から出た（蝶から呼ばれる）
    public void UnregisterReachable(CatchButterFly butterfly)
    {
        if (reachableButterflies.Contains(butterfly))
        {
            reachableButterflies.Remove(butterfly);
        }
    }

    // --- 捕獲処理 ---

    private void CatchNearestButterfly()
    {
        // リストの先頭（または距離が一番近いもの）を捕まえる
        // ここではシンプルにリストの0番目を捕獲する。キャッチ可能範囲を抜けていればリストには存在しないので
        if (reachableButterflies.Count > 0)
        {
            // ※リスト操作中に要素を削除するとエラーになるため、対象を特定してから処理
            CatchButterFly target = reachableButterflies[0];

            // 捕獲実行
            target.Catch();

            // リストから削除（Catch内で行われる非表示化によりOnTriggerExitが呼ばれない場合の手動削除）
            // ただしCatch()内でSetActive(false)するとOnTriggerExitが呼ばれないことがあるので、
            // ここで明示的にリストから外すのが安全
            if (reachableButterflies.Contains(target))
            {
                reachableButterflies.Remove(target);
            }
        }
    }

    // 蝶が実際に捕まった時に呼ばれる（スコア加算など）
    public void OnButterflyCaught()
    {
        caughtCount++;
        Debug.Log($"捕獲！ ({caughtCount}/{totalCount})");

        if (caughtCount >= totalCount)
        {
            Debug.Log("コンプリート！");

            if (obstacleObject != null)
            {
                obstacleObject.SetActive(false);
            }

            taskManager_Cheerfulness.StartTask();
        }
    }
}