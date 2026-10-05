using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public enum SoundType
{
    Button,     // UI音效
    Bubblem,    // 泡泡音效
}
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }

    [Header("音频源")]
    public AudioSource musicSource;      // 播放背景音乐
    public AudioSource sfxSource;        // 播放音效
    [Header("音量设置")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("music")]
    public AudioClip mainTheme;

    [Header("sfx")]
    private Dictionary<AudioClip, int> playingCount = new Dictionary<AudioClip, int>();
    [SerializeField] private int maxPerClip = 5;
    public AudioClip buttonClip;


    private AudioClip myClip;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 播放背景音乐（如果传入新音乐会自动切换）
    public void PlayMusic(AudioClip musicClip)
    {
        AudioClip clip = musicClip;
        if (clip == null)
        {
            clip = mainTheme;
        }
        
        if (musicSource.clip == clip && musicSource.isPlaying)
            return; // 同一首曲子正在播放则不重复播放
        if (!musicSource.enabled)
        {
            musicSource.enabled = true;
        }
        musicSource.clip = clip;
        musicSource.Play();
    }

    // 停止背景音乐
    public void StopMusic()
    {
        musicSource.Stop();
    }

    private AudioClip GetClipByType(SoundType soundType)
    {
        AudioClip clip = null;
        switch (soundType)
        {
            case SoundType.Button:
                clip = buttonClip;
                break;
            default:
                Debug.LogWarning($"未找到类型为 {soundType} 的音效！");
                break;
        }
        return clip;
    }

    //多个sfx可同时播放
    public void PlaySFX(SoundType soundType)
    {
        myClip = GetClipByType(soundType);
        if (myClip != null)
        {
            PlayWithCap(myClip);
        }
    }

    //限制同时播放的音效数量，放置声音过大
    public void PlayWithCap(AudioClip clip, float volume = 1f)
    {
        playingCount.TryGetValue(clip, out int current);
        
        if (current >= maxPerClip) return; // 同类音效播放太多，忽略本次
        
        playingCount[clip] = current + 1;
        //sfxSource.PlayOneShot(clip, volume);

        //根据同时播放数缩小当前音量
        sfxSource.PlayOneShot(clip, volume*Mathf.Pow(0.7f, playingCount[clip]));
        
        StartCoroutine(DecrementCount(clip, clip.length));
    }
    
    private IEnumerator DecrementCount(AudioClip clip, float delay)
    {
        //不受timescale影响
        yield return new WaitForSecondsRealtime(delay);
        playingCount[clip]--;
    }

    public void StopSFX()
    {
       // 停止语音播放
        if (sfxSource.isPlaying)
            sfxSource.Stop();
    }

}
