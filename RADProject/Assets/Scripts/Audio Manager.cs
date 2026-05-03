using UnityEngine;
using System.Collections.Generic;
 
public enum SoundType
{
    // Ambiences
    Light_Ambience, Birds_Ambience, Fan_Ambience,

    // Contamination
    Low_Hum_Creepy, Low_Hum, Sudden_Bass, Whispers,

    // Doors
    Door_Open, Door_Close, Heavy_Door_Open, Heavy_Door_Close,

    // Effects
    Crows, Ding, Heartbeat, Text_Boop,

    // Footsteps
    Step_Concrete, Step_Stone_1, Step_Stone_2, Step_Grass_1, Step_Grass_2,

    // Water
    Pond_Water, Sink_Water_Start, Sink_Water_Loop, Sink_Water_Stop
}

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public SoundType Type;
        public AudioClip Clip;
 
        [Range(0f, 1f)]
        public float Volume = 1f;
 
        [HideInInspector]
        public AudioSource Source;
    }
 
    //Singleton
    public static AudioManager Instance;
 
    //All sounds and their associated type - Set these in the inspector
    public Sound[] AllSounds;
 
    //Runtime collections
    private Dictionary<SoundType, Sound> _soundDictionary = new Dictionary<SoundType, Sound>();
    private AudioSource ambienceSrc;
 
    private void Awake()
    {
        // //Assign singleton
        // Instance = this;
 
        // //Set up sounds
        // foreach(var s in AllSounds)
        // {
        //     _soundDictionary[s.Type] = s;
        // }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var s in AllSounds)
        {
            _soundDictionary[s.Type] = s;
        }
        
    }
 
 
 
    //Call this method to play a sound
    public void Play(SoundType type)
    {
        //Make sure there's a sound assigned to your specified type
        if (!_soundDictionary.TryGetValue(type, out Sound s))
        {
            Debug.LogWarning($"Sound type {type} not found!");
            return;
        }
 
        //Creates a new sound object
        var soundObj = new GameObject($"Sound_{type}");
        soundObj.transform.SetParent(transform);
        var audioSrc = soundObj.AddComponent<AudioSource>();
 
        //Assigns your sound properties
        audioSrc.clip = s.Clip;
        audioSrc.volume = s.Volume;
 
        //Play the sound
        audioSrc.Play();
 
        //Destroy the object
        Destroy(soundObj, s.Clip.length);
    }
 
    //Call this method to change music tracks
    public void Ambience(SoundType type)
    {
        if (!_soundDictionary.TryGetValue(type, out Sound track))
        {
            Debug.LogWarning($"Ambience track {type} not found!");
            return;
        }
 
        var container = new GameObject($"Ambience_{type}");
        container.transform.SetParent(transform);
        ambienceSrc = container.AddComponent<AudioSource>();
        ambienceSrc.loop = true;
 
        ambienceSrc.clip = track.Clip;
        ambienceSrc.volume = track.Volume;
        ambienceSrc.Play();
    }
    public void StopAmbience(SoundType type)
    {
        var obj = transform.Find($"Ambience_{type}");
        if (obj != null)
        {
            Destroy(obj.gameObject);
        }
    }
}