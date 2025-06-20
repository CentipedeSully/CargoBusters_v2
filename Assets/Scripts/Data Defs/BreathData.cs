using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;





public enum BreathType{
    unset,
    quiet,
    continuousHeavy,
    continuousLight,
    jump,
    landModerate,
    landHeavy,
    landNasty,
    gruntLight,
    gruntModerate,
    gruntHeavy

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
    [SerializeField] private List<AudioClip> _maleContinuousHeavyBreaths = new();
    [SerializeField] private List<AudioClip> _maleContinuousLightBreaths = new();
    [SerializeField] private List<AudioClip> _maleJumpBreaths = new();
    [SerializeField] private List<AudioClip> _maleLandBreaths = new();

    [Space(20)]
    [SerializeField] private List<AudioClip> _femaleContinuousHeavyBreaths = new();
    [SerializeField] private List<AudioClip> _femaleContinuousLightBreaths = new();
    [SerializeField] private List<AudioClip> _femaleJumpBreaths = new();
    [SerializeField] private List<AudioClip> _femaleLandModerateBreaths = new();
    [SerializeField] private List<AudioClip> _femaleLandHeavyBreaths = new();
    [SerializeField] private List<AudioClip> _femaleLandNastyBreaths = new();
    [SerializeField] private List<AudioClip> _femaleLightGrunt = new();
    [SerializeField] private List<AudioClip> _femaleModerateGrunt = new();
    [SerializeField] private List<AudioClip> _femaleHeavyGrunt = new();



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

                case BreathType.continuousHeavy:
                    return RandomUtils<AudioClip>.GetRandomElement(_maleContinuousHeavyBreaths);

                case BreathType.continuousLight:
                    return RandomUtils<AudioClip>.GetRandomElement(_maleContinuousLightBreaths);


                case BreathType.jump:
                    return RandomUtils<AudioClip>.GetRandomElement(_maleJumpBreaths);


                case BreathType.landModerate:
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

                case BreathType.continuousHeavy:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleContinuousHeavyBreaths);

                case BreathType.continuousLight:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleContinuousLightBreaths);


                case BreathType.jump:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleJumpBreaths);


                case BreathType.landModerate:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleLandModerateBreaths);

                case BreathType.landHeavy:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleLandHeavyBreaths);

                case BreathType.landNasty:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleLandNastyBreaths);


                case BreathType.gruntLight:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleLightGrunt);

                case BreathType.gruntModerate:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleModerateGrunt);

                case BreathType.gruntHeavy:
                    return RandomUtils<AudioClip>.GetRandomElement(_femaleHeavyGrunt);



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
