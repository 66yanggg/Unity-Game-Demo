using UnityEngine;

/// <summary>
/// 音效播放器。使用独立的 AudioSource 播放一次性音效，
/// 不会打断场景里负责循环播放的背景音乐。
/// </summary>
public class Soundcontroller : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlaySound(AudioClip audioClip, float volume = 1f)
    {
        if (audioClip == null || audioSource == null) return;
        audioSource.PlayOneShot(audioClip, Mathf.Clamp01(volume));
    }
}
