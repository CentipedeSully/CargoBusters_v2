using Cinemachine;
using Cinemachine.Utility;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CamDisplacer : MonoBehaviour
{
    //Declarations
    [SerializeField] private CinemachineVirtualCamera _virtualCamera;
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private FirstPersonController _playerController;

    [Header("Head movement on Sprint")]
    [Tooltip("Off by default. May cause motion sickness.")]
    [SerializeField] private bool _toggleHeadBob = false;
    [SerializeField] private float _bobDistance = .2f;
    [SerializeField] private float _yDampening = .2f;
    [SerializeField] private float _bobIterationTime = .5f;
    private float _currentBobTime = 0;
    private Cinemachine3rdPersonFollow _3rdPersonfollowComponent;
    private Vector3 _originalLocalPosition;
    private bool _isBobActive = false;
    private bool _cancellingDamp = false;

    [Header("ScreenShake")]
    [SerializeField] private bool _isScreenShaking = false;
    [SerializeField] private float _defaultShakeDuration = .2f;
    private float _currentShakeTime = 0;
    [SerializeField] private float _defaultDisplacementFrequency = .02f;
    [SerializeField] private Vector2 _defaultDisplacementRange = new Vector2(.02f, .02f);
    [SerializeField] private Vector2 _displacementStrength;
    [SerializeField] private float _shakeDuration;




    //Monobehaviours
    private void Start()
    {
        _originalLocalPosition = _cameraRoot.transform.localPosition;
        _3rdPersonfollowComponent = _virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
    }

    private void OnEnable()
    {
        _playerController.OnLand += ShakeScreenOnHardLandings;
    }

    private void OnDisable()
    {
        _playerController.OnLand -= ShakeScreenOnHardLandings;
    }

    private void Update()
    {
        ManageScreenShake();
        WatchPlayerSprintState();
        ManageBob();
        RecedeDamping();
    }




    //internals
    private void WatchPlayerSprintState()
    {
        if (_playerController.IsSprinting() && _playerController.GetSpeed() > 4 && !_playerController.IsUngrounded() && !_isScreenShaking)
            _isBobActive = true;
        else _isBobActive = false;
    }
    private void ManageBob()
    {

        if (_isBobActive && _toggleHeadBob)
        {
            if (_cancellingDamp)
            {
                _cancellingDamp = false;

                //reset the damp back to original
                _3rdPersonfollowComponent.Damping = Vector3.up * _yDampening;
            }
                

            if (_currentBobTime == 0)
            {
                if (_3rdPersonfollowComponent.Damping == Vector3.zero)
                {
                    //setup the natural dampening
                    _3rdPersonfollowComponent.Damping = Vector3.up * _yDampening;
                }

                //displace the camera
                _cameraRoot.localPosition = _cameraRoot.localPosition + (Vector3.up * -_bobDistance);

            }
            _currentBobTime += Time.deltaTime;

            //go back up when halfway done
            if (_currentBobTime >= _bobIterationTime / 2 && _cameraRoot.position != _originalLocalPosition)
            {
                _cameraRoot.localPosition = _originalLocalPosition;
            }

            //reset the counter when the iteration is complete
            if (_currentBobTime >= _bobIterationTime)
            {
                _currentBobTime = 0;
            }
        }

        else
        {
            if (_3rdPersonfollowComponent.Damping != Vector3.zero)
            {
                //reset the timer
                _currentBobTime = 0;

                //reposition camera to the original position (only if not screen shaking)
                if (!_isScreenShaking)
                    _cameraRoot.localPosition = _originalLocalPosition;

                //reset damp settings after a delay
                //to allow the cam to damp back into place and avoid jerky cam
                _cancellingDamp = true;

            }
            
        }
    }
    private void RecedeDamping()
    {
        if (_cancellingDamp)
        {
            //calculate the reduced damp value
            Vector3 newDampValue = _3rdPersonfollowComponent.Damping - (Vector3.up * Time.deltaTime);
            
            //don't allow a below-zero value
            float dampedMag = Mathf.Max(0, newDampValue.magnitude);

            //update the damp value
            _3rdPersonfollowComponent.Damping = dampedMag * Vector3.up;

            //stop if the recession is completed
            if (dampedMag == 0)
                _cancellingDamp = false;

        }
    }
    private void ManageScreenShake()
    {
        if (_isScreenShaking)
        {
            if (_currentShakeTime == 0)
                InvokeRepeating(nameof(DisplaceCam), 0, _defaultDisplacementFrequency);

            _currentShakeTime += Time.deltaTime;

            if (_currentShakeTime >= _shakeDuration)
            {
                CancelScreenShake();
                _isScreenShaking = false;
            }
        }

        //in case screen shake got disabled externally while in mid-shake
        else
        {
            if (_currentShakeTime > 0)
                CancelScreenShake();
        }
    }
    private void DisplaceCam()
    {
        float randomDisplacementX = Random.Range(-_displacementStrength.x, _displacementStrength.x);
        float randomDisplacementY = Random.Range(-_displacementStrength.y, _displacementStrength.y);
        Vector3 randomDisplacement = new Vector3(randomDisplacementX, randomDisplacementY, _originalLocalPosition.z);
        _cameraRoot.localPosition = _originalLocalPosition + randomDisplacement;
    }

    private void CancelScreenShake()
    {
        _currentShakeTime = 0;
        CancelInvoke(nameof(DisplaceCam));
        _cameraRoot.localPosition = _originalLocalPosition;
        _isScreenShaking = false;
    }

    private void ShakeScreenOnHardLandings(FootSoundType landing)
    {
        if (landing == FootSoundType.landModerate)
            ShakeScreen();
        else if (landing == FootSoundType.landHeavy || landing == FootSoundType.landNasty)
            ShakeScreen(0.03f, _defaultDisplacementRange.magnitude * 2);
    }


    //externals
    public void ShakeScreen()
    {

        ShakeScreen(0, 0);
    }

    public void ShakeScreen(float duration, float strength)
    {
        if (duration <= 0)
            _shakeDuration = _defaultShakeDuration;
        else _shakeDuration = duration;

        if (strength <= 0)
            _displacementStrength = _defaultDisplacementRange;
        else _displacementStrength = new Vector2(strength,strength);


        //interrupt current shake
        if (_isScreenShaking)
            CancelScreenShake();

        //start new shake
        _isScreenShaking = true;

    }


}
