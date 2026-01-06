using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class Task_FullPreparation_C5 : MonoBehaviour
{
    [Header("設定")]
    public PersonalityFacet personalityFacet = PersonalityFacet.C5_SelfDiscipline;

    [Header("ドア関連")]
    public Transform rightDoor, leftDoor;
    public Transform afterRightDoorPosition, afterLeftDoorPosition;
    public float moveTime = 2f;
    public Button openDoorButton;
    public Transform touchTransform_Door;
    public Transform afterTouchTransform_Door;

    [Header("補助装置起動エフェクト")]
    public GameObject[] chargeEffect;
    public Button[] chargeButton;
    [Header("ボタンアニメーション用")]
    public Transform[] touchTransform; // ボタンの可動部分
    public Transform[] afterTouchTransform; // 押し込まれた位置（目標地点）

    [Header("ドアの上に表示する何個補助装置を起動したかを示すエフェクト")]
    public GameObject[] chargeLampEffect;

    [System.Serializable]
    public class MessageGroup
    {
        [TextArea(2, 3)]
        public string[] messages;
    }
    [Header("起動スイッチを押した後のメッセージ（天の声）")]
    [Tooltip("Element 0:(0点) ～ Element 4:(4点)")]
    public MessageGroup[] resultMessages;

    [Header("効果音")]
    public AudioClip audioClip_Charge;
    public AudioClip audioClip_Door;

    private int chargeCount = 0;//補助装置のボタンを押した回数（4回まで）

    void Start()
    {
        for (int i = 0; i < chargeEffect.Length; i++)
        {
            chargeEffect[i].SetActive(false);
        }
        for (int i = 0; i < chargeLampEffect.Length; i++)
        {
            chargeLampEffect[i].SetActive(false);
        }
        for (int i = 0; i < chargeButton.Length; i++)
        {
            int index = i;
            chargeButton[index].onClick.AddListener(() => ClickChargeButton(index));
        }

        openDoorButton.onClick.AddListener(OpenDoorButton);
    }

    public void ClickChargeButton(int index)
    {
        StartCoroutine(Charge(index));
    }
    //補助装置のボタンにアタッチ
    private IEnumerator Charge(int index)
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.sound3DButton, 1f);
        //ボタンのアニメーション (DOTween)
        // 押し込んで戻る
        if (touchTransform[index] != null && afterTouchTransform[index] != null)
        {
            Vector3 originalPos = touchTransform[index].position;
            Sequence seq = DOTween.Sequence();
            seq.Append(touchTransform[index].DOMove(afterTouchTransform[index].position, 0.2f));
            seq.Append(touchTransform[index].DOMove(originalPos, 0.2f));
            yield return seq.WaitForCompletion();
        }
        else
        {
            yield return new WaitForSeconds(0.5f); // アニメーションがない場合のウェイト
        }

        AudioManager.Instance.PlaySound(audioClip_Charge, 1f);
        //重複防止
        chargeButton[index].gameObject.SetActive(false);
        chargeLampEffect[chargeCount].SetActive(true);

        chargeCount++;

        chargeEffect[index].SetActive(true);
    }

    //起動スイッチ
    public void OpenDoorButton()
    {
        StartCoroutine(OpenDoor());
    }
    private IEnumerator OpenDoor()
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.sound3DButton, 1f);
        //ボタンのアニメーション (DOTween)
        // 押し込んで戻る
        if (touchTransform_Door != null && afterTouchTransform_Door != null)
        {
            Vector3 originalPos = touchTransform_Door.position;
            Sequence seq = DOTween.Sequence();
            seq.Append(touchTransform_Door.DOMove(afterTouchTransform_Door.position, 0.2f));
            seq.Append(touchTransform_Door.DOMove(originalPos, 0.2f));
            yield return seq.WaitForCompletion();
        }
        else
        {
            yield return new WaitForSeconds(0.5f); // アニメーションがない場合のウェイト
        }

        AudioManager.Instance.PlaySound(audioClip_Door, 1f);

        rightDoor.DOMove(afterRightDoorPosition.position, moveTime);
        leftDoor.DOMove(afterLeftDoorPosition.position, moveTime);


        //評価
        PersonalityManager.Instance.AddFacetScore(personalityFacet, chargeCount);

        //メッセージ表示
        StoryManager.Instance.StartMiddleDialogue(resultMessages[chargeCount].messages, () =>
        {
            StoryManager.Instance.MoveNextScene();
        });
    }
}
