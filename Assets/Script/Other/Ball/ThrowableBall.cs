using UnityEngine;
using System.Collections;

/// <summary>
/// 投げられるボールにアタッチするスクリプト。
/// スクリプトによる軌道制御と、物理演算による衝突の両方に対応。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThrowableBall : MonoBehaviour
{
    // BallToss.cs から参照をセットされる
    [HideInInspector]
    public Target targetScript;

    [Tooltip("地面などに当たった場合に消滅するまでの時間")]
    public float lifeTimeAfterGroundHit = 3.0f;

    private bool hitTarget = false;
    private Rigidbody rb;
    private Coroutine lifeTimeCoroutine;

    void Awake()
    {
        // Rigidbodyをキャッシュ
        rb = GetComponent<Rigidbody>();
    }

    #region Scripted Throwing (BallToss.csから呼び出す)

    /// <summary>
    /// 指定されたターゲット位置まで、指定された時間でアーチ状に飛ぶ。
    /// </summary>
    /// <param name="targetPosition">着弾点のVector3</param>
    /// <param name="duration">到達までの時間（秒）</param>
    /// <param name="arcHeight">アーチの高さ</param>
    public void ThrowToTarget_Arc(Vector3 targetPosition, float duration, float arcHeight, int score, string qualityText, AudioClip clip)
    {
        // 状態をリセット (プールから再利用されるため)
        hitTarget = false;
        // スクリプト飛行コルーチンを開始
        StartCoroutine(MoveToTargetCoroutine(targetPosition, duration, arcHeight, score, qualityText, clip));
    }

    private IEnumerator MoveToTargetCoroutine(Vector3 targetPosition, float duration, float arcHeight, int score, string qualityText, AudioClip clip)
    {
        // 物理演算を一時的に無効化し、スクリプトで位置を制御する
        rb.isKinematic = true;

        Vector3 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration); // 0.0 から 1.0 への進行度

            // 1. 水平方向の移動 (Lerp)
            Vector3 currentPos = Vector3.Lerp(startPos, targetPosition, t);

            // 2. 垂直方向のアーチ (放物線)
            // Sin(t * PI) は t=0 で 0、t=0.5 で 1、t=1.0 で 0 になる
            float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;
            currentPos.y += arc;

            // 3. ボールの位置を更新
            transform.position = currentPos;

            yield return null;
        }

        // ループ終了後：確実にターゲット位置に到達させる
        transform.position = targetPosition;

        //  Target.cs の DisplayResult を呼び出す
        targetScript.DisplayResult(score, qualityText, clip);

        // 着弾処理を自分で呼び出す
        OnHitTarget();
    }

    #endregion

    #region Hit Handling

    // ターゲットに到着した時 (コルーチンから呼ばれる)
    public void OnHitTarget()
    {
        if (hitTarget) return; // 二重処理を防止
        hitTarget = true;

        // 物理演算を元に戻す (着弾後、的に当たって跳ね返るなどの演出用)
        rb.isKinematic = false;

        // すぐに非表示にし、プールに戻す
        gameObject.SetActive(false);
    }

    //  的「以外」に当たった場合の物理衝突
    void OnCollisionEnter(Collision collision)
    {
        // スクリプトで移動中 (isKinematic=true) は、物理衝突を無視
        if (rb.isKinematic) return;

        // 既に的に当たっているか、プレイヤー自身なら無視
        if (hitTarget || collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // "Target" タグ以外に当たった場合 (地面など)
        if (!collision.gameObject.CompareTag("Target"))
        {
            // 既に消滅コルーチンが動いていなければ開始
            if (lifeTimeCoroutine == null)
                lifeTimeCoroutine = StartCoroutine(HideAfterDelay());
        }
    }

    // 地面に当たってから一定時間後にプールに戻す
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(lifeTimeAfterGroundHit);

        // まだ消えていなければ消す
        if (this.gameObject.activeSelf)
        {
            this.gameObject.SetActive(false); // プールに戻す
        }
        lifeTimeCoroutine = null; // コルーチン状態をリセット
    }

    #endregion

    #region Pooling Reset

    // プールに戻る(SetActive(false))時に自動で呼ばれるリセット処理
    void OnDisable()
    {
        // 実行中のコルーチンをすべて停止 (重要)
        StopAllCoroutines();
        lifeTimeCoroutine = null;

        // 状態フラグをリセット
        hitTarget = false;

        // 物理演算の状態をリセット
        if (rb != null)
        {
            // 速度のリセットは、BallToss.csがプールから再利用する際に行うため、
            // ここではisKinematicをtrueに設定するだけで良い。
            rb.isKinematic = true;

        }
    }

    //プールから再利用(SetActive(true))される時の初期化処理 (念のため)
    void OnEnable()
    {
        hitTarget = false;
        // isKinematic は OnDisable で true にしているので、投擲開始まで物理演算は無効
    }

    #endregion
}

