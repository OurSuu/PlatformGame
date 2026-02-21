using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    [Header("BGM ที่ต้องการให้เล่น")]
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 1f;

    [Header("Fade Controls")]
    public float fadeInTime = 2f;
    public float fadeOutTime = 2f;

    private AudioSource audioSource;
    private bool isFadingOut = false;

    void Awake()
    {
        SetupBGM();
    }

    private void SetupBGM()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = bgmClip;
        audioSource.volume = 0f; // Start at zero for fade in
        audioSource.loop = false; // We'll handle looping with fade
        audioSource.playOnAwake = false;

        if (bgmClip != null)
        {
            audioSource.Play();
            StartCoroutine(FadeInCoroutine());
        }
    }

    private IEnumerator FadeInCoroutine()
    {
        float timer = 0f;
        while (timer < fadeInTime)
        {
            if (audioSource == null) yield break;
            audioSource.volume = Mathf.Lerp(0f, bgmVolume, timer / fadeInTime);
            timer += Time.deltaTime;
            yield return null;
        }
        if (audioSource != null)
            audioSource.volume = bgmVolume;

        // Start monitoring for manual loop
        StartCoroutine(LoopWithFade());
    }

    private IEnumerator FadeOutCoroutine()
    {
        isFadingOut = true;
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < fadeOutTime)
        {
            if (audioSource == null) yield break;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeOutTime);
            timer += Time.deltaTime;
            yield return null;
        }
        if (audioSource != null)
            audioSource.volume = 0f;

        isFadingOut = false;
    }

    // This will handle smooth looping with fade in and fade out
    private IEnumerator LoopWithFade()
    {
        while (bgmClip != null)
        {
            float loopPoint = bgmClip.length - fadeOutTime;
            // Wait until it's time to fade out
            while (audioSource.time < loopPoint)
                yield return null;

            // Fade out
            yield return FadeOutCoroutine();

            // Restart from beginning, fade in
            if (bgmClip != null)
            {
                audioSource.Stop();
                audioSource.time = 0f;
                audioSource.Play();
                yield return FadeInCoroutine();
                yield break; // The next FadeInCoroutine will start a new LoopWithFade
            }
        }
    }
}