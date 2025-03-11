using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Footstepper : MonoBehaviour
{
    [SerializeField] private FirstPersonController _firstPersonController;
    [Tooltip("The minimum distance needed to trek before a footstep is made")]
    [SerializeField] [Range(1,5)]private float _tickDistance = 4;
    [SerializeField] private float _currentTickProgress = 0;

    [SerializeField] private FloorType _currentFloorType = FloorType.unset;
    [SerializeField] private FootstepData _footstepData;
    private AudioSource _footAudioSource;




    //monobehaviours
    private void Awake()
    {
        _footAudioSource = GetComponent<AudioSource>();
        SetFloorType(FloorType.general);
    }

    private void Update()
    {
        if (_firstPersonController != null)
            IncrementTick(_firstPersonController.GetSpeed() * Time.deltaTime);
    }






    //internals
    private void IncrementTick(float distance)
    {
        _currentTickProgress += distance;
        if (_currentTickProgress >= _tickDistance)
        {
            SetFootSound(FootSoundType.walk);
            PlayFootSound();
            ResetTick();
        }
    }

    private void SetFootSound(FootSoundType newType)
    {
        if (_footAudioSource != null && _footstepData != null)
            _footAudioSource.clip = _footstepData.GetRandomClip(newType, _currentFloorType);
        else Debug.LogError($"Missing reference detected in footstepper on object '{this.gameObject}'\n footAudioSource:'{_footAudioSource}', footstepData:'{_footstepData}'");
    }

    private void PlayFootSound()
    {
        if (_footAudioSource != null)
        {
            if (_footAudioSource.isPlaying == false)
                _footAudioSource.Play();
        }
    }

    private void ResetTick()
    {
        _currentTickProgress = 0;
    }



    //externals
    public void SetFloorType(FloorType newType)
    {
        if (newType != _currentFloorType)
            _currentFloorType = newType;
    }


}
