using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>常驻播放器：场景/地点换曲，卧室按日期选曲。</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }

    public AudioSource musicSource;
    public AudioSource sfxSource;
    [Header("场景 / 剧情音乐")]
    [SerializeField] private AudioClip bgm_bedroom1;
    [SerializeField] private AudioClip bgm_bedroom2;
    [SerializeField] private AudioClip bgm_sunset;
    [SerializeField] private AudioClip bgm_park;
    [SerializeField] private AudioClip bgm_meteor;
    [SerializeField] private AudioClip sound_hallway;
    [Header("场景音效")]
    [SerializeField] private AudioClip sound_mall;
    [SerializeField] private AudioClip sound_traffic;
    [SerializeField] private AudioClip sound_truck;
    [Header("交互音效")]
    [SerializeField] private AudioClip effect_bubble_select;
    [SerializeField] private AudioClip effect_bubble_cancel;
    [SerializeField] private AudioClip effect_emergence;
    [SerializeField] private AudioClip effect_button;
    [SerializeField] private AudioClip effect_error;
    [SerializeField] private AudioClip effect_interact;
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
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

    private void PlaySceneMusic()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "BedRoomScene":
                int day = DateController.instance != null ? DateController.instance.currentDate : 0;
                PlayMusic(day >= 5 ? bgm_bedroom2 : bgm_bedroom1);
                break;
            case "MapScene": PlayMusic(bgm_sunset); break;
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
        AudioClip clip = location == "公园" ? bgm_park : location == "街道" ? bgm_meteor : null;
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
            case "sound_hallway": PlayMusic(sound_hallway); break;
            default: Debug.LogWarning($"未配置的音乐 ID：{id}", this); break;
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip != null && musicSource.clip == clip && musicSource.isPlaying) return;
        StopMusic();
        if (clip == null) return;
        musicSource.clip = clip;
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
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    public void PlaySFX(string id)
    {
        Debug.Log($"Playing SFX with ID: {id}", this);
        switch (id)
        {
            case "sound_mall": PlaySFX(sound_mall); break;
            case "sound_traffic": PlaySFX(sound_traffic); break;
            case "sound_truck": PlaySFX(sound_truck); break;
            case "effect_bubble_select": PlaySFX(effect_bubble_select); break;
            case "effect_bubble_cancel": PlaySFX(effect_bubble_cancel); break;
            case "effect_emergence": PlaySFX(effect_emergence); break;
            case "effect_button": PlaySFX(effect_button); break;
            case "effect_error": PlaySFX(effect_error); break;
            case "effect_interact": PlaySFX(effect_interact); break;
            default: Debug.LogWarning($"未配置的音效 ID：{id}", this); break;
        }
    }

    public void StopSFX()
    {
        if (sfxSource != null) sfxSource.Stop();
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
        musicSource.volume = SourceVolume(musicVolume, musicMultiplier);
        sfxSource.volume = SourceVolume(sfxVolume, sfxMultiplier);
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
