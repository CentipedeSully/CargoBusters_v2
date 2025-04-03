using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class ArmAnimController : MonoBehaviour
{
    //declarations
    [SerializeField] private Animator _armAnimator;
    [SerializeField] private Footstepper _footstepper;
    private FirstPersonController _playerController;
    private bool _isRunDetected = false;
    private float _startDelay;
    private bool _waitingToStart = false;




    //monobehaviours
    private void Start()
    {
        _playerController = GetComponent<FirstPersonController>();
    }

    private void OnEnable()
    {
        _footstepper.OnFootstep += StartAnimation;
    }

    private void OnDisable()
    {
        _footstepper.OnFootstep -= StartAnimation;
    }

    private void Update()
    {
        ReadRunState();
        if (_isRunDetected)
            CalculateAnimationSpeed();
        else
            StopAnimation();
    }


    //internals
    private void ReadRunState()
    {
        if (_playerController != null)
        {
            if (_playerController.IsSprinting() && !_playerController.IsUngrounded() && _playerController.GetSpeed() > 4.0f)
                _isRunDetected = true;
            else _isRunDetected = false;
        }

        else
            _isRunDetected = false;
    }

    private void CalculateAnimationSpeed()
    {
        if (_armAnimator != null && _footstepper != null)
        {
            float armSwingCycle = 1 / _footstepper.GetTickDistance() * 3f; //3 footstep ticks per full arm swing cycle
            _startDelay = armSwingCycle / 2;
            _armAnimator.speed = armSwingCycle;
            
        }
    }

    private void StopAnimation()
    {
        if (_armAnimator != null)
        {
            _armAnimator.SetBool("isRunning", false);
            CancelInvoke(nameof(EnterRun));
            _waitingToStart = false;
        }
            
    }

    private void StartAnimation()
    {
        if (_armAnimator!= null) 
        {
            //only run if the player is running while the animation isn't
            if (_isRunDetected && !_armAnimator.GetBool("isRunning") && !_waitingToStart)
            {
                _waitingToStart = true;
                Invoke(nameof(EnterRun), _startDelay);
            }
                
        }
    }

    private void EnterRun()
    {
        _waitingToStart = false;
        if (_armAnimator != null)
            _armAnimator.SetBool("isRunning", true);
    }



    //externals






}
