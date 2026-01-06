using UnityEngine;

/// <summary>
/// 効果音とBGMを管理するシングルトンクラス。
/// シーンをまたいで存在し続けます。
/// </summary>
public class AudioManager : MonoBehaviour
{
    // シングルトンのための静的インスタンス
    public static AudioManager Instance { get; private set; }

    // Inspectorから設定するオーディオクリップ
    [Header("効果音")]
    public AudioClip soundMiss;
    public AudioClip soundInspectable, soundCancel;
    public AudioClip soundStoryMessage; // テキスト表示音
    public AudioClip soundInventory;//インベントリ表示時、終了時
    public AudioClip soundClick;
    public AudioClip soundWalk;//移動音
    public AudioClip soundWeapon;//武器装備音
    public AudioClip soundDrag;//8_Task用
    public AudioClip soundGet;//ゲットしたときの音(例：蝶)
    public AudioClip soundPurchase;//アイテム購入時
    public AudioClip sound3DButton;//3Dボタンを押した

    [Header("効果音の音量")]
    // 音を再生するためのAudioSource
    public float Normal = 1.0f;
    public float Half = 0.5f;
    public float Mini = 0.2f;

    // 音を再生するためのAudioSource
    private AudioSource sfxSource; // 通常の効果音用
    private AudioSource loopSource; // ループ再生用
    private AudioSource loopSourceSub; // ループ再生用サブ
    private AudioSource walkSource;//移動音（足音）再生用

    void Awake()
    {
        // シングルトンパターンの実装
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            // AudioSourceコンポーネントを初期化
            InitializeAudioSources();
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        // 通常の効果音用AudioSourceを追加
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        // ループ再生用AudioSourceを追加
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;

        // 移動音用AudioSourceを追加
        walkSource = gameObject.AddComponent<AudioSource>();
        walkSource.playOnAwake = false;
    }

    /// <summary>
    /// 指定された効果音を一度だけ再生します。
    /// </summary>
    public void PlaySound(AudioClip clip, float soundVolume)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, soundVolume);
        }
        else
        {
            Debug.Log("AudioClipがアタッチされていません");
        }
    }

    /// <summary>
    /// 指定された効果音のループ再生を開始します。
    /// </summary>
    public void PlayLoopingSound(AudioClip clip)
    {
        if (clip != null && !loopSource.isPlaying)
        {
            loopSource.clip = clip;
            loopSource.loop = true;
            loopSource.Play();
        }
    }

    /// <summary>
    /// ループ再生中の効果音を停止します。
    /// </summary>
    public void StopLoopingSound()
    {
        if (loopSource.isPlaying)
        {
            loopSource.Stop();
        }
    }

    /// <summary>
    /// 指定された効果音のループ再生を開始します。
    /// </summary>
    public void PlayLoopingSoundSub(AudioClip clip)
    {
        if (loopSourceSub == null)
        {
            // ループ再生用AudioSourceを追加
            loopSourceSub = gameObject.AddComponent<AudioSource>();
            loopSourceSub.playOnAwake = false;
        }

        if (clip != null && !loopSourceSub.isPlaying)
        {
            loopSourceSub.clip = clip;
            loopSourceSub.loop = true;
            loopSourceSub.Play();
        }
    }

    /// <summary>
    /// ループ再生中の効果音を停止します。
    /// </summary>
    public void StopLoopingSoundSub()
    {
        if (loopSourceSub != null && loopSourceSub.isPlaying)
        {
            loopSourceSub.Stop();
        }
    }

    /// <summary>
    /// 移動音（足音）効果音のループ再生を開始します。
    /// </summary>
    public void PlayLoopingWalkSound(AudioClip clip)
    {
        if (clip != null && !walkSource.isPlaying)
        {
            walkSource.clip = clip;
            walkSource.loop = true;
            walkSource.Play();
        }
    }

    /// <summary>
    /// ループ再生中の効果音を停止します。
    /// </summary>
    public void StopLoopingWalkSound()
    {
        if (walkSource.isPlaying)
        {
            walkSource.Stop();
        }
    }
}

