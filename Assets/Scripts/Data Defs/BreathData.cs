using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;





public enum BreathType{
    unset,
    quiet,
    heavy,
    reducing,
    jump,
    land

}

public enum VoiceGender
{
    unset,
    male,
    female
}

[CreateAssetMenu(fileName = "New Breath Data Asset", menuName = "Data Assets/Breath Data Asset")]
public class BreathData : ScriptableObject
{
    [Header("Breathing Source AudioClips")]
    [SerializeField] private List<AudioClip> _maleHeavyBreaths = new();
    [SerializeField] private List<AudioClip> _maleReducedHeavyBreaths = new();
    [SerializeField] private List<AudioClip> _maleJumpBreaths = new();
    [SerializeField] private List<AudioClip> _maleLandBreaths = new();

    [Space(20)]
    [SerializeField] private List<AudioClip> _femaleHeavyBreaths = new();
    [SerializeField] private List<AudioClip> _femaleReducedHeavyBreaths = new();
    [SerializeField] private List<AudioClip> _femaleJumpBreaths = new();
    [SerializeField] private List<AudioClip> _femaleLandBreaths = new();



    public AudioClip GetRandomClip(VoiceGender gender, BreathType breathType)
    {
        if (gender == VoiceGender.unset)
        {
            Debug.Log("Attempted to voice a breath with an unset gender. Defaulting to 'male'");
            gender = VoiceGender.male;
        }

        if (gender == VoiceGender.male)
        {
            switch (breathType)
            {

                case BreathType.heavy:
                    return RandomUtils<AudioClip>.GetRandomElement(_maleHeavyBreaths);

                case BreathType.reducing:
                    return RandomUtils<AudioClip>.GetRandomElement(_maleReducedHeavyBreaths);


                case BreathType.jump:
                    return RandomUtils<AudioClip>.GetRandomElement(_maleJumpBreaths);


                case BreathType.land:
                    return RandomUtils<AudioClip>.GetRandomElement(_maleLandBreaths);
                    

                default:
                    Debug.LogWarning(
                        $"Failed to catch a proper male audioclip for breathType: {breathType}'." +
                        $"\nReturning a 'male, Jump' clip");
                    return RandomUtils<AudioClip>.GetRandomElement(_maleJumpBreaths);
            }
        }

        else if (gender == VoiceGender.female)
        {
            switch (breathType)
            {

                case BreathType.heavy:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleHeavyBreaths);

                case BreathType.reducing:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleReducedHeavyBreaths);


                case BreathType.jump:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleJumpBreaths);


                case BreathType.land:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleLandBreaths);
                    

                default:
                    Debug.LogWarning(
                        $"Failed to catch a proper female audioClip for breathType: {breathType}'." +
                        $"\nReturning a 'female, jump' clip");
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleJumpBreaths);
            }
        }

        Debug.LogWarning($"Failed to catch a proper audioClip for parameters 'voiceGender: {gender}, breathType: {breathType}'." +
           $"\nReturning a 'male jump' clip");
        return RandomUtils<AudioClip>.GetRandomElement(_maleJumpBreaths);
    }
}
