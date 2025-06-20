using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum breathState
{
    unset,
    inactive,
    intro,
    heavyBreaths,
    exitingHeavy,
    calmingBreaths
    
}

public class Breather : MonoBehaviour
{
    [SerializeField] private FirstPersonController _firstPersonController;
    [SerializeField] private BreathData _breathData;
    [SerializeField] private VoiceGender _voiceGender = VoiceGender.female;
    [SerializeField] private bool _isBreathEnabled = true;
    private AudioSource _audioSource;
    [SerializeField] private AudioSource _heavyBreathingSource;
    [SerializeField] private bool _isWinded = false;
    [SerializeField] private bool _isHeavyBreathingPaused = false;
    [SerializeField] private breathState _heavyBreathState = breathState.unset;
    [SerializeField] private float _windedStartDelay = 0.3f;
    private float _currentStartTime = 0;
    [SerializeField] private float _windedEndDelay = 2.0f;
    private float _currentEndTime = 0;
    [SerializeField] private float _calmedBreathsDuration = 2f;
    private float _currentCalmedTime = 0;

    [SerializeField] private float _fadeInDuration = 8f;
    private float _currentFadeTime = 0;
    [SerializeField] private float _breathVolume;
    [SerializeField] private float _volumeMultiplier = 0;

    public delegate void BreathingEvent();
    public event BreathingEvent OnPauseHeavyBreathing;
    public event BreathingEvent OnResumeHeavyBreathing;


    //monobehaviours
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        _firstPersonController.OnJump += TriggerJumpBreath;
        _firstPersonController.OnLand += TriggerLandingBreath;
        _firstPersonController.OnRunEnter += TriggerHeavyBreathing;
        _firstPersonController.OnRunExit += ExitHeavyBreathing;

        _firstPersonController.OnMidTransitionEnter += TriggerModerateGrunt;
        _firstPersonController.OnWallClimbOverTriggered += TriggerHeavyGrunt;

       _firstPersonController.OnSlideEnter += TriggerLightGrunt;
    }

    private void OnDisable()
    {
        _firstPersonController.OnJump -= TriggerJumpBreath;
        _firstPersonController.OnLand -= TriggerLandingBreath;
        _firstPersonController.OnRunEnter -= TriggerHeavyBreathing;
        _firstPersonController.OnRunExit -= ExitHeavyBreathing;

        _firstPersonController.OnMidTransitionEnter -= TriggerModerateGrunt;
        _firstPersonController.OnWallClimbOverTriggered -= TriggerHeavyGrunt;

        _firstPersonController.OnSlideEnter -= TriggerLightGrunt;


    }

    private void Update()
    {
        //WatchSprintState(); //sets or unsets the winded state
        WatchUngroundedState(); //pauses or unpauses breathing

        if (!_isHeavyBreathingPaused && _isBreathEnabled)
        {
            ManageHeavyBreathing();
            FadeSound();
        }
            
    }


    //internals
    private void WatchUngroundedState()
    {
        //pause the breathing if the player is falling while heavy breathing
        if (_firstPersonController.IsUngrounded())
        {
            if ((_heavyBreathState==breathState.heavyBreaths 
                || _heavyBreathState == breathState.calmingBreaths 
                || _heavyBreathState == breathState.exitingHeavy) && !_isHeavyBreathingPaused)
                PauseHeavyBreathing();
        }

        //resume the breathing if the player isn't falling anymore, but is still paused
        else
        {
            if ((_heavyBreathState == breathState.heavyBreaths
                || _heavyBreathState == breathState.calmingBreaths
                || _heavyBreathState == breathState.exitingHeavy) && _isHeavyBreathingPaused)
                ResumeHeavyBreathing();

        }
    }

    private void ManageHeavyBreathing()
    {
        if (_isWinded)
        {
            //initialize
            if (_heavyBreathState == breathState.unset || _heavyBreathState == breathState.inactive)
                _heavyBreathState = breathState.intro;


            //tick down the time before the heavy breaths begin
            if (_heavyBreathState == breathState.intro)
            {
                if (_currentStartTime < _windedStartDelay)
                {
                    _currentStartTime += Time.deltaTime;
                }

                //start the heavy breathing now
                if (_currentStartTime >= _windedStartDelay)
                {
                    _currentStartTime = 0;
                    _heavyBreathState = breathState.heavyBreaths;

                    if (_heavyBreathingSource != null && _breathData != null)
                    {
                        //QoL, loop if the designer (also me) forgot
                        if (_heavyBreathingSource.loop == false)
                            _heavyBreathingSource.loop = true;

                        _heavyBreathingSource.clip = _breathData.GetRandomClip(_voiceGender, BreathType.continuousHeavy);
                        _heavyBreathingSource.Play();

                    }

                }
            }

            else if (_heavyBreathState == breathState.calmingBreaths || _heavyBreathState == breathState.exitingHeavy)
            {
                _currentCalmedTime = 0;
                _currentEndTime = 0;
                _heavyBreathState = breathState.heavyBreaths;
                _heavyBreathingSource.clip = _breathData.GetRandomClip(_voiceGender, BreathType.continuousHeavy);
                _heavyBreathingSource.Play();
            }


        }

        else
        {
            //simply reset the startup time
            if (_heavyBreathState == breathState.intro)
            {
                _currentStartTime = 0;
                _heavyBreathState = breathState.inactive;
            }

            //don't immediately stop the heavy breathing. Begin ticking the delayed exit
            else if (_heavyBreathState == breathState.heavyBreaths)
            {
                _heavyBreathState = breathState.exitingHeavy;
                
            }

            if (_heavyBreathState == breathState.exitingHeavy)
            {
                _currentEndTime += Time.deltaTime;

                if (_currentEndTime >= _windedEndDelay)
                {
                    _currentEndTime = 0;
                    _heavyBreathState = breathState.calmingBreaths;
                    _heavyBreathingSource.clip = _breathData.GetRandomClip(_voiceGender, BreathType.continuousLight);
                    _heavyBreathingSource.Play();
                }
            }

            //end the heavy breathing if the time is up
            if (_heavyBreathState == breathState.calmingBreaths)
            {
                _currentCalmedTime += Time.deltaTime;

                if (_currentCalmedTime >= _calmedBreathsDuration)
                {
                    _currentCalmedTime = 0;
                    _heavyBreathState = breathState.inactive;
                    _heavyBreathingSource.Stop();
                }
            }
        }
    }

    private void FadeSound()
    {

        if (_heavyBreathState == breathState.heavyBreaths)
        {
            //increase the multiplier up to 1, based on how long we're running
            if (_currentFadeTime < _fadeInDuration)
                _currentFadeTime += Time.deltaTime;
            else _currentFadeTime = _fadeInDuration;

        }

        else if (_heavyBreathState == breathState.inactive)
        {
            //decrease the volumen multiplier down to 0, based on how long we aren't running
            if (_currentFadeTime > 0)
                _currentFadeTime -= Time.deltaTime;
            else _currentFadeTime = 0;
        }

        //calculate the modifier. Range: [0,1]
        _volumeMultiplier = _currentFadeTime / _fadeInDuration;

        _heavyBreathingSource.volume = _breathVolume * _volumeMultiplier;

    }

    private void TriggerHeavyBreathing()
    {
        _isWinded = true;
        //Debug.Log("Heavy Breath Triggered");
    }
    private void ExitHeavyBreathing()
    {
        _isWinded = false;
        //Debug.Log("CANCALLED Heavy Breath");
    }

    private void PauseHeavyBreathing() { _isHeavyBreathingPaused = true; _heavyBreathingSource.Stop(); }
    private void ResumeHeavyBreathing() { _isHeavyBreathingPaused = false; _heavyBreathingSource.Play(); }

    private void TriggerJumpBreath()
    {
        if (_isBreathEnabled && _audioSource != null && _breathData != null)
        {
            OnPauseHeavyBreathing?.Invoke();
            _audioSource.clip = _breathData.GetRandomClip(_voiceGender, BreathType.jump);
            _audioSource.Play();
        }
    }

    private void TriggerLandingBreath(FootSoundType landingType)
    {
        if (_isBreathEnabled && _audioSource != null && _breathData != null)
        {
            OnResumeHeavyBreathing?.Invoke();

            switch (landingType)
            {
                case FootSoundType.landModerate:
                    _audioSource.clip = _breathData.GetRandomClip(_voiceGender, BreathType.landModerate);
                    _audioSource.Play();
                    break;

                case FootSoundType.landHeavy:
                    _audioSource.clip = _breathData.GetRandomClip(_voiceGender, BreathType.landHeavy);
                    _audioSource.Play();
                    break;

                case FootSoundType.landNasty:
                    _audioSource.clip = _breathData.GetRandomClip(_voiceGender, BreathType.landNasty);
                    _audioSource.Play();
                    break;
            }
            
        }
    }

    private void TriggerLightGrunt()
    {
        if (_isBreathEnabled && _audioSource != null && _breathData != null)
        {
            _audioSource.clip = _breathData.GetRandomClip(_voiceGender, BreathType.gruntLight);
            _audioSource.Play();
        }
    }

    private void TriggerModerateGrunt()
    {
        if (_isBreathEnabled && _audioSource != null && _breathData != null)
        {
            _audioSource.clip = _breathData.GetRandomClip(_voiceGender,BreathType.gruntModerate);
            _audioSource.Play();
        }
    }

    private void TriggerHeavyGrunt()
    {
        if (_isBreathEnabled && _audioSource != null && _breathData != null)
        {
            _audioSource.clip = _breathData.GetRandomClip(_voiceGender, BreathType.gruntHeavy);
            _audioSource.Play();
        }
    }



    //externals



}
