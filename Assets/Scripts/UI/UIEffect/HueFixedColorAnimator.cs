using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HueFixedNoiseAnimator : MonoBehaviour
{
    [Range(0f, 1f)] public float satMin = 0.4f;
    [Range(0f, 1f)] public float satMax = 0.9f;
    [Range(0f, 1f)] public float valMin = 0.6f;
    [Range(0f, 1f)] public float valMax = 1.0f;

    [Tooltip("变化速度")]
    public float speed = 0.3f;

    private Image _image;
    private float _hue;
    private float _seedSat;
    private float _seedVal;

    void Awake()
    {
        _image = GetComponent<Image>();
        Color.RGBToHSV(_image.color, out _hue, out _, out _);

        _seedSat = Random.value * 100f;
        _seedVal = Random.value * 100f;
    }

    void Update()
    {
        float t = Time.time * speed;

        float nSat = Mathf.PerlinNoise(_seedSat, t); // 0..1
        float nVal = Mathf.PerlinNoise(_seedVal, t);

        float sat = Mathf.Lerp(satMin, satMax, nSat);
        float val = Mathf.Lerp(valMin, valMax, nVal);

        _image.color = Color.HSVToRGB(_hue, sat, val);
    }
}