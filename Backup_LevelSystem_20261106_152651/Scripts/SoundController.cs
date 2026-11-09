using UnityEngine;

public class Soundcontroller : MonoBehaviour
{
    private AudioSource audioSource;
    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }
    public void PlaySound(AudioClip audioClip, float volume = 1f)
    {
        audioSource.PlayOneShot(audioClip, volume);
    }
}
