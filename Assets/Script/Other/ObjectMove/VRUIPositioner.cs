using UnityEngine;

public class VRUIPositioner : MonoBehaviour
{
    [Tooltip("追従対象のカメラ")]
    public Transform targetCamera;
    [Tooltip("カメラからの距離")]
    public float distanceFromCamera = 4.0f;
    [Tooltip("追従の滑らかさ")]
    public float smoothTime = 0.3f;

    private Vector3 velocity = Vector3.zero;

    void Update()
    {
        if (targetCamera == null)
        {
            // MainCameraを自動取得
            if (Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
            return;
        }

        // 1. 目標位置を計算（カメラの正面 + 指定距離）
        Vector3 targetPosition = targetCamera.position + (targetCamera.forward * distanceFromCamera);

        // 2. 現在地から目標位置へ滑らかに移動
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // 3. 常にカメラの方を向く
        transform.LookAt(transform.position + targetCamera.rotation * Vector3.forward, targetCamera.rotation * Vector3.up);
    }
}