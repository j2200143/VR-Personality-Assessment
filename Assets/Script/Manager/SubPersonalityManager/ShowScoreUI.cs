using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Linq;
using System.Text;

public class ShowScoreUI : MonoBehaviour
{
    [Header("基本UI設定")]
    [Tooltip("診断結果表示を開始するボタン")]
    public Button showButton;

    [Tooltip("スコアUIの親オブジェクト（ポップアップ全体）")]
    public GameObject scoreCanvasRoot;

    [Header("パネル切替設定")]
    [Tooltip("各ドメインのスコア得点率を示すパネル")]
    public GameObject allPanel;//概要
    public Text scoringRateText_O, scoringRateText_C, scoringRateText_E, scoringRateText_A, scoringRateText_N;
    [Tooltip("どのパネルを選択しているかわかりやすくするパネル")]
    public GameObject choicedPanel_all;

    [Tooltip("ドメイン詳細説明が表示されるパネル（共有パネル）")]
    public GameObject domainInfoPanel;

    [Tooltip("概要パネルと詳細パネルを切り替えるボタン")]
    public Button toggleViewButton;

    [Header("詳細パネル内の共有テキスト")]
    [Tooltip("選択中のドメイン名を表示するText（例：開放性）")]
    public Text domainName;

    [Tooltip("選択中のドメインの説明文を表示するText")]
    public Text sharedExplanationText;

    [Tooltip("選択中のドメインの得点率を表示するText")]
    public Text sharedPercentText;

    [Tooltip("ファセット（下位次元）の情報を表示するTextリスト（通常は6つ）")]
    public List<Text> sharedFacetTexts;

    [Header("ドメイン選択ボタン設定")]
    [Tooltip("各ドメインを選択するボタンと設定のリスト")]
    public List<DomainUISetting> domainSettings;

    // --- データ構造クラス ---
    [System.Serializable]
    public class DomainUISetting
    {
        public string displayName = "表示名（例：O:開放性）";
        public PersonalityDomain domain;    // O, C, E, A, N
        public Button selectButton;
        public GameObject choicedPanel;//どのパネルを選択しているかわかりやすくするパネル
    }
    // ドメインごとの「一般的な平均範囲」を定義する構造体
    [System.Serializable]
    public struct DomainNorm
    {
        public PersonalityDomain domain;
        public int averageMin; // 平均的とされる範囲の下限
        public int averageMax; // 平均的とされる範囲の上限
        // ※この範囲より上が「高い」、下が「低い」になります
    }
    //データ数が少ないので平均範囲は一律にする
    private List<DomainNorm> defaultNorms = new List<DomainNorm>()
    {
        new DomainNorm { domain = PersonalityDomain.O, averageMin = 30, averageMax = 70 },
        new DomainNorm { domain = PersonalityDomain.C, averageMin = 30, averageMax = 70 },
        new DomainNorm { domain = PersonalityDomain.E, averageMin = 30, averageMax = 70 },
        new DomainNorm { domain = PersonalityDomain.A, averageMin = 30, averageMax = 70 },
        new DomainNorm { domain = PersonalityDomain.N, averageMin = 30, averageMax = 70 }
    };

    [Header("効果音")]
    public AudioClip audioClip_open;

    // 定数
    private const int HighScoreThreshold = 70;//傾向が高いとするしきい値
    private const int LowScoreThreshold = 30;//傾向が低いとするしきい値

    private bool isAllPanelActive = true;//どのパネルを開いているか


    [Header("CSVに結果を書き込むかどうか")]
    public bool isCSVWrite = false;
    private bool isWrite = false;

    void Start()
    {
        // 1. 表示開始ボタン
        if (showButton != null)
            showButton.onClick.AddListener(ShowScore);

        // 2. パネル切替ボタン
        if (toggleViewButton != null)
            toggleViewButton.onClick.AddListener(ToggleView);

        // 3. 各ドメイン選択ボタンの登録
        foreach (var setting in domainSettings)
        {
            if (setting.selectButton != null)
            {
                setting.selectButton.onClick.AddListener(() =>
                {
                    UpdateDomainPanelInfo(setting);
                    SwitchToDomainView();
                });
            }
        }

        if (scoreCanvasRoot != null) scoreCanvasRoot.SetActive(false);
    }

    //パネル表示
    public void ShowScore()
    {
        if (scoreCanvasRoot == null) return;

        AudioManager.Instance.PlaySound(audioClip_open, 1f);

        showButton.gameObject.SetActive(false);
        scoreCanvasRoot.SetActive(true);
        scoreCanvasRoot.transform.localScale = Vector3.zero;
        scoreCanvasRoot.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        ShowAllPanel();
        SwitchAllView();

        if (isCSVWrite && !isWrite)
        {
            PersonalityManager.Instance.ResultFile();
            isWrite = true;
        }
    }

    //チャートパネルとドメインパネル切り替え
    private void ToggleView()
    {
        if (isAllPanelActive) SwitchToDomainView();
        else SwitchAllView();
    }

    private void SwitchAllView()
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundClick, 1f);
        isAllPanelActive = true;
        if (allPanel != null) allPanel.SetActive(true);
        if (domainInfoPanel != null) domainInfoPanel.SetActive(false);
        //表示中のパネルをわかりやすくする
        for (int i = 0; i < domainSettings.Count; i++)
        {
            domainSettings[i].choicedPanel.SetActive(false);
        }
        choicedPanel_all.SetActive(true);
    }

    private void SwitchToDomainView()
    {
        AudioManager.Instance.PlaySound(AudioManager.Instance.soundClick, 1f);
        isAllPanelActive = false;
        if (allPanel != null) allPanel.SetActive(false);
        if (domainInfoPanel != null) domainInfoPanel.SetActive(true);
    }

    /// <summary>
    /// ドメイン詳細パネルのテキスト内容を更新する
    /// </summary>
    private void UpdateDomainPanelInfo(DomainUISetting setting)
    {
        // 1. ドメインスコア計算 (PersonalityManager利用)
        int domainScore = PersonalityManager.Instance.CalculateDomainScore(setting.domain);

        // 2. ドメイン名と全体スコアの更新
        if (domainName != null) domainName.text = setting.displayName;
        if (sharedPercentText != null) sharedPercentText.text = $"{domainScore}%";
        if (sharedExplanationText != null) sharedExplanationText.text = GetExplanationText(setting.domain, domainScore);

        // 3. ファセット（下位次元）情報の更新
        List<PersonalityFacet> targetFacets = GetFacetsByDomain(setting.domain);

        if (sharedFacetTexts != null)
        {
            for (int i = 0; i < sharedFacetTexts.Count; i++)
            {
                if (i < targetFacets.Count)
                {
                    // 表示するファセットがある場合
                    PersonalityFacet facet = targetFacets[i];
                    int facetScore = CalculateFacetScore(facet);
                    string facetName = GetFacetDisplayName(facet); // 日本語名取得

                    sharedFacetTexts[i].gameObject.SetActive(true);

                    // スコアが(未測定)の場合
                    if (facetScore == -1)
                    {
                        sharedFacetTexts[i].text = $"{facetName}\n未測定";
                    }
                    else
                    {
                        sharedFacetTexts[i].text = $"{facetName}\n{facetScore}%";
                    }
                }
                else
                {
                    // 枠が余っている場合は非表示にする
                    sharedFacetTexts[i].gameObject.SetActive(false);
                }
            }
        }

        //表示中のパネルをわかりやすくする
        for (int i = 0; i < domainSettings.Count; i++)
        {
            domainSettings[i].choicedPanel.SetActive(false);
        }
        choicedPanel_all.SetActive(false);
        setting.choicedPanel.SetActive(true);
    }

    //各ドメインのスコア得点率を一つのパネルで表示
    private void ShowAllPanel()
    {
        if (allPanel == null) return;
        allPanel.SetActive(true);

        //ドメインに従って値を取得
        scoringRateText_O.text = $"スコア率{PersonalityManager.Instance.CalculateDomainScore(PersonalityDomain.O)}%";
        scoringRateText_C.text = $"スコア率{PersonalityManager.Instance.CalculateDomainScore(PersonalityDomain.C)}%";
        scoringRateText_E.text = $"スコア率{PersonalityManager.Instance.CalculateDomainScore(PersonalityDomain.E)}%";
        scoringRateText_A.text = $"スコア率{PersonalityManager.Instance.CalculateDomainScore(PersonalityDomain.A)}%";
        scoringRateText_N.text = $"スコア率{PersonalityManager.Instance.CalculateDomainScore(PersonalityDomain.N)}%";
    }

    /// <summary>
    /// ドメインとスコア(0-100)を受け取り、傾向テキストとレベルを返す
    /// </summary>
    private string GetExplanationText(PersonalityDomain domain, int rawScore)
    {
        // そのドメインの基準を取得
        var norm = defaultNorms.Find(n => n.domain == domain);

        string level = "平均的";
        string comparisonText = "";

        if (rawScore > norm.averageMax)
        {
            level = "高い";
            comparisonText = "平均よりも強く特性が現れていると考えられます。";
        }
        else if (rawScore < norm.averageMin)
        {
            level = "低い";
            comparisonText = "平均よりも特性は控えめと考えられます。";
        }
        else
        {
            level = "平均的";
            comparisonText = "多くの人と同程度のスコア傾向と考えられます。";
        }

        // ドメイン名の取得
        string jpDomain = GetDomainDisplayName(domain);

        // ファセットごとの詳細な概要文章を生成
        string facetSummary = GetFacetSummary(domain);

        // 表示文の構成
        return $"{jpDomain}の傾向：【{level}】\n" +
               $"スコア率: {rawScore}%\n\n" +
               $"{comparisonText}\n\n" +
               $"【詳細分析】\n{facetSummary}\n" +
               $"（上記のような傾向があります）";
    }

    /// <summary>
    /// 指定ドメイン内の全ファセットをチェックし、特徴的な項目（高・低）の文章を生成する
    /// </summary>
    private string GetFacetSummary(PersonalityDomain domain)
    {
        List<PersonalityFacet> facets = GetFacetsByDomain(domain);
        StringBuilder sb = new StringBuilder();
        bool hasFeature = false;

        foreach (var facet in facets)
        {
            int score = CalculateFacetScore(facet);
            string description = "";

            // 未測定(-1)の場合は集計から除外してスキップ
            if (score == -1) continue;

            // 70点より上なら「高い」特徴、30点未満なら「低い」特徴を表示
            // ※定数 HighScoreThreshold, LowScoreThreshold を使用
            if (score > HighScoreThreshold)
            {
                description = GetFacetDescriptionText(facet, true); // true = High
                if (!string.IsNullOrEmpty(description))
                {
                    sb.AppendLine($"・{description}");
                    hasFeature = true;
                }
            }
            else if (score < LowScoreThreshold)
            {
                description = GetFacetDescriptionText(facet, false); // false = Low
                if (!string.IsNullOrEmpty(description))
                {
                    sb.AppendLine($"・{description}");
                    hasFeature = true;
                }
            }
            // 平均的な場合（30~70）は、記述が長くなりすぎるため今回は省略しますが、
            // 必要であれば else ブロックで「○○は平均的です」と追加も可能です。
        }

        if (!hasFeature)
        {
            return "・特筆すべき偏りはなく、全体的にバランスが取れています。";
        }

        return sb.ToString();
    }

    /// <summary>
    /// ファセットごとの「高い場合」「低い場合」の説明文定義
    /// 引用元：『日本版NEO-PI-Rの作成とその因子的妥当性の検討』Appendix
    /// </summary>
    private string GetFacetDescriptionText(PersonalityFacet facet, bool isHigh)
    {
        // isHighがtrueなら高得点時のテキスト、falseなら低得点時のテキストを返す
        // 文末に（ファセット名：高/低）を付記して、どちらの判定か分かりやすくしています。

        switch (facet)
        {
            // --- 神経症傾向 (Neuroticism) ---
            case PersonalityFacet.N1_Anxiety:
                return isHigh ? "緊張、恐れ、心配、懸念を感じやすい(不安：高）"
                              : "最悪の事態をあまり恐れない(不安：低）";
            case PersonalityFacet.N2_Anger:
                return isHigh ? "短気で、怒りやフラストレーションを持ちやすい（敵意：高）"
                              : "友好的で落ち着いており、なかなか攻撃的な態度を取らない（敵意：低）";
            case PersonalityFacet.N3_Depression:
                return isHigh ? "失望、罪悪感、意気消沈、ゆううつを感じやすい（抑うつ：高）"
                              : "めったに悲しまず、希望に満ちて自信を持っている（抑うつ：低）";
            case PersonalityFacet.N4_SelfConsciousness:
                return isHigh ? "恥ずかしがりで劣等感を持ちやすく、すぐに当惑する（自意識：高）"
                              : "安定して確信があり、自足したゆとりのある態度を持つ（自意識：低）";
            case PersonalityFacet.N5_Immoderation:
                return isHigh ? "やりたいと思ったら止まらず、衝動に負けやすい（衝動性：高）"
                              : "誘惑に抗することができ、自己コントロールが可能である（衝動性：低）";
            case PersonalityFacet.N6_Vulnerability:
                return isHigh ? "すぐ混乱してパニックになりやすく、ストレスを処理できない（傷つきやすさ：高）"
                              : "回復力があり、苦痛に耐えられ、冷静で有能に対処できます（傷つきやすさ：低）";

            // --- 外向性 (Extraversion) ---
            case PersonalityFacet.E1_Friendliness:
                return isHigh ? "社交的でおしゃべり、愛情深い（温かさ：高）"
                              : "冷たく、人と距離を持ち、形式的な態度をとる（温かさ：低）";
            case PersonalityFacet.E2_Gregariousness:
                return isHigh ? "宴会好きで多くの友達を持ち、人との接触を求める（群居性：高）"
                              : "人混みを避け、一人でいることや孤独を好む、無口である（群居性：低）";
            case PersonalityFacet.E3_Assertiveness:
                return isHigh ? "支配的で力強く、自信家で確信に満ちている（断行性：高）"
                              : "でしゃばらず控えめで、遠慮なく話すのを避ける（断行性：低）";
            case PersonalityFacet.E4_ActivityLevel:
                return isHigh ? "エネルギッシュでペースが速く、精力的である（活動性：高）"
                              : "あわてず、ゆっくりと慎重に行動する（活動性：低）";
            case PersonalityFacet.E5_ExcitementSeeking:
                return isHigh ? "一時的で強い刺激を求め、危険を冒すことを好す（刺激希求性：高）"
                              : "過剰な刺激を避け、注意深く落ち着いており、スリルを求めない（刺激希求性：低）";
            case PersonalityFacet.E6_Cheerfulness:
                return isHigh ? "元気で気概があり、喜びにあふれている（よい感情：高）"
                              : "情熱的ではなく、穏やかでまじめである（よい感情：低）";

            // --- 開放性 (Openness to experience) ---
            case PersonalityFacet.O1_Imagination:
                return isHigh ? "想像力があり、夢想を楽しみ、空想をふくらませる（空想：高）"
                              : "現実的思考を好み、実際的で、夢想を避ける（空想：低）";
            case PersonalityFacet.O2_ArtisticInterests:
                return isHigh ? "審美的経験に価値を置き、芸術や美に感動する（審美性：高）"
                              : "実用的な価値や事実を重視する（審美性：低）";
            case PersonalityFacet.O3_Emotionality:
                return isHigh ? "感情的反応や共感を示し、自分の感情に価値を置く（感情：高）"
                              : "感情の幅が狭く、周囲に鈍感である（感情：低）";
            case PersonalityFacet.O4_Adventurousness:
                return isHigh ? "新奇なものを好み、様々なものを求め、新しい活動を試す（行為：高）"
                              : "慣れたものを好み、決まりきったやり方や決まった道を好む（行為：低）";
            case PersonalityFacet.O5_Intellect:
                return isHigh ? "知的好奇心があり、理論的志向で分析的である（アイデア：高）"
                              : "実際的・事実志向で、知的挑戦を楽しまない（アイデア：低）";
            case PersonalityFacet.O6_Liberalism:
                return isHigh ? "心が広く寛容で、偏見がなく、決まりにとらわれない（価値：高）"
                              : "教条的で決まりに従い、保守的で心が狭い（価値：低）";

            // --- 調和性 (Agreeableness) ---
            case PersonalityFacet.A1_Trust:
                return isHigh ? "人を信じ、他者に対して善意を持っている（信頼：高）"
                              : "皮肉屋で懐疑的、人を信じない（信頼：低）";
            case PersonalityFacet.A2_Morality:
                return isHigh ? "実直で誠実、無邪気な傾向がある（実直さ：高）"
                              : "目的達成のために戦略的な振る舞いをする(実直さ：低）";
            case PersonalityFacet.A3_Altruism:
                return isHigh ? "人のためになろうとする利他的な態度を持つ（利他性：高）"
                              : "自己中心的で、人の問題に関わるのを好まない（利他性：低）";
            case PersonalityFacet.A4_Cooperation:
                return isHigh ? "人に譲り、攻撃をおさえ、人を許し忘れることができる（応諾：高）"
                              : "攻撃的で競争を好み、敵意を露にするのをためらわない（応諾：低）";
            case PersonalityFacet.A5_Modesty:
                return isHigh ? "謙遜し、自分を前に出さない慎み深さがある（慎み深さ：高）"
                              : "自分が優れていると思い、あつかましい態度をとることがある（慎み深さ：低）";
            case PersonalityFacet.A6_Sympathy:
                return isHigh ? "人の必要に動かされ、社会政策の側で人に同情する（優しさ：高）"
                              : "論理的で客観的な判断を優先する（優しさ：低）";

            // --- 誠実性 (Conscientiousness) ---
            case PersonalityFacet.C1_SelfEfficacy:
                return isHigh ? "人生上の問題にうまく対処できると考えているコンピテンス：高）"
                              : "自分の能力が低いと感じ、準備不足だと思う（コンピテンス：低）";
            case PersonalityFacet.C2_Orderliness:
                return isHigh ? "きちんとしており、気構えができ、物を整理する（秩序：高）"
                              : "気構えができておらず、順序立てて物事ができない（秩序：低）";
            case PersonalityFacet.C3_Dutifulness:
                return isHigh ? "倫理的原則に従い、道徳的義務に忠実に従う（良心性：高）"
                              : "物事にいいかげんで、頼りにならず信頼できない（良心性：低）";
            case PersonalityFacet.C4_AchievementStriving:
                return isHigh ? "要求水準が高く、目標達成のために頑張る（達成追求：高）"
                              : "いい加減で怠け者、野心や目的を持たない（達成追求：低）";
            case PersonalityFacet.C5_SelfDiscipline:
                return isHigh ? "飽きずに継続し、やり終えるよう自分を動機づける能力がある（自己鍛錬：高）"
                              : "つまらないことに時間を費やし、すぐにがっかりして止めたがる（自己鍛錬：低）";
            case PersonalityFacet.C6_Cautiousness:
                return isHigh ? "慎重で、よく考えてから行動する熟考型である（慎重さ：高）"
                              : "あわて者で、結果を考えずに話したり行動する（慎重さ：低）";

            default:
                return "";
        }
    }

    /// <summary>
    /// 特定のファセットのスコア(0-100)を計算して返す
    /// </summary>
    private int CalculateFacetScore(PersonalityFacet facet)
    {
        var pm = PersonalityManager.Instance;
        int current = 0;
        int max = 0;

        if (pm.facetScores.ContainsKey(facet)) current = pm.facetScores[facet];
        if (pm.maxFacetScores.ContainsKey(facet)) max = pm.maxFacetScores[facet];

        if (max > 0)//ファセット測定タスク実行しているなら
        {
            return Mathf.RoundToInt((float)current / max * 100f);
        }
        else
        {
            return -1;
        }
    }

    /// <summary>
    /// ドメイン（O, C...）に対応するファセットリストを自動取得する
    /// PersonalityManagerの命名規則（O1_..., C1_...）を利用
    /// </summary>
    private List<PersonalityFacet> GetFacetsByDomain(PersonalityDomain domain)
    {
        // Enum名を文字列にして、ドメイン名で始まるものをフィルタリング (例: "O" で始まる "O1_Imagination" など)
        return Enum.GetValues(typeof(PersonalityFacet))
                   .Cast<PersonalityFacet>()
                   .Where(f => f.ToString().StartsWith(domain.ToString()))
                   .ToList();
    }

    /// <summary>
    /// ドメインの日本語名取得
    /// </summary>
    private string GetDomainDisplayName(PersonalityDomain domain)
    {
        switch (domain)
        {
            case PersonalityDomain.O: return "開放性";
            case PersonalityDomain.C: return "誠実性";
            case PersonalityDomain.E: return "外向性";
            case PersonalityDomain.A: return "調和性";
            case PersonalityDomain.N: return "神経症傾向";
            default: return domain.ToString();
        }
    }

    /// <summary>
    /// ファセットの日本語表示名を取得する
    /// 引用元：『日本版NEO-PI-Rの作成とその因子的妥当性の検討』Appendix
    /// </summary>
    private string GetFacetDisplayName(PersonalityFacet facet)
    {
        switch (facet)
        {
            // 開放性 (O)
            case PersonalityFacet.O1_Imagination: return "空想";
            case PersonalityFacet.O2_ArtisticInterests: return "審美性";
            case PersonalityFacet.O3_Emotionality: return "感情";
            case PersonalityFacet.O4_Adventurousness: return "行為";
            case PersonalityFacet.O5_Intellect: return "アイデア";
            case PersonalityFacet.O6_Liberalism: return "価値";

            // 誠実性 (C)
            case PersonalityFacet.C1_SelfEfficacy: return "コンピテンス";
            case PersonalityFacet.C2_Orderliness: return "秩序";
            case PersonalityFacet.C3_Dutifulness: return "良心性";
            case PersonalityFacet.C4_AchievementStriving: return "達成追求";
            case PersonalityFacet.C5_SelfDiscipline: return "自己鍛錬";
            case PersonalityFacet.C6_Cautiousness: return "慎重さ";

            // 外向性 (E)
            case PersonalityFacet.E1_Friendliness: return "温かさ";
            case PersonalityFacet.E2_Gregariousness: return "群居性";
            case PersonalityFacet.E3_Assertiveness: return "断行性";
            case PersonalityFacet.E4_ActivityLevel: return "活動性";
            case PersonalityFacet.E5_ExcitementSeeking: return "刺激希求性";
            case PersonalityFacet.E6_Cheerfulness: return "よい感情";

            // 調和性 (A)
            case PersonalityFacet.A1_Trust: return "信頼";
            case PersonalityFacet.A2_Morality: return "実直さ";
            case PersonalityFacet.A3_Altruism: return "利他性";
            case PersonalityFacet.A4_Cooperation: return "応諾";
            case PersonalityFacet.A5_Modesty: return "慎み深さ";
            case PersonalityFacet.A6_Sympathy: return "優しさ";

            // 神経症傾向 (N)
            case PersonalityFacet.N1_Anxiety: return "不安";
            case PersonalityFacet.N2_Anger: return "敵意";
            case PersonalityFacet.N3_Depression: return "抑うつ";
            case PersonalityFacet.N4_SelfConsciousness: return "自意識";
            case PersonalityFacet.N5_Immoderation: return "衝動性";
            case PersonalityFacet.N6_Vulnerability: return "傷つきやすさ";

            default: return facet.ToString();
        }
    }
}