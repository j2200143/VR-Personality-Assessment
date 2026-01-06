using UnityEngine;

/// <summary>
/// オブジェクトをY軸回転させながら、Y軸で上下に浮遊させるスクリプト。
/// これを浮遊させたいオブジェクトに直接アタッチします。
/// </summary>
public class Floater : MonoBehaviour
{
    [Header("浮遊設定")]
    [Tooltip("Y軸の回転速度（1秒あたりの角度）")]
    public float rotationSpeed = 25f;

    [Tooltip("上下移動の速度")]
    public float bobSpeed = 1f;

    [Tooltip("上下移動の振幅（どれくらいの高さまで動くか）")]
    public float bobHeight = 0.15f;

    // 浮遊の中心となるY座標
    private float startY;

    void Start()
    {
        // 起動時のY座標を浮遊の中心として記憶
        this.startY = transform.position.y;
    }

    // Updateは、このオブジェクトがアクティブな時だけ自動で呼ばれます
    void Update()
    {
        // 1. Y軸で回転させる (処理は非常に軽いです)
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // 2. Y軸で上下させる (Mathf.Sinは非常に高速な処理です)

        // Mathf.Sin() は -1 から 1 の間で滑らかに変化する波（サイン波）を返します。
        // Time.time * bobSpeed で波の速さを制御します。
        float newY = startY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        // 現在のX, Z座標はそのままに、Y座標だけを更新
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}