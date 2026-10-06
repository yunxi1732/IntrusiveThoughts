
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSlider_UI : MonoBehaviour
{
    public Slider slider;
    public string parameter;

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private float multiplier;

    private void Start()
    {
        float value = SaveManager.instance != null
            ? SaveManager.instance.GetVolumePreference(parameter) : slider.value;
        LoadSlider(value);
    }

    public void SliderVolume(float _value)
    {
        if (audioMixer == null) return;
        float value = Mathf.Clamp01(_value);
        audioMixer.SetFloat(parameter, value <= 0f ? -80f : Mathf.Log10(value) * multiplier);
    }

    public void LoadSlider(float _value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(Mathf.Clamp(_value, slider.minValue, slider.maxValue));
        // 显式应用，避免恢复依赖 Inspector 中的事件绑定。
        SliderVolume(slider.value);
    }

}
