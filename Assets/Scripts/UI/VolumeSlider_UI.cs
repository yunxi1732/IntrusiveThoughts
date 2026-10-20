
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSlider_UI : MonoBehaviour
{
    public Slider slider;
    public string parameter;

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private float multiplier = 20f;
    private bool bound;

    private void Awake()
    {
        InitializeBinding();
    }

    // 由常驻设置管理器初始化，面板隐藏时仍接收音量变化。
    public void InitializeBinding()
    {
        if (bound) return;
        if (slider == null) slider = GetComponent<Slider>();
        if (slider == null) return;
        bound = true;
        if (slider != null) slider.onValueChanged.AddListener(SliderVolume);
        EventManager2P<string, float>.StartListening(GameEvents.AudioVolumeChanged, SyncSlider);
        if (AudioManager.instance != null)
        {
            if (parameter == "bgm") SyncSlider(parameter, AudioManager.instance.musicVolume);
            else if (parameter == "sfx") SyncSlider(parameter, AudioManager.instance.sfxVolume);
        }
    }

    private void OnDestroy()
    {
        if (!bound) return;
        if (slider != null) slider.onValueChanged.RemoveListener(SliderVolume);
        EventManager2P<string, float>.StopListening(GameEvents.AudioVolumeChanged, SyncSlider);
    }

    private void Start()
    {
        InitializeBinding();
    }

    public void SliderVolume(float _value)
    {
        float value = Mathf.Clamp01(_value);
        // BGM/SFX 统一由管理器控制，避免 Mixer 与 AudioSource 重复衰减。
        if (AudioManager.instance != null && AudioManager.instance.SetVolume(parameter, value, multiplier))
            return;
        if (audioMixer != null)
            audioMixer.SetFloat(parameter, AudioManager.VolumeToDecibels(value, multiplier > 0f ? multiplier : 20f));
    }

    public void LoadSlider(float _value)
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (slider == null) return;
        slider.SetValueWithoutNotify(Mathf.Clamp(_value, slider.minValue, slider.maxValue));
        // 显式应用，避免恢复依赖 Inspector 中的事件绑定。
        SliderVolume(slider.value);
    }

    private void SyncSlider(string channel, float value)
    {
        if (channel == parameter && slider != null)
            slider.SetValueWithoutNotify(Mathf.Clamp(value, slider.minValue, slider.maxValue));
    }

}
