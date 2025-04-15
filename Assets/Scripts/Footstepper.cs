using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Footstepper : MonoBehaviour
{
    [SerializeField] private FirstPersonController _firstPersonController;
    [Tooltip("The minimum distance needed to trek before a footstep is made")]
    [SerializeField] [Range(1,5)]private float _tickDistance = 1.75f;
    [SerializeField] private float _relateiveCrouchedTickDistance = .5f;
    [SerializeField] private float _currentTickProgress = 0;

    [SerializeField] private FloorType _currentFloorType = FloorType.unset;
    [SerializeField] private FootstepData _footstepData;
    private AudioSource _footAudioSource;
    [SerializeField] private bool _footstepsEnabled = true;
    [SerializeField] private float _originalFootstepVolume;
    [SerializeField] private float _crouchedVolume = .4f;
    private float _targetTickDistance = 0;

    public delegate void FootstepEvent();
    public event FootstepEvent OnFootstep;




    //monobehaviours
    private void Awake()
    {
        _footAudioSource = GetComponent<AudioSource>();
        SetFloorType(FloorType.general);
    }

    private void OnEnable()
    {
        _firstPersonController.OnJump += TriggerJumpSideEffects;
        _firstPersonController.OnLand += TriggerLandingSideEffects;
        _firstPersonController.OnUngrounded += HaltFootstepTracking;
    }

    private void OnDisable()
    {
        _firstPersonController.OnJump -= TriggerJumpSideEffects;
        _firstPersonController.OnLand -= TriggerLandingSideEffects;
        _firstPersonController.OnUngrounded -= HaltFootstepTracking;
    }

    private void Update()
    {
        if (_firstPersonController != null && _footstepsEnabled)
        {
            ManageFootstepVolume();
            IncrementTick(_firstPersonController.GetSpeed() * Time.deltaTime);
        }
            
    }






    //internals
    private void IncrementTick(float distance)
    {
        _currentTickProgress += distance;


        //apply the crouch tick distance if the player is crouching
        if (_firstPersonController.IsCrouched() || _firstPersonController.IsCrouchTransitioning())
            _targetTickDistance = _tickDistance * _relateiveCrouchedTickDistance;
        else _targetTickDistance = _tickDistance;

        if  (_currentTickProgress >= _targetTickDistance)
        {
            SetFootSound(FootSoundType.walk);
            PlayFootSound();
            ResetTick();
            OnFootstep?.Invoke();
        }
    }

    private void ManageFootstepVolume()
    {
        if (_firstPersonController.IsCrouchTransitioning() || _firstPersonController.IsCrouched())
        {
            _footAudioSource.volume = _originalFootstepVolume * _crouchedVolume;
        }
        else
        {
            _footAudioSource.volume = _originalFootstepVolume;
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
            _footAudioSource.Play();
    }

    private void ResetTick()
    {
        _currentTickProgress = 0;
    }


    private void HaltFootstepTracking()
    {
        _footstepsEnabled = false;
        ResetTick();
    }


    private void TriggerJumpSideEffects()
    {
        //halt the footstep tracker
        HaltFootstepTracking();

        //play the jump sound
        SetFootSound(FootSoundType.jump);
        PlayFootSound();

    }

    private void TriggerLandingSideEffects(FootSoundType landingType)
    {
        //play the land sound
        SetFootSound(landingType);
        PlayFootSound();


        //resume the footstep tracker
        _footstepsEnabled = true;

    }


    //externals
    public void SetFloorType(FloorType newType)
    {
        if (newType != _currentFloorType)
            _currentFloorType = newType;
    }

    public float GetTickDistance()
    {
        return _tickDistance;
    }

}
