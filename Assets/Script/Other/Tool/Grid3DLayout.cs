using UnityEngine;

public class Grid3DLayout : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("1行（または1列）あたりのオブジェクト数")]
    [Min(1)] public int columnCount = 5;

    [Tooltip("オブジェクト間の距離 (X, Y, Z)")]
    public Vector3 spacing = new Vector3(1.5f, 0f, 1.5f);

    [Tooltip("開始位置のオフセット")]
    public Vector3 startOffset = Vector3.zero;

    [Header("Layout Axis")]
    [Tooltip("どの平面に並べるか")]
    public LayoutPlane layoutPlane = LayoutPlane.XZ_Floor;

    [Header("並べるオブジェクト")]
    public GameObject[] targetObjectArray;


    public enum LayoutPlane
    {
        XZ_Floor, // 床に並べる (XとZ)
        XY_Wall,  // 壁に並べる (XとY)
        YZ_Wall   // 横壁に並べる (ZとY)
    }

    // インスペクターのコンテキストメニューから実行可能にする
    [ContextMenu("並び替え")]
    public void ArrangeObjects()
    {
        if (targetObjectArray == null || targetObjectArray.Length == 0)
        {
            Debug.LogWarning("Target Object Array is empty.");
            return;
        }

        if (columnCount < 1) columnCount = 1; // ゼロ除算防止

        for (int i = 0; i < targetObjectArray.Length; i++)
        {
            if (targetObjectArray[i] == null) continue;

            // グリッド座標の計算
            int xIndex = i % columnCount; // 列 (横)
            int yIndex = i / columnCount; // 行 (縦/奥行き)

            // ローカル座標の算出
            Vector3 newPos = Vector3.zero;

            switch (layoutPlane)
            {
                case LayoutPlane.XZ_Floor:
                    // X軸とZ軸（床）に並べる。Zは奥にいくほどプラス、または手前ならマイナス
                    newPos = new Vector3(xIndex * spacing.x, 0, -yIndex * spacing.z);
                    break;

                case LayoutPlane.XY_Wall:
                    // X軸とY軸（壁）に並べる。Yは下に行くほどマイナスにするのが一般的
                    newPos = new Vector3(xIndex * spacing.x, -yIndex * spacing.y, 0);
                    break;

                case LayoutPlane.YZ_Wall:
                    // Z軸とY軸（横向きの壁）に並べる
                    newPos = new Vector3(0, -yIndex * spacing.y, xIndex * spacing.z);
                    break;
            }

            // オフセットを加えて適用
            // 親オブジェクトからの相対座標(localPosition)を使用
            targetObjectArray[i].transform.localPosition = newPos + startOffset;

            // 階層整理（オプション：スクリプトがついているオブジェクトの子にする場合）
            // targetObjectArray[i].transform.SetParent(this.transform, true);
        }

        Debug.Log($"Arranged {targetObjectArray.Length} objects.");
    }


}