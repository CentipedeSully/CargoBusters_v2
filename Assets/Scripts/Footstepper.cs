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
    [SerializeField] private AudioSource _slideAudioSource;
    private AudioSource _footAudioSource;
    [SerializeField] private bool _footstepsEnabled = true;
    [SerializeField] private float _originalFootstepVolume;
    [SerializeField] private float _crouchedVolume = .4f;
    private float _targetTickDistance = 0;
    private bool _isClimbing = false;
    private bool _isSliding = false;
    private float _slideAudioSpeed = 1;
    [SerializeField] private float _slideFadeInRate = 1;
    [SerializeField] private float _slideFadeOutRate = 1;
    private float _currentSlideVolume = 0;

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

        _firstPersonController.OnMidTransitionEnter += EnterClimb;
        _firstPersonController.OnMidTransitionExit += ExitClimb;

        _firstPersonController.OnWallHangEntered += EnterClimb;
        _firstPersonController.OnWallHangExited += ExitClimb;

        _firstPersonController.OnSlideEnter += EnterSlide;
        _firstPersonController.OnSlideExit += ExitSlide;
    }

    private void OnDisable()
    {
        _firstPersonController.OnJump -= TriggerJumpSideEffects;
        _firstPersonController.OnLand -= TriggerLandingSideEffects;
        _firstPersonController.OnUngrounded -= HaltFootstepTracking;

        _firstPersonController.OnMidTransitionEnter -= EnterClimb;
        _firstPersonController.OnMidTransitionExit -= ExitClimb;

        _firstPersonController.OnWallHangEntered -= EnterClimb;
        _firstPersonController.OnWallHangExited -= ExitClimb;

        _firstPersonController.OnSlideEnter -= EnterSlide;
        _firstPersonController.OnSlideExit -= ExitSlide;
    }

    private void Update()
    {
        if (_firstPersonController != null && _footstepsEnabled && !_isClimbing && !_isSliding)
        {
            ManageFootstepVolume();
            IncrementTick(_firstPersonController.GetSpeed() * Time.deltaTime);
        }

        ManageSlideVolume();
        
            
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

    private void ManageSlideVolume()
    {
        if (_slideAudioSource != null)
        {
            if (!_isSliding && _currentSlideVolume > 0)
            {
                //clamp to zero if calculations dip below zero
                _currentSlideVolume = Mathf.Max(0, _slideFadeOutRate * Time.deltaTime - _currentSlideVolume);
                _slideAudioSource.volume = _currentSlideVolume;

                if (_currentSlideVolume == 0)
                    _slideAudioSource.Stop();
            }

            if (_isSliding && _currentSlideVolume < 1)
            {
                //clamp to 1 if calculations reach above 1
                _currentSlideVolume = Mathf.Min(1, _currentSlideVolume + _slideFadeInRate * Time.deltaTime);
                _slideAudioSource.volume = _currentSlideVolume;
            }
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

    private void EnterClimb()
    {
        _isClimbing = true;
    }

    private void ExitClimb()
    {
        _isClimbing = false;
    }

    private void EnterSlide()
    {
        _isSliding = true;

        if (_slideAudioSource.isPlaying == false)
            _slideAudioSource.Play();
    }

    private void ExitSlide()
    {
        _isSliding = false;
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
