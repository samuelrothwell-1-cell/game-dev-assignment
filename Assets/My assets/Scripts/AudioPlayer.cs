using System;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [SerializeField] AudioClip eatPellet;
    [SerializeField] AudioClip eatPowerPellet;
    [SerializeField] AudioClip MoveEmpty;
    [SerializeField] AudioClip spawnAudio;
    [SerializeField] AudioClip backgroundBeats;
    public AudioSource sfxSource;
    public AudioSource musicSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        musicSource.clip = backgroundBeats;
        musicSource.Play();
        sfxSource.PlayOneShot(spawnAudio);
    }
    public void playEat()
    {
        sfxSource.PlayOneShot(eatPellet);
    }
    public void playPEat()
    {
        sfxSource.PlayOneShot(eatPowerPellet);
    }
    public void playMove()
    {
        sfxSource.PlayOneShot(MoveEmpty);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
