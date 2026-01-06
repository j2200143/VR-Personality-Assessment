using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // SceneManagerのために必要
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    private Image fadeImage; // フェードイン・アウトに用いるImage
    private string fadeImageTag = "FadeImage"; // 検索に使うタグ名

    [SerializeField]
    private float fadeDuration = 2.0f; // フェードにかかる時間

    void Awake()
    {
        // シングルトン化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンをまたいでも破棄されないようにする
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        // シーンがロードされたら FadeIn を呼ぶように登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // シーンがロードされた直後に自動で呼ばれる
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindImageByTag();

        // 新しいシーンが始まったらフェードイン（黒から透明へ）
        StartCoroutine(FadeIn());
    }

    /// <s_ummary>
    /// シーンをフェードアウト（透明から黒へ）してからロードします。
    /// </summary>
    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    // タグを使ってImageを検索し、変数に格納するメソッド
    private void FindImageByTag()
    {
        GameObject fadeObj = GameObject.FindGameObjectWithTag(fadeImageTag);
        if (fadeObj != null)
        {
            fadeImage = fadeObj.GetComponent<Image>();
            if (fadeImage == null)
            {
                Debug.Log($"'{fadeImageTag}'タグのオブジェクトにImageコンポーネントがありません。", fadeObj);
            }
        }
        else
        {
            Debug.Log($"'{fadeImageTag}'タグを持つオブジェクトがシーンに見つかりません。");
            fadeImage = null; // 見つからなかった場合はnullを明示
        }
    }

    // フェードイン（黒 → 透明）
    public IEnumerator FadeIn()
    {
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0f, 0f, 0f, 1f); // まず真っ黒にする
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = 1f - (timer / fadeDuration);
                fadeImage.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            fadeImage.color = new Color(0f, 0f, 0f, 0f); // 完全に透明に

            //thisSceneSOに基づいてメッセージを表示する 
            StoryManager.Instance.StartDialogue(true, StoryManager.Instance.isStoryVersion);
        }

    }

    // フェードアウト（透明 → 黒）
    public IEnumerator FadeOut()
    {
        fadeImage.color = new Color(0f, 0f, 0f, 0f); // まず透明にする
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = timer / fadeDuration;
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, 1f); // 完全に真っ黒に
    }

    // フェードアウトしてからシーンをロードする一連の流れ
    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        // 1. フェードアウト（透明から黒へ）
        yield return StartCoroutine(FadeOut());

        // 2. フェードアウトが完了したらシーンをロード
        SceneManager.LoadScene(sceneName);
    }
}