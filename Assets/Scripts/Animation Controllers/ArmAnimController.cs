using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;


public enum ArmSide
{
    Left,
    Right 
}

public class ArmAnimController : MonoBehaviour
{
    //declarations
    [SerializeField] private Animator _armAnimator;
    [SerializeField] private Footstepper _footstepper;

    [SerializeField] private TwoBoneIKConstraint _rightTwoBoneIK;
    [SerializeField] private TwoBoneIKConstraint _leftTwoBoneIK;
    [SerializeField] private Transform _rightArmTarget;
    [SerializeField] private Transform _leftArmTarget;
    [SerializeField] private MultiParentConstraint _rightHandRig;
    [SerializeField] private MultiParentConstraint _leftHandRig;
    [SerializeField] private Transform _rightHandTarget;
    [SerializeField] private Transform _leftHandTarget;

    [SerializeField] private Transform _rightShoulder;
    [SerializeField] private Transform _leftShoulder;
    [SerializeField] private Vector3 _rightArmContactPosition;
    [SerializeField] private Vector3 _leftArmContactPosition;
    [SerializeField] private float _reachDistance=1;
    [SerializeField] private LayerMask _obstaclesLayers;


    [SerializeField] private float _armTransitionSpeed = 0.2f;
    [SerializeField] private bool _isMovingForwardsAgainstWall;


    private FirstPersonController _playerController;
    private bool _isRunDetected = false;
    private float _startDelay;
    private bool _waitingToStart = false;
    private bool _isRightArmDetectingObstacle = false;
    private bool _isLeftArmDetectingObstacle = false;




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
        ManageRunAnimation();
        DetectWall();
    }



    //internals
    private void ManageRunAnimation()
    {
        ReadRunState();
        if (_isRunDetected)
            CalculateAnimationSpeed();
        else
            StopAnimation();
    }

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




    private Vector3 FindDetectionPoint(Vector3 origin, Vector3 direction, float distance, ArmSide side)
    {
        RaycastHit hit;
        Physics.SphereCast(origin,.5f,direction, out hit,distance,_obstaclesLayers);

        if (hit.collider == null)
        {
            Debug.DrawLine(origin, origin + direction * distance,Color.red);

            if (side == ArmSide.Right)
                _isRightArmDetectingObstacle = false;
            else if (side == ArmSide.Left)
                _isLeftArmDetectingObstacle= false;

            return Vector3.negativeInfinity;
        }
            
        else
        {
            if (side == ArmSide.Right)
                _isRightArmDetectingObstacle = true;
            else if (side == ArmSide.Left)
                _isLeftArmDetectingObstacle = true;

            Debug.DrawLine(origin, hit.point, Color.green);
            return hit.point;
        }
    }


    private void DetectWall()
    {
        _rightArmContactPosition = FindDetectionPoint(_rightShoulder.position, transform.TransformDirection(Vector3.forward), 1,ArmSide.Right);
        _leftArmContactPosition = FindDetectionPoint(_leftShoulder.position, transform.TransformDirection(Vector3.forward), 1, ArmSide.Left);

        if (_isRightArmDetectingObstacle)
        {
            _rightArmTarget.transform.position = _rightArmContactPosition;
            _rightHandTarget.rotation = Quaternion.Euler(0, -90, 180);
            _rightHandRig.weight = 1;
            _rightTwoBoneIK.weight = 1;
        }
        else
        {
            _rightHandRig.weight=0;
            _rightTwoBoneIK.weight = 0;
            _rightArmTarget.transform.position = transform.TransformVector(Vector3.zero);
            
        }

        if (_isLeftArmDetectingObstacle)
        {
            
            _leftArmTarget.transform.position = _leftArmContactPosition;
            _leftHandTarget.rotation = Quaternion.Euler(0, 90, -180);
            _leftHandRig.weight=-1;
            _leftTwoBoneIK.weight = 1;
        }
        else
        {
            _leftHandRig.weight=0;
            _leftTwoBoneIK.weight = 0;
            _leftArmTarget.transform.position = transform.TransformVector(Vector3.zero);
            
        }
    }



    //externals






}
