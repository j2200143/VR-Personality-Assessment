using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using DG.Tweening;
using System.Collections.Generic;

public class Boomerang : MonoBehaviour
{
    [Header("設定")]
    public GameObject boomerangObject; // 投げるブーメランのモデル
    public GameObject manualCanvas;    // 「トリガーで投げる」UI
    public Text manualText;
    private string manualMessage = "トリガーボタンで投げる";

    [Header("投げ設定")]
    //public float throwDistance = 10f;
    public float throwDuration = 1.5f;
    public Transform endBoomerangTransform;

    [Header("タスクと関連している場合に設定")]
    public bool isRelationTask = false;
    [Tooltip("投げ終わった後にSceneを移動するかどうか")]
    public bool isMoveNextScene = false;
    public PersonalityFacet personalityFacet = PersonalityFacet.E6_Cheerfulness;
    private bool isScored = false;

    [Header("効果音")]
    public AudioClip audioClip_Boomerang;

    // 内部変数
    private bool canThrow = false; // エリア内にいるか
    private bool isThrowing = false;
    private InputDevice rightController;

    void Start()
    {
        if (manualText != null) manualText.text = manualMessage;
        if (manualCanvas != null) manualCanvas.SetActive(false);
        if (boomerangObject != null) boomerangObject.SetActive(false);

        InitializeController();

        if (StoryManager.Instance.isPCMode)
        {
            manualMessage = "左クリックで投げる";
            if (manualText != null) manualText.text = manualMessage;
        }
    }

    void Update()
    {
        if (canThrow && !isThrowing)
        {
            CheckInput();
        }
    }

    private void CheckInput()
    {
        if (!rightController.isValid) InitializeController();

        bool triggerPressed = false;
        if (rightController.isValid)
            rightController.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);

        // VRトリガー または PCデバッグ用(Nキー)
        if (triggerPressed || Input.GetKeyDown(KeyCode.N) || (StoryManager.Instance.isPCMode && Input.GetMouseButtonDown(0)))
        {
            ThrowBoomerang();
        }
    }

    private void InitializeController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0) rightController = devices[0];
    }

    private void ThrowBoomerang()
    {
        isThrowing = true;
        if (manualCanvas != null) manualCanvas.SetActive(false);

        // ブーメランを表示・配置
        boomerangObject.SetActive(true);
        boomerangObject.transform.localPosition = Vector3.zero;
        boomerangObject.transform.localRotation = Quaternion.identity;

        // 行動スコア加算（遊んだ = +2点）
        if (isRelationTask && !isScored)
        {
            isScored = true;
            int firstStepScore = (int)(float)PersonalityManager.TASK_MAX_SCORE / 4;
            int score = firstStepScore * 2;
            PersonalityManager.Instance.AddFacetScore(personalityFacet, score);
        }


        // アニメーション (行って戻る)
        AudioManager.Instance.PlayLoopingSoundSub(audioClip_Boomerang);

        Sequence seq = DOTween.Sequence();

        seq.Join(boomerangObject.transform.DOMove(endBoomerangTransform.position, throwDuration * 0.5f).SetEase(Ease.OutQuad));

        seq.Join(boomerangObject.transform.DOLocalRotate(new Vector3(0, 720, 0), throwDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));

        // 戻ってくる (Localの原点 Vector3.zero へ移動)
        // 戻りの移動は親オブジェクトの座標系に戻るため、DOLocalMove(Vector3.zero)で問題ありません。
        seq.Append(boomerangObject.transform.DOLocalMove(Vector3.zero, throwDuration * 0.5f).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            AudioManager.Instance.StopLoopingSoundSub();
            boomerangObject.SetActive(false);
            isThrowing = false;

            if (isMoveNextScene)
            {
                StoryManager.Instance.MoveNextScene();
            }
        });
    }

    // エリア侵入判定
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canThrow = true;
            if (manualCanvas != null) manualCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canThrow = false;
            if (manualCanvas != null) manualCanvas.SetActive(false);
        }
    }
}