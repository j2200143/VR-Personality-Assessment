using UnityEngine;
using System.Collections.Generic;
using System.IO; // JSONファイル書き出しのため
using System;   // DateTimeのため
using System.Linq;

/// <summary>
/// プレイヤーの選択と応答時間を記録・管理するシングルトン。
/// 全タスク終了時には全タスクを行った者のユーザID(実行順に数字を割り当て)毎にその人の最終スコアを記録（PersonalityManagerの CalculateDomainScore()と GetAllDomainScores()で求めたもの。また、各ファセットの記録も（ AddFacetScore（）で各ファセットの記録は取っている？））、
///同一デバイスならPlayerPrefsで足りるが、オンラインでデータを取得したい場合はPlayFabなどで行った方がいい
/// </summary>
[System.Serializable]
public class ResponseData
{
    // どのファセット（性格特性）を測定しているか
    public string facet;
    // どの選択肢を選んだか (時間測定タスクの場合は -1)
    public int choiceIndex;
    // 応答にかかった時間（秒）または 測定された時間
    public float responseTime;
    // 回答した現実時間
    public string timestamp;

    /// <summary>
    /// コンストラクタ（選択肢タスク用）
    /// </summary>
    public ResponseData(string facet, int choiceIndex, float responseTime)
    {
        this.facet = facet;
        this.choiceIndex = choiceIndex;
        this.responseTime = responseTime;
        this.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// コンストラクタ（時間測定タスク用）
    /// </summary>
    public ResponseData(string facet, float timeValue)
    {
        this.facet = facet;
        this.choiceIndex = -1; // 選択肢はないので-1
        this.responseTime = timeValue; // 滞在時間などを記録
        this.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

// ResponseDataCollection を UserAnalyticsData に統合・変更
// 記録された全データをJSONに書き出すためのラッパークラス
[System.Serializable]
public class UserAnalyticsData
{
    public int userId;
    public string exportTimestamp;

    // 最終的な集計スコア
    public SerializableDictionary<string, int> finalDomainScores;
    public SerializableDictionary<string, int> finalFacetScores;

    // 個々の応答履歴
    public List<ResponseData> individualResponses;

    // (JsonUtilityのためのデフォルトコンストラクタ)
    public UserAnalyticsData() { }

    public UserAnalyticsData(
        int userId,
        List<ResponseData> responses,
        Dictionary<PersonalityDomain, int> domainScores,
        Dictionary<PersonalityFacet, int> facetScores)
    {
        this.userId = userId;
        this.exportTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        this.individualResponses = responses;

        // Dictionaryを変換
        this.finalDomainScores = new SerializableDictionary<string, int>();
        foreach (var pair in domainScores)
        {
            this.finalDomainScores[pair.Key.ToString()] = pair.Value;
        }

        this.finalFacetScores = new SerializableDictionary<string, int>();
        foreach (var pair in facetScores)
        {
            this.finalFacetScores[pair.Key.ToString()] = pair.Value;
        }
    }
}

// SONファイルに書き出す、全ユーザーのデータリスト
[System.Serializable]
public class AllUsersAnalyticsCollection
{
    public List<UserAnalyticsData> allUserData;

    public AllUsersAnalyticsCollection()
    {
        allUserData = new List<UserAnalyticsData>();
    }
}


[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();
    [SerializeField]
    private List<TValue> values = new List<TValue>();

    // save the dictionary to lists
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    // load dictionary from lists
    public void OnAfterDeserialize()
    {
        this.Clear();
        if (keys.Count != values.Count)
            throw new Exception($"SerializableDictionary: key count ({keys.Count}) != value count ({values.Count})");

        for (int i = 0; i < keys.Count; i++)
            this.Add(keys[i], values[i]);
    }
}


public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    // 全タスクの回答データを蓄積するリスト (このインスタンス固有)
    private List<ResponseData> allResponses = new List<ResponseData>();

    // タイマー用
    private float choiceStartTime;

    // 現在計測中のタスク情報
    private string currentFacet;

    // ユーザーID (実行順に割り当て)
    // PlayerPrefs を使って、PC全体でIDを永続化する
    private static int nextUserId = -1; // 未初期化を示す
    private int userId;
    //外部からIDを取得するためのプロパティ
    public int UserId => userId;

    private const string ALL_RESPONSES_FILENAME = "all_user_responses.json";


    void Awake()
    {
        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 実行順にIDを割り当て (PlayerPrefsから読み込む)
            if (nextUserId == -1) // アプリ起動後、初回のみ実行
            {
                // 以前保存したIDを読み込み、それに+1したものを次のIDとする
                nextUserId = PlayerPrefs.GetInt("NextUserID", 1);
            }

            userId = nextUserId; // このインスタンス(プレイヤー)のIDを確定
            PlayerPrefs.SetInt("NextUserID", nextUserId + 1); // 次回起動時のためにIDを+1して保存
            PlayerPrefs.Save();

            Debug.Log($"AnalyticsManager initialized for UserID: {userId}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// NPCが選択肢を表示した瞬間に呼び出す
    /// </summary>
    public void StartResponseTimer(string facet)
    {
        currentFacet = facet;
        choiceStartTime = Time.time; // 現在時刻を記録
    }

    /// <summary>
    /// プレイヤーが選択肢を選んだ瞬間に呼び出す
    /// </summary>
    public void LogResponse(int choiceIndex)
    {
        if (choiceStartTime == 0f || string.IsNullOrEmpty(currentFacet))
        {
            Debug.LogWarning("Timer was not started before logging response.");
            return;
        }

        float responseTime = Time.time - choiceStartTime;
        ResponseData data = new ResponseData(currentFacet, choiceIndex, responseTime);
        allResponses.Add(data);
        Debug.Log($"Logged Response: Facet={data.facet}, Choice={data.choiceIndex}, Time={data.responseTime:F2}s");

        choiceStartTime = 0f;
        currentFacet = null;
    }

    // 計測した時間を記録する
    public void LogTime(PersonalityFacet facet, float time)
    {
        ResponseData data = new ResponseData(facet.ToString(), time);
        allResponses.Add(data);
        Debug.Log($"Logged Time: Facet={data.facet}, Time={data.responseTime:F2}s");
    }

    /// <summary>
    /// 全タスク終了時などに呼び出し、全データをJSONファイルとして保存する。
    /// 単一のファイルに追記する
    /// </summary>
    public void ExportResponsesToJson()
    {
        if (allResponses.Count == 0 && PersonalityManager.Instance == null)
        {
            Debug.Log("No data to export.");
            return;
        }

        // 保存先のパスを決定 (固定ファイル名)
        string path = Path.Combine(Application.persistentDataPath, ALL_RESPONSES_FILENAME);

        AllUsersAnalyticsCollection collection = null;

        try
        {
            //  既存のJSONファイルがあれば読み込む
            if (File.Exists(path))
            {
                string existingJson = File.ReadAllText(path);
                if (!string.IsNullOrEmpty(existingJson))
                {
                    collection = JsonUtility.FromJson<AllUsersAnalyticsCollection>(existingJson);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read existing JSON file: {e.Message}. A new file will be created.");
        }

        // 読み込めなかった場合、新しいコレクションを作成
        if (collection == null)
        {
            collection = new AllUsersAnalyticsCollection();
        }

        //PersonalityManagerから今回の最終スコアを取得
        Dictionary<PersonalityDomain, int> domainScores = PersonalityManager.Instance.GetAllDomainScores();
        Dictionary<PersonalityFacet, int> facetScores = PersonalityManager.Instance.facetScores;

        //今回のユーザーの全データを作成
        UserAnalyticsData newUserData = new UserAnalyticsData(userId, allResponses, domainScores, facetScores);

        //既存のリストに今回のデータを追記
        collection.allUserData.Add(newUserData);

        try
        {
            //リスト全体をJSONに変換
            string json = JsonUtility.ToJson(collection, true);

            //ファイルに上書き保存
            File.WriteAllText(path, json);
            Debug.Log($"Successfully exported UserID {userId}'s data. Total users in file: {collection.allUserData.Count}. Path: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export responses to JSON: {e.Message}");
        }
    }
}

