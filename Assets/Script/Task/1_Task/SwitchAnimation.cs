using UnityEngine;
using DG.Tweening; // DOTweenを使用するために必要

public class SwitchAnimation : MonoBehaviour
{
    [Header("レバーを引くためのボタン")]
    public GameObject[] buttons;

    [Header("レバーの設定")]
    // 複数のレバーを登録できるように配列にしています
    public Transform[] switchLevers;

    [Tooltip("レバーを倒した後のX軸の角度")]
    public float targetLeverRotationX = 30f;

    [Tooltip("レバーが倒れるまでの時間（秒）")]
    public float leverAnimDuration = 1.5f;

    [Header("ドアの設定")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Tooltip("左ドアの移動先の位置（空のGameObjectなどを配置して指定）")]
    public Transform afterMoveLeftDoorPosition;

    [Tooltip("右ドアの移動先の位置（空のGameObjectなどを配置して指定）")]
    public Transform afterMoveRightDoorPosition;

    [Tooltip("ドアが開くまでの時間（秒）")]
    public float doorAnimDuration = 2.5f;

    [Header("全ての動作が完了した際にプレイヤーに表示するメッセージ")]
    public string[] messages;

    [Header("オーディオ")]
    public AudioClip soundLever, soundDoor;



    /// <summary>
    /// ボタンから呼び出す関数
    /// </summary>
    /// <param name="index">動かすレバーの配列インデックス</param>
    public void ChoiceSwitch(int index)
    {
        //メッセージ表示中は動作しない
        if (StoryManager.Instance.isExcuting)
        {
            Debug.Log("メッセージ表示中です");
            return;
        }
        // インデックスが不正な場合は処理しない
        if (index < 0 || index >= switchLevers.Length)
        {
            Debug.LogError("指定されたインデックスのレバーが存在しません。");
            return;
        }

        //二度目の動作を防ぐためbuttonを非表示にする
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].SetActive(false);
        }

        Transform targetLever = switchLevers[index];

        // --- アニメーションのシーケンス（順序）を作成 ---
        Sequence seq = DOTween.Sequence();

        // 1. 効果音再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(soundLever, 1f);
        }

        // 2. レバーのアニメーション (回転)
        // 現在の角度から、指定したX角度(30度)へ回転させる
        // LocalRotateにすることで、親オブジェクトが回転していても正しく動きます
        seq.Append(targetLever.DOLocalRotate(new Vector3(targetLeverRotationX, 0, 0), leverAnimDuration));

        // 3. ドアのアニメーション (移動)
        // Appendを使うことで、上のレバーアニメーションが「終わってから」開始します

        // 左ドアの移動
        seq.Append(leftDoor.DOMove(afterMoveLeftDoorPosition.position, doorAnimDuration));

        // 右ドアの移動
        // Joinを使うことで、直前の左ドアのアニメーションと「同時に」開始します
        seq.Join(rightDoor.DOMove(afterMoveRightDoorPosition.position, doorAnimDuration));

        // 効果音再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(soundDoor, 1f);
        }
        // 4. 終了処理
        seq.OnComplete(() =>
        {
            Debug.Log("全ての動作が完了しました");

            //プレイヤーにメッセージを表示
            StoryManager.Instance.StartMiddleDialogue(messages);
        });
    }
}