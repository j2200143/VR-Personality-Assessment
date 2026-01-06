using UnityEngine;
using System.Collections; // IEnumerator を使うために必要

public class SpikeBlockMove : MonoBehaviour
{
    public float forwardTime = 3f; // 前方へ移動するのにかかる時間
    public float backTime = 3f;    // 元の位置へ戻るのにかかる時間
    public Transform target;       // 移動先の目標地点 (Inspectorで設定)

    private Vector3 startPosition; // 開始位置
    private Vector3 endPosition;   // 目標地点の位置

    void Start()
    {
        // 自身の初期位置を開始位置として保存
        startPosition = transform.position;

        // targetが設定されているか確認
        if (target != null)
        {
            // 目標地点の位置を保存
            endPosition = target.position;

            // Moveコルーチンを開始
            StartCoroutine(Move());
        }
        else
        {
            // targetが設定されていない場合はエラーをログに出力
            Debug.LogError("Targetが設定されていません。", this.gameObject);
        }
    }

    private IEnumerator Move()
    {
        // 無限ループで動きを繰り返す
        while (true)
        {
            // --- 前方へ移動 (startPosition から endPosition へ) ---
            float elapsedTime = 0f;
            while (elapsedTime < forwardTime)
            {
                // 経過時間 / 目的時間 で 0 から 1 の値 (t) を計算
                float t = elapsedTime / forwardTime;

                // Vector3.Lerpを使って2点間を線形補間
                transform.position = Vector3.Lerp(startPosition, endPosition, t);

                // 経過時間を加算
                elapsedTime += Time.deltaTime;

                // 次のフレームまで待機
                yield return null;
            }
            // ズレをなくすため、ループ終了時に強制的に目標地点へ設定
            transform.position = endPosition;

            // --- 元の位置へ戻る (endPosition から startPosition へ) ---
            elapsedTime = 0f; // 経過時間をリセット
            while (elapsedTime < backTime)
            {
                float t = elapsedTime / backTime;

                // 戻るときは Lerp の第2引数と第3引数が逆になる
                transform.position = Vector3.Lerp(endPosition, startPosition, t);

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            // ズレをなくすため、ループ終了時に強制的に開始位置へ設定
            transform.position = startPosition;
        }
    }
}