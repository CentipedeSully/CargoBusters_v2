using System.Collections;
using System.Collections.Generic;
using UnityEngine;




[CreateAssetMenu(fileName = "New Interaction Data Asset", menuName = "Data Assets/Interaction Data Asset")]
public class InteractionAudioData : ScriptableObject
{
    [Header("Material Sound Audio Clips")]
    [SerializeField] private List<AudioClip> _fleshyAudio = new();
    [SerializeField] private List<AudioClip> _fleshyMultihitAudio = new();

    



    public AudioClip GetFleshyAudioClip()
    {
        if (_fleshyAudio.Count > 0)
        {
            return RandomUtils<AudioClip>.GetRandomElement(_fleshyAudio);
        }
        else
        {
            Debug.LogError("Attempted to get an audio clip from an empty Audio Data List [Fleshy Audio]. Returned NULL, expect consequencial errors");
            return null;
        }
    }

    public AudioClip GetFleshyMultihitClip()
    {
        if (_fleshyMultihitAudio.Count > 0)
        {
            return RandomUtils<AudioClip>.GetRandomElement(_fleshyMultihitAudio);
        }
        else
        {
            Debug.LogError("Attempted to get an audio clip from an empty Audio Data List [Fleshy Multihit Audio]. Returned NULL, expect consequencial errors");
            return null;
        }
    }





}
