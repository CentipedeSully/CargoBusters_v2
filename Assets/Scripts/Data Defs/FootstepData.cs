using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum FloorType
{
    unset,
    general,
    wood,
    metal
}

public enum FootSoundType
{
    unset,
    walk,
    run,
    slide,
    landEasy,
    landModerate,
    landHeavy,
    landNasty,
    jump
}

public static class RandomUtils<T>
{
    public static T GetRandomElement(List<T> genericList)
    {
        if (genericList.Count == 0) return default;

        return genericList[Random.Range(0, genericList.Count)];
    }
}


[CreateAssetMenu(fileName ="New Footstep Data Asset", menuName = "Data Assets/Footstep Data Asset")]
public class FootstepData : ScriptableObject
{
    [Header("Footstep Source AudioClips")]
    [SerializeField] private List<AudioClip> _generalWalksteps = new();
    [SerializeField] private List<AudioClip> _generalRunsteps = new();
    [SerializeField] private List<AudioClip> _generalSlidings = new();
    [SerializeField] private List<AudioClip> _generalEasyLandsteps = new();
    [SerializeField] private List<AudioClip> _generalModerateLandsteps = new();
    [SerializeField] private List<AudioClip> _generalHeavyLandsteps = new();
    [SerializeField] private List<AudioClip> _generalNastyLandsteps = new();
    [SerializeField] private List<AudioClip> _generalJumpsteps = new();

    [Space(20)]
    [SerializeField] private List<AudioClip> _woodWalksteps = new();
    [SerializeField] private List<AudioClip> _woodRunsteps = new();
    [SerializeField] private List<AudioClip> _woodLandsteps = new();
    [SerializeField] private List<AudioClip> _woodJumpsteps = new();

    [Space(20)]
    [SerializeField] private List<AudioClip> _metalWalksteps = new();
    [SerializeField] private List<AudioClip> _metalRunsteps = new();
    [SerializeField] private List<AudioClip> _metalLandsteps = new();
    [SerializeField] private List<AudioClip> _metalJumpsteps = new();






    public AudioClip GetRandomClip(FootSoundType footSoundType, FloorType floorType)
    {

        if (footSoundType == FootSoundType.unset)
        {
            footSoundType = FootSoundType.walk;
            Debug.LogWarning("Attempted to get a footstep audio clip with an unset FootSoundType.\nReturning a walking step sound");
        }    

        if (floorType == FloorType.unset)
        {
            floorType = FloorType.general;
            Debug.LogWarning("Attempted to get a footstep audio clip with an unset FloorType.\nReturning a general step sound");
        }

        if (floorType == FloorType.general)
        {
            switch (footSoundType)
            {
                case FootSoundType.walk:
                    return RandomUtils<AudioClip>.GetRandomElement(_generalWalksteps);

                case FootSoundType.run:
                    return RandomUtils<AudioClip>.GetRandomElement(_generalRunsteps);

                case FootSoundType.slide:
                    return RandomUtils<AudioClip>.GetRandomElement(_generalSlidings);

                case FootSoundType.landEasy:
                    return RandomUtils<AudioClip>.GetRandomElement(_generalEasyLandsteps);

                case FootSoundType.landModerate:
                    return RandomUtils<AudioClip>.GetRandomElement(_generalModerateLandsteps);

                case FootSoundType.landHeavy:
                    return RandomUtils<AudioClip>.GetRandomElement(_generalHeavyLandsteps);

                case FootSoundType.landNasty:
                    return RandomUtils<AudioClip>.GetRandomElement(_generalNastyLandsteps);

                case FootSoundType.jump:
                    return RandomUtils<AudioClip>.GetRandomElement(_generalJumpsteps);

            }
        }

        else if (floorType == FloorType.wood)
        {
            switch (footSoundType)
            {
                case FootSoundType.walk:
                    return RandomUtils<AudioClip>.GetRandomElement(_woodWalksteps);

                case FootSoundType.run:
                    return RandomUtils<AudioClip>.GetRandomElement(_woodRunsteps);

                case FootSoundType.landModerate:
                    return RandomUtils<AudioClip>.GetRandomElement(_woodLandsteps);

                case FootSoundType.jump:
                    return RandomUtils<AudioClip>.GetRandomElement(_woodJumpsteps);

            }
        }
        
        else if (floorType == FloorType.metal)
        {
            switch (footSoundType)
            {
                case FootSoundType.walk:
                    return RandomUtils<AudioClip>.GetRandomElement(_metalWalksteps);

                case FootSoundType.run:
                    return RandomUtils<AudioClip>.GetRandomElement(_metalRunsteps);

                case FootSoundType.landModerate:
                    return RandomUtils<AudioClip>.GetRandomElement(_metalLandsteps);

                case FootSoundType.jump:
                    return RandomUtils<AudioClip>.GetRandomElement(_metalJumpsteps);

            }
        }


        Debug.LogWarning($"Failed to catch a proper audioClip for parameters 'footSoundType: {footSoundType}, floorType: {floorType}'." +
            $"\nReturning a general walk clip");
        return RandomUtils<AudioClip>.GetRandomElement(_generalWalksteps);
    }




}
