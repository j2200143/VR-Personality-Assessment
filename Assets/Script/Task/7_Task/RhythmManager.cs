using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;
/// <summary>
/// リズムゲーム管理クラス
/// </summary>
public class RhythmManager : MonoBehaviour
{
    [Header(" --- オブジェクト参照 --- ")]
    [Tooltip("スタートボタン")]
    public Button startButton;
    [Tooltip("親オブジェクト")]
    public GameObject rhythmCanvas;
    [Tooltip("流れてくるImage付きのオブジェクト群")]
    public GameObject[] floatObjects;
    [Tooltip("ノーツが生成される位置（右端）")]
    public Transform spawnPoint;
    [Tooltip("判定エリアのコライダー（IsTrigger=On推奨）")]
    public Collider hitZoneCollider;
    [Tooltip("ノーツが消える位置（左端）")]
    public Transform endPoint;
    [Tooltip("InteractionManagerの参照")]
    public InteractionManager interactionManager;

    [Header(" --- ゲーム設定 --- ")]
    [Tooltip("ミス（お手付き）とみなす距離（コライダーの外側どれくらいまで反応するか）")]
    public float missThreshold = 0.4f;
    [Tooltip("ノーツが流れる最小スピード")]
    public float minSpeed = 1.5f;
    [Tooltip("ノーツが流れる最大スピード")]
    public float maxSpeed = 3.5f;
    [Tooltip("次のノーツが出るまでの最小間隔（秒）")]
    public float minInterval = 0.5f;
    [Tooltip("次のノーツが出るまでの最大間隔（秒）")]
    public float maxInterval = 1.8f;
    [Tooltip("コンボテキスト")]
    public Text comboText;

    [Header("タスクに関連するかどうか")]
    public bool isRelationTask = false;
    public PersonalityFacet personalityFacet = PersonalityFacet.E1_Friendliness;
    private bool isScored = false;
    [Tooltip("ゲームを始めたときに送信するスコア")]
    public int score = 0;
    [Tooltip("表示するオブジェクト")]
    public GameObject[] showObjects;
    [Tooltip("表示するメッセージ")]
    public string[] showMessage;

    [Header("焚火エフェクト")]
    public GameObject fireEffect;

    [Header("効果音")]
    public AudioClip audioClip_Success;
    public AudioClip audioClip_Miss;
    public AudioClip audioClip_Tap;

    // --- 内部変数 ---
    private int comboCount = 0;
    private bool isGameActive = false;
    private InputDevice rightController;
    private InputDevice leftController;
    private bool wasTriggerPressed = false;
    private Vector3 initialFireScale; // エフェクトの初期サイズ

    private List<ActiveNote> activeNotes = new List<ActiveNote>();

    private class ActiveNote
    {
        public GameObject gameObject;
        public Transform transform;
        public float speed;
        public bool isHit;

        public ActiveNote(GameObject obj, float spd)
        {
            gameObject = obj;
            transform = obj.transform;
            speed = spd;
            isHit = false;
        }
    }

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
        if (comboText != null)
        {
            comboText.text = "";
        }
        foreach (var obj in floatObjects)
        {
            if (obj != null) obj.SetActive(false);
        }
        if (fireEffect != null)
        {
            initialFireScale = fireEffect.transform.localScale;
        }
        GetController();
    }

    public void StartGame()
    {
        if (isGameActive) return;

        //スタートボタンをゲーム終了まで非表示にする
        startButton.gameObject.SetActive(false);
        //リセット
        comboText.text = "";

        if (rhythmCanvas != null) rhythmCanvas.SetActive(true);
        comboCount = 0;
        isGameActive = true;
        Debug.Log("Game Start!");

        StartCoroutine(SpawnNotesRoutine());
    }

    private void Update()
    {
        if (!isGameActive) return;
        MoveNotes();
        HandleInput();
    }

    private IEnumerator SpawnNotesRoutine()
    {
        for (int i = 0; i < floatObjects.Length; i++)
        {
            if (!isGameActive) yield break;

            GameObject obj = floatObjects[i];
            if (obj != null)
            {
                obj.transform.position = spawnPoint.position;
                obj.SetActive(true);
                float randomSpeed = Random.Range(minSpeed, maxSpeed);
                activeNotes.Add(new ActiveNote(obj, randomSpeed));
            }

            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);
        }

        yield return new WaitUntil(() => activeNotes.Count == 0);

        //ゲーム終了
        Debug.Log($"Game Finished! Final Combo: {comboCount}");
        isGameActive = false;
        if (isRelationTask)
        {
            //スコア送信
            if (!isScored)
            {
                isScored = true;
                PersonalityManager.Instance.AddFacetScore(personalityFacet, score);
            }

            for (int i = 0; i < showObjects.Length; i++)
            {
                showObjects[i].SetActive(true);
            }

            //メッセージ表示
            StoryManager.Instance.StartMiddleDialogue(showMessage);
        }

        //スタートボタンを表示する
        startButton.gameObject.SetActive(true);
        if (rhythmCanvas != null) rhythmCanvas.SetActive(false);

    }

    private void MoveNotes()
    {
        List<ActiveNote> notesToRemove = new List<ActiveNote>();

        Vector3 direction = (endPoint.position - spawnPoint.position);
        direction.y = 0;
        direction.Normalize();

        foreach (var note in activeNotes)
        {
            note.transform.position += direction * note.speed * Time.deltaTime;

            Vector3 toEnd = endPoint.position - note.transform.position;
            toEnd.y = 0;

            if (Vector3.Dot(toEnd, direction) < 0)
            {
                OnMiss("Miss: Passed through");
                note.gameObject.SetActive(false);
                notesToRemove.Add(note);
            }
        }

        foreach (var note in notesToRemove)
        {
            activeNotes.Remove(note);
        }
    }

    private void HandleInput()
    {
        GetController();

        bool triggerPressed = false;
        bool rightPressed = false;
        bool leftPressed = false;

        if (rightController.isValid)
        {
            if (rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightValueBool))
            {
                if (rightValueBool) rightPressed = true;
            }
        }

        if (leftController.isValid)
        {
            if (leftController.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftValueBool))
            {
                if (leftValueBool) leftPressed = true;
            }
        }

        if (Input.GetKey(KeyCode.N)) rightPressed = true;

        if (StoryManager.Instance.isPCMode && Input.GetMouseButtonDown(0))
        {
            rightPressed = true;
        }

        triggerPressed = rightPressed || leftPressed;

        if (triggerPressed && !wasTriggerPressed)
        {
            CheckHit();
        }

        wasTriggerPressed = triggerPressed;
    }

    private void CheckHit()
    {
        if (activeNotes.Count == 0) return;
        if (hitZoneCollider == null)
        {
            Debug.LogError("HitZoneColliderが設定されていません");
            return;
        }

        // 1. 一番近い（判定すべき）ノーツを探す
        // 判定エリアの中心点からのX軸距離で最も近いものを対象とする
        ActiveNote closestNote = null;
        float minDistanceX = float.MaxValue;
        Vector3 zoneCenter = hitZoneCollider.bounds.center;

        foreach (var note in activeNotes)
        {
            // X座標の差分絶対値
            float distX = Mathf.Abs(note.transform.position.x - zoneCenter.x);
            if (distX < minDistanceX)
            {
                minDistanceX = distX;
                closestNote = note;
            }
        }

        if (closestNote == null) return;

        // 2. 範囲判定：ノーツの中心座標がコライダーの中に入っているか？
        // BoxCollider.bounds.Contains は点が含まれているかを判定します
        if (hitZoneCollider.bounds.Contains(closestNote.transform.position))
        {
            // --- 成功 (Good) ---
            OnSuccess();

            // ノーツを消す
            closestNote.gameObject.SetActive(false);
            activeNotes.Remove(closestNote);
        }
        else
        {
            // --- 範囲外だが、惜しい距離（お手付き）かどうかの判定 ---
            // bounds.extents.x は「中心から端までの距離（幅の半分）」
            float colliderHalfWidth = hitZoneCollider.bounds.extents.x;

            // 「中心からの距離」が「幅の半分 + 許容誤差」以内ならお手付き
            if (minDistanceX <= colliderHalfWidth + missThreshold)
            {
                // お手付き (Bad)
                OnMiss("Miss: Too early/late");

                // ノーツを消す
                closestNote.gameObject.SetActive(false);
                activeNotes.Remove(closestNote);
            }
            else
            {
                // 遠すぎる（空振り）
                if (audioClip_Tap != null) AudioManager.Instance.PlaySound(audioClip_Tap, 1f);
            }
        }
    }

    private void OnSuccess()
    {
        comboCount++;
        Debug.Log($"<color=green>Good!</color> Combo: {comboCount}");
        comboText.text = $"<color=green>Good!</color> {comboCount}";
        if (audioClip_Success != null) AudioManager.Instance.PlaySound(audioClip_Success, 1f);

        // エフェクトを大きくする（DOTween）
        // 現在のスケールから1.2倍にし、Yoyo（行って戻る）で元のサイズに戻す
        if (fireEffect != null)
        {
            fireEffect.transform.DOKill(); // 重複実行防止
            fireEffect.transform.localScale = initialFireScale; // 一旦リセット

            // 1.2倍まで拡大して戻る。所要時間は minInterval (次のノーツまでの最短時間) を目安にする
            fireEffect.transform.DOScale(initialFireScale * 1.2f, minInterval / 2f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);
        }
    }

    private void OnMiss(string reason)
    {
        comboCount = 0;
        Debug.Log($"<color=red>{reason}</color>");
        comboText.text = "";
        if (audioClip_Miss != null) AudioManager.Instance.PlaySound(audioClip_Miss, 1f);
    }

    private void GetController()
    {
        if (interactionManager != null)
        {
            this.rightController = interactionManager.rightController;
            this.leftController = interactionManager.leftController;
        }
    }
}