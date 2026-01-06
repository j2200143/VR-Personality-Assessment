using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Text;
/// <summary>
/// 各タスクの結果（ファセットスコア）を記録し、最終的な性格特性（ドメインスコア）を計算する管理クラス。
/// シングルトンパターンを使用しており、どのスクリプトからでも PersonalityManager.Instance でアクセスできます。
/// </summary>
public class PersonalityManager : MonoBehaviour
{
    [Header("デバッグ用にスコアをコンソールに表示するか")]
    public bool isDebug = true;

    // 各タスクの満点を定数として定義し、全タスクで統一
    public const int TASK_MAX_SCORE = 4;

    // 各ファセットの「現在のスコア」を保存するDictionary
    public Dictionary<PersonalityFacet, int> facetScores = new Dictionary<PersonalityFacet, int>();
    // 各ファセットの「満点（最高スコア）」を保存するDictionary
    public Dictionary<PersonalityFacet, int> maxFacetScores = new Dictionary<PersonalityFacet, int>();

    public static PersonalityManager Instance { get; private set; }

    [Header("保存ファイル名 (拡張子.csvを含める)")]
    public string saveFileName = "PersonalityResults.csv";

    void Awake()
    {
        // シングルトンパターンの実装
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeScores();
    }

    /// <summary>
    /// 全てのファセットスコアと最高スコアを0で初期化します。
    /// </summary>
    void InitializeScores()
    {
        // Enum.GetValuesを使って、全てのPersonalityFacetメンバーに対してループ処理
        foreach (PersonalityFacet facet in Enum.GetValues(typeof(PersonalityFacet)))
        {
            facetScores[facet] = 0;
            maxFacetScores[facet] = 0;
        }
    }

    /// <summary>
    /// タスクからファセットのスコアを加算します。
    /// </summary>
    /// <param name="facet">スコアを加算するファセット</param>
    /// <param name="scoreToAdd">加算するスコア</param>
    public void AddFacetScore(PersonalityFacet facet, int scoreToAdd)
    {
        if (facetScores.ContainsKey(facet))
        {
            facetScores[facet] += scoreToAdd;
            if (isDebug)
                Debug.Log($"{facet} に {scoreToAdd} を加算。現在の合計: {facetScores[facet]}");
        }
    }

    /// <summary>
    /// あるファセットを測定するタスクが実行されたことを記録します。
    /// これにより、最終的な割合計算の分母が正しく設定されます。
    /// 各タスクの開始時に一度だけ呼び出してください。
    /// 配布時においてはStoryManagerのOnSceneLoadedで呼び出しています。
    /// </summary>
    /// <param name="facet">測定対象のファセット</param>
    public void RegisterExecutedTask(PersonalityFacet facet)
    {
        if (maxFacetScores.ContainsKey(facet))
        {
            maxFacetScores[facet] += TASK_MAX_SCORE;
            if (isDebug)
                Debug.Log($"{facet} のタスクが実行されました。このファセットの最大スコア合計: {maxFacetScores[facet]}");
        }
    }


    /// <summary>
    /// 指定されたドメインの最終スコアをパーセンテージ（0-100）で計算します。
    /// </summary>
    /// <param name="domain">計算したいドメイン</param>
    /// <returns>0から100の間のパーセンテージ</returns>
    public int CalculateDomainScore(PersonalityDomain domain)
    {
        // 指定されたドメインに属するファセットのリストを取得
        List<PersonalityFacet> facetsInDomain = GetFacetsForDomain(domain);

        int totalScore = 0;
        int totalMaxScore = 0;

        foreach (PersonalityFacet facet in facetsInDomain)
        {
            // そのプレイヤーが体験したタスクのスコアと満点のみを合計
            if (maxFacetScores.ContainsKey(facet) && maxFacetScores[facet] > 0)
            {
                totalScore += facetScores[facet];
                totalMaxScore += maxFacetScores[facet];
            }
        }

        // ゼロ除算を避ける
        if (totalMaxScore == 0)
        {
            return 0; // または -1 など、未測定を示す値を返す
        }

        // (float)でキャストし、小数として計算してから四捨五入する
        return Mathf.RoundToInt(((float)totalScore / totalMaxScore) * 100.0f);
    }

    /// <summary>
    /// 全てのドメインの最終スコアを計算して返します。
    /// </summary>
    /// <returns>各ドメインとそのパーセンテージスコアを含むDictionary</returns>
    public Dictionary<PersonalityDomain, int> GetAllDomainScores()
    {
        var domainScores = new Dictionary<PersonalityDomain, int>();
        foreach (PersonalityDomain domain in Enum.GetValues(typeof(PersonalityDomain)))
        {
            domainScores[domain] = CalculateDomainScore(domain);
        }

        if (isDebug)
        {
            Debug.Log("--- 最終結果 ---");
            foreach (var result in domainScores)
            {
                Debug.Log($"{result.Key}: {result.Value}%");
            }
            Debug.Log("----------------");
        }

        return domainScores;
    }

    /// <summary>
    /// 指定されたドメインに属するファセットのリストを返すヘルパーメソッド。
    /// </summary>
    private List<PersonalityFacet> GetFacetsForDomain(PersonalityDomain domain)
    {
        // LINQを使って、enumの名前からドメインに属するファセットをフィルタリング
        return Enum.GetValues(typeof(PersonalityFacet))
                   .Cast<PersonalityFacet>()
                   .Where(f => f.ToString().StartsWith(domain.ToString()))
                   .ToList();
    }

    //SaveResultsToFile()の呼び出し関数
    public void ResultFile()
    {
        // AnalyticsManagerからIDを取得し、文字列に変換して渡す
        // 必要であれば "Player_" + ... のように接頭辞をつける
        if (AnalyticsManager.Instance != null)
        {
            string playerId = AnalyticsManager.Instance.UserId.ToString();
            SaveResultsToFile(playerId);
        }
        else
        {
            Debug.LogWarning("AnalyticsManager Instance is null. Saving with default ID.");
            SaveResultsToFile("UnknownPlayer");
        }
    }
    /// <summary>
    /// 結果をCSVファイルに追記保存します。
    /// 保存場所: Unityエディタ上では「Assets」フォルダ直下、ビルド後は「PersistentDataPath」
    /// </summary>
    /// <param name="playerId">プレイヤーIDや名前</param>
    public void SaveResultsToFile(string playerId)
    {
        // 保存先のパスを決定（エディタならAssets直下、実機なら推奨パス）
#if UNITY_EDITOR
        string folderPath = Application.dataPath; 
#else
        string folderPath = Application.persistentDataPath;
#endif
        string filePath = Path.Combine(folderPath, saveFileName);

        // ファイルが存在しない場合はヘッダー行を作成
        bool fileExists = File.Exists(filePath);

        StringBuilder sb = new StringBuilder();

        // --- ヘッダー作成 (初回のみ) ---
        if (!fileExists)
        {
            sb.Append("Timestamp,ID");

            // ドメインのカラム (例: O_Percent, O_Score)
            foreach (PersonalityDomain domain in Enum.GetValues(typeof(PersonalityDomain)))
            {
                sb.Append($",{domain}_Result"); // 例: O:50%-12点 という文字列が入る列
            }

            // ファセットのカラム (例: O1_Percent, O1_Score)
            foreach (PersonalityFacet facet in Enum.GetValues(typeof(PersonalityFacet)))
            {
                sb.Append($",{facet}_Result"); // 例: O1:50%-2点
            }
            sb.Append("\n");
        }

        // --- データ行作成 ---
        // 日時とID
        sb.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{playerId}");

        // 1. ドメインの書き出し (例: N:50%-12点)
        foreach (PersonalityDomain domain in Enum.GetValues(typeof(PersonalityDomain)))
        {
            int percent = CalculateDomainScore(domain);
            int rawScore = GetDomainRawScore(domain); // 生スコア合計取得用メソッドを追加して使用

            // CSVのエスケープ処理が必要な場合は注意ですが、今回は単純な文字列結合
            sb.Append($",{domain}:{percent}%-{rawScore}点");
        }

        // 2. ファセットの書き出し (例: N1:50%-2点)
        foreach (PersonalityFacet facet in Enum.GetValues(typeof(PersonalityFacet)))
        {
            int current = facetScores.ContainsKey(facet) ? facetScores[facet] : 0;
            int max = maxFacetScores.ContainsKey(facet) ? maxFacetScores[facet] : 0;
            int percent = 0;

            if (max > 0)
            {
                percent = Mathf.RoundToInt(((float)current / max) * 100.0f);
            }

            // ファセット名が長い場合があるので、短縮したい場合はここで加工可能
            sb.Append($",{facet}:{percent}%-{current}点");
        }

        sb.Append("\n");

        // --- ファイル書き込み ---
        try
        {
            // UTF-8 (BOM付き) で追記保存。Excelで開くときの文字化け防止のためEncoding.UTF8を使用
            // ※StreamWriterの第2引数 true は Append(追記)モード
            using (StreamWriter sw = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                sw.Write(sb.ToString());
            }

            if (isDebug) Debug.Log($"結果を保存しました: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV書き込みエラー: {e.Message}");
        }
    }
    /// <summary>
    /// 指定されたドメインの合計生スコアを取得
    /// </summary>
    public int GetDomainRawScore(PersonalityDomain domain)
    {
        List<PersonalityFacet> facetsInDomain = GetFacetsForDomain(domain);
        int totalScore = 0;
        foreach (PersonalityFacet facet in facetsInDomain)
        {
            if (facetScores.ContainsKey(facet))
                totalScore += facetScores[facet];
        }
        return totalScore;
    }
}

/// <summary>
/// BigFiveの5つのドメイン（次元）を定義します。
/// </summary>
public enum PersonalityDomain
{
    O, // 開放性
    C, // 誠実性
    E, // 外向性
    A, // 協調性
    N  // 神経症傾向
}

/// <summary>
/// IPIP-NEO-120の30個のファセット（下位次元）を定義します。
/// </summary>
public enum PersonalityFacet
{
    // 開放性 (Openness)
    O1_Imagination, O2_ArtisticInterests, O3_Emotionality, O4_Adventurousness, O5_Intellect, O6_Liberalism,
    // 誠実性 (Conscientiousness)
    C1_SelfEfficacy, C2_Orderliness, C3_Dutifulness, C4_AchievementStriving, C5_SelfDiscipline, C6_Cautiousness,
    // 外向性 (Extraversion)
    E1_Friendliness, E2_Gregariousness, E3_Assertiveness, E4_ActivityLevel, E5_ExcitementSeeking, E6_Cheerfulness,
    // 協調性 (Agreeableness)
    A1_Trust, A2_Morality, A3_Altruism, A4_Cooperation, A5_Modesty, A6_Sympathy,
    // 神経症傾向 (Neuroticism)
    N1_Anxiety, N2_Anger, N3_Depression, N4_SelfConsciousness, N5_Immoderation, N6_Vulnerability
}

