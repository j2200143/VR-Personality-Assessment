using UnityEngine;
using UnityEngine.AI; //  NavMeshAgent (自動経路探索) を使うために必要
using System.Collections; //  コルーチン (IEnumerator) を使うために必要

/// <summary>
/// 複数のNPCの移動とアニメーションを管理するコントローラー。
/// NavMeshAgentの使用を前提としています。
/// </summary>
public class NPCController : MonoBehaviour
{
    [Header("NPCのリスト")]
    [Tooltip("管理したいNPCのGameObjectをすべて登録します")]
    public GameObject[] controlNPCs;

    [Header("移動先のリスト")]
    [Tooltip("NPCの移動先となるTransformを登録します。controlNPCsのインデックスと対応させます。")]
    public Transform[] targetPositions;

    [Header("移動設定")]
    [Tooltip("NPCの移動速度")]
    public float moveSpeed = 1f;

    [Header("移動させたときにプレイヤーの方向を向かせたいなら設定")]
    public Transform playerTransform;

    //アニメーション設定
    //Animatorの『歩行中かどうか』を制御するboolパラメータ名
    private const string animatorWalkParameterName = "isWalking";


    /// <summary>
    /// 指定されたインデックスのNPCを、対応する移動先まで移動させます。
    /// ButtonのOnClick()から呼び出すことを想定しています。
    /// </summary>
    /// <param name="index">controlNPCs配列のインデックス番号</param>
    public void MoveNPC(int index)
    {
        // --- 安全確認 (配列の範囲外やnullを指定されていないか) ---
        if (controlNPCs == null || index < 0 || index >= controlNPCs.Length || controlNPCs[index] == null)
        {
            Debug.LogError($"NPCController: controlNPCs[{index}] が正しく設定されていません。");
            return;
        }
        if (targetPositions == null || index < 0 || index >= targetPositions.Length || targetPositions[index] == null)
        {
            Debug.LogError($"NPCController: targetPositions[{index}] が正しく設定されていません。");
            return;
        }

        // --- NPCの移動処理をコルーチンで開始 ---
        // 既に動いている可能性も考慮し、古いコルーチンを停止してから新しいコルーチンを開始します。
        StartCoroutine(MoveNPCToTargetCoroutine(index));
    }

    /// <summary>
    /// NPCの移動とアニメーション制御を行うコルーチン（非同期処理）
    /// </summary>
    private IEnumerator MoveNPCToTargetCoroutine(int index)
    {
        // --- 1. 必要なコンポーネントを取得 ---
        GameObject npc = controlNPCs[index];
        Transform target = targetPositions[index];

        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
        Animator animator = npc.GetComponent<Animator>();

        // コンポーネントの存在チェック
        if (agent == null)
        {
            Debug.LogError($"NPC '{npc.name}' に NavMeshAgent コンポーネントがありません。", npc);
            yield break; // コルーチンを終了
        }
        if (animator == null)
        {
            Debug.LogError($"NPC '{npc.name}' に Animator コンポーネントがありません。", npc);
            yield break; // コルーチンを終了
        }

        // --- 2. 移動開始とアニメーションの再生 ---
        agent.speed = moveSpeed;
        agent.SetDestination(target.position);
        animator.SetBool(animatorWalkParameterName, true); // "walk" アニメーション開始

        // --- 3. 到着するまで待機 ---

        // agent.remainingDistance は経路計算が終わるまで正しくないため、
        // まず経路計算中(pathPending)かどうかをチェックします。
        while (agent.pathPending)
        {
            yield return null; // 1フレーム待つ
        }

        // 経路計算が完了したら、目的地に到着するまで待機
        // (stoppingDistance: 目的地との許容誤差)
        while (agent.remainingDistance > agent.stoppingDistance)
        {
            // もし何らかの理由でパスが無効になったら（目的地にたどり着けないなど）
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning($"NPC '{npc.name}' が目的地に到達できませんでした。", npc);
                break; // ループを抜ける
            }
            yield return null; // 1フレーム待つ
        }

        // --- 4. 到着処理とアニメーションの停止 ---
        animator.SetBool(animatorWalkParameterName, false); // "idle" アニメーションに戻す

        // 到着したらplayerの方向を向く
        if (playerTransform != null)
            npc.transform.LookAt(new Vector3(playerTransform.position.x, playerTransform.transform.position.y, playerTransform.position.z));
        else// 到着したらtargetの方向を向く
        {
            npc.transform.LookAt(new Vector3(target.position.x, target.transform.position.y, target.position.z));

        }
    }
}