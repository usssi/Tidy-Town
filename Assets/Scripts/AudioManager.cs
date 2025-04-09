using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;
    public static AudioManager instance; // Static instance

    void Awake()
    {
        // Updated Singleton setup
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return; // Exit early if duplicate
        }

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.outputAudioMixerGroup = s.output;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.mute = s.mute;
        }
    }

    public void Play(string name, float pitchTone)
    {
        if (instance == null) return; // Safety check
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) return;
        s.source.pitch = pitchTone;
        s.source.Play();
    }

    public void Stop(string name)
    {
        if (instance == null) return; // Safety check
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) return;
        s.source.Stop();
    }

    // Example usage, assuming UI Buttons call this. Keep if needed.
    public void OnButtonSelect()
    {
        Play("buttonSelect", 1f); // Assumes "buttonSelect" sound exists
    }
}