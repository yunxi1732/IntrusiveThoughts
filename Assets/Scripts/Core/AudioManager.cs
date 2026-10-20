using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>常驻播放器：场景/地点换曲，卧室按日期选曲。</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }

    public AudioSource musicSource;
    public AudioSource sfxSource;
    private AudioSource dialogueSfxSource;
    public bool IsDialogueSfxPlaying => dialogueSfxSource != null && dialogueSfxSource.isPlaying;
    [Header("场景 / 剧情音乐")]
    [SerializeField] private AudioClip bgm_bedroom1;
    [SerializeField] private AudioClip bgm_bedroom2;
    [SerializeField] private AudioClip bgm_sunset;
    [SerializeField] private AudioClip bgm_park;
    [SerializeField] private AudioClip bgm_meteor;
    [SerializeField] private AudioClip bgm_transparentsea;
    [SerializeField] private AudioClip sound_hallway;
    [Header("场景音效")]
    [SerializeField] private AudioClip sound_mall;
    [SerializeField] private AudioClip sound_traffic;
    [SerializeField] private AudioClip sound_truck;
    [Header("交互音效")]
    [SerializeField] private AudioClip effect_bubble_select;
    [SerializeField] private AudioClip effect_bubble_cancel;
    [SerializeField] private AudioClip effect_emergence;
    [Header("音效数组")]
    [SerializeField] private AudioClip[] clickSfx = new AudioClip[7];
    private readonly System.Collections.Generic.Dictionary<string, AudioClip> dialogueClips = new();
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    [Header("输出增益（1 为原始音量）")]
    public bool enableOutputGain = false;
    [Range(1f, 4f)] public float outputGain = 2f;
    [Header("编辑器音频诊断")]
    public bool logAudioPlayback = false;
    private float musicMultiplier = 20f;
    private float sfxMultiplier = 20f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null || sfxSource == musicSource) sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        dialogueSfxSource = gameObject.AddComponent<AudioSource>();
        dialogueSfxSource.playOnAwake = false;
        dialogueSfxSource.loop = false;
        // 提前加载短音效，播放时只读取缓存。
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Audios/sfx"))
        {
            dialogueClips[clip.name] = clip;
            clip.LoadAudioData();
        }
        foreach (AudioClip clip in clickSfx)
            if (clip != null) clip.LoadAudioData();
        ApplyVolumes();
    }

    private void OnEnable()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventManager1P<string>.StartListening(GameEvents.AudioLocationChanged, PlayLocationMusic);
        EventManager1P<int>.StartListening(GameEvents.DayChanged, OnDayChanged);
    }

    private void Start()
    {
        if (SaveManager.instance != null)
        {
            SetVolume("bgm", SaveManager.instance.GetVolumePreference("bgm"));
            SetVolume("sfx", SaveManager.instance.GetVolumePreference("sfx"));
        }
        PlaySceneMusic();
    }

    private void OnDisable()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventManager1P<string>.StopListening(GameEvents.AudioLocationChanged, PlayLocationMusic);
        EventManager1P<int>.StopListening(GameEvents.DayChanged, OnDayChanged);
        StopMusic();
        StopSFX();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        StopMusic();
        StopSFX();
        PlaySceneMusic();
    }

    private void ConfigureOutputGain()
    {
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener == null) return;
        AudioOutputGain gain = listener.GetComponent<AudioOutputGain>();
        if (!enableOutputGain)
        {
            if (gain != null) gain.enabled = false;
            return;
        }
        if (gain == null) gain = listener.gameObject.AddComponent<AudioOutputGain>();
        gain.enabled = true;
        gain.Gain = outputGain;
    }

    private void OnValidate()
    {
        if (Application.isPlaying && instance == this) ConfigureOutputGain();
    }

    private void PlaySceneMusic()
    {
        ConfigureOutputGain();
        if (StoryManager.instance != null && StoryManager.instance.IsOpening)
        {
            StopMusic();
            return;
        }
        switch (SceneManager.GetActiveScene().name)
        {
            case "MainScene": PlayMusic(bgm_sunset); break;
            case "BedRoomScene":
                int day = DateController.instance != null ? DateController.instance.currentDate : 0;
                PlayMusic(day >= 5 ? bgm_bedroom2 : bgm_bedroom1);
                break;
            case "MapScene": PlayMusic(bgm_sunset); break;
            case "EndScene": PlayMusic(bgm_transparentsea); break;
            case "LocationScene":
                PlayLocationMusic(StoryPlayer.instance != null ? StoryPlayer.instance.currentLocation : null);
                break;
            default: StopMusic(); break;
        }
    }

    private void OnDayChanged(int day)
    {
        if (SceneManager.GetActiveScene().name == "BedRoomScene") PlaySceneMusic();
    }

    public void PlayLocationMusic(string location)
    {
        if (SceneManager.GetActiveScene().name != "LocationScene") return;
        AudioClip clip = location switch
        {
            "公园" => bgm_park,
            "商圈" => sound_mall,
            "街道" => sound_traffic,
            "楼道" => sound_hallway,
            _ => null,
        };
        // 相同地点的初始化通知不重启当前曲目；换地点时 PlayMusic 会停止旧曲。
        PlayMusic(clip);
    }

    public void PlayBGM(string id)
    {
        switch (id)
        {
            case "bgm_bedroom1": PlayMusic(bgm_bedroom1); break;
            case "bgm_bedroom2": PlayMusic(bgm_bedroom2); break;
            case "bgm_sunset": PlayMusic(bgm_sunset); break;
            case "bgm_park": PlayMusic(bgm_park); break;
            case "bgm_meteor": PlayMusic(bgm_meteor); break;
            case "bgm_transparentsea": PlayMusic(bgm_transparentsea); break;
            case "sound_hallway": PlayMusic(sound_hallway); break;
            default: Debug.LogWarning($"未配置的音乐 ID：{id}", this); break;
        }
    }

    // 台词 action 使用资源文件名，不依赖 Inspector 中的固定曲目映射。
    public void PlayResourceBGM(string resourceName)
    {
        StopMusic();
        if (string.IsNullOrWhiteSpace(resourceName)) return;
        var clip = Resources.Load<AudioClip>("Audios/" + resourceName.Trim());
        if (clip != null) PlayMusic(clip);
    }

    public void RestoreSceneBGM()
    {
        StopMusic();
        PlaySceneMusic();
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip != null && musicSource.clip == clip && musicSource.isPlaying) return;
        StopMusic();
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        TracePlayback("BGM", clip, musicSource);
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.clip = null;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        TracePlayback("交互音效", clip, sfxSource);
        sfxSource.PlayOneShot(clip);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void TracePlayback(string channel, AudioClip clip, AudioSource source)
    {
        if (!logAudioPlayback) return;
        Debug.Log($"[AudioPlayback] {channel} clip={clip.name} frame={Time.frameCount} "
            + $"time={Time.realtimeSinceStartup:F3} source={source.GetInstanceID()} "
            + $"scene={SceneManager.GetActiveScene().name}\n"
            + new System.Diagnostics.StackTrace(1, true), this);
    }

    public System.Collections.IEnumerator PlayOpeningTruck(System.Action<AudioSource> onStarted = null)
    {
        if (sound_truck == null)
        {
            Debug.LogWarning("开场卡车音效未配置，直接进入游戏。", this);
            yield break;
        }
        sfxSource.Stop();
        sfxSource.loop = false;
        sfxSource.clip = sound_truck;
        TracePlayback("开场音效", sound_truck, sfxSource);
        sfxSource.Play();
        onStarted?.Invoke(sfxSource);
        while (sfxSource.isPlaying) yield return null;
        sfxSource.clip = null;
    }

    public void PlaySFX(int index)
    {
        if (index < 0 || index >= clickSfx.Length)
        {
            Debug.LogWarning($"点击音效索引超出范围：{index}", this);
            return;
        }
        PlaySFX(clickSfx[index]);
    }

    public void PlayEmergenceSFX() => PlaySFX(effect_emergence);

    public void PlayDialogueSFX(string id)
    {
        if (!dialogueClips.TryGetValue(id, out AudioClip clip))
        {
            Debug.LogWarning($"找不到台词音效资源：Audios/sfx/{id}", this);
            return;
        }
        // 独立通道，按钮点击等交互声音不影响台词音效的跳过锁定。
        TracePlayback("台词音效", clip, dialogueSfxSource);
        dialogueSfxSource.PlayOneShot(clip);
    }

    public void StopSFX()
    {
        if (sfxSource != null) sfxSource.Stop();
        if (dialogueSfxSource != null) dialogueSfxSource.Stop();
    }

    // 保留已有滑条同步：滑条数值转分贝，再转 AudioSource 的线性增益。
    public static float VolumeToDecibels(float value, float multiplier = 20f)
    {
        value = Mathf.Clamp01(value);
        return value <= 0f ? -80f : Mathf.Max(-80f, Mathf.Log10(value) * multiplier);
    }

    private float SourceVolume(float value, float multiplier)
    {
        return value <= 0f ? 0f : Mathf.Clamp01(masterVolume) * Mathf.Pow(10f, VolumeToDecibels(value, multiplier) / 20f);
    }

    public void ApplyVolumes()
    {
        ConfigureOutputGain();
        musicSource.volume = SourceVolume(musicVolume, musicMultiplier);
        sfxSource.volume = SourceVolume(sfxVolume, sfxMultiplier);
        if (dialogueSfxSource != null) dialogueSfxSource.volume = SourceVolume(sfxVolume, sfxMultiplier);
    }

    public bool SetVolume(string channel, float value, float multiplier = 20f)
    {
        value = Mathf.Clamp01(value);
        multiplier = multiplier > 0f ? multiplier : 20f;
        if (channel == "bgm") { musicVolume = value; musicMultiplier = multiplier; }
        else if (channel == "sfx") { sfxVolume = value; sfxMultiplier = multiplier; }
        else return false;
        ApplyVolumes();
        EventManager2P<string, float>.TriggerEvent(GameEvents.AudioVolumeChanged, channel, value);
        return true;
    }
}
