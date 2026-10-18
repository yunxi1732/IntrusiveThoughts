using UnityEngine;

/// <summary>在监听器输出端放大声音，避免 AudioSource.volume 的 0–1 限制。</summary>
[RequireComponent(typeof(AudioListener))]
[DisallowMultipleComponent]
public sealed class AudioOutputGain : MonoBehaviour
{
    // 主线程更新，音频线程只读取，不调用 Unity 对象 API。
    private volatile float gain = 1f;

    public float Gain
    {
        set => gain = Mathf.Clamp(value, 1f, 4f);
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float currentGain = gain;
        for (int i = 0; i < data.Length; i++)
        {
            float sample = data[i] * currentGain;
            // 限制峰值，避免输出超出有效范围。
            data[i] = sample > 1f ? 1f : sample < -1f ? -1f : sample;
        }
    }
}
