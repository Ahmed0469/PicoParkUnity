using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioClip[] playerFootStepSFX;
    public AudioClip[] playerLandingSFX;
    public AudioClip[] playerJumpingSFX;
    public AudioClip[] countDownSFX;
    public AudioClip killSFX;
    public AudioClip playerDieSFX;
    public AudioClip timeTickSFX;
    public AudioClip cannonSFX;
    public AudioClip vaseSFX;
    public AudioClip wallBreakSFX;
    public AudioClip coinCatchSFX;
    public AudioClip JumperSFX;
    public AudioClip winSFX;
    public AudioClip looseSFX;
    public AudioSource audioSource;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        PlayMusic();
    }
    public void PlayOneShot(AudioClip audioClip)
    {
        if (PlayerPrefs.GetInt("Sound", 1) == 1)
        {
            audioSource.PlayOneShot(audioClip);
        }
    }
    public void PlayMusic()
    {
        if (PlayerPrefs.GetInt("Music",1) == 1)
        {
            audioSource.Play();
        }
    }
    public void StopMusic()
    {
        audioSource.Stop();
    }
}
