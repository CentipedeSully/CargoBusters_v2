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
    [SerializeField] private Transform _rightArmHint;
    [SerializeField] private Transform _leftArmHint;
    [SerializeField] private Transform _rightShoulder;
    [SerializeField] private Transform _leftShoulder;
    [SerializeField] private float _againstWallTargetTweak = 0.5f;
    [SerializeField] private Transform _againstWallRightHintPosition;
    [SerializeField] private Transform _againstWallLeftHintPosition;
    [SerializeField] private LayerMask _obstaclesLayers;
    [SerializeField] private float _transitionDuration = .5f;
    
    
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
        PositionHandsAgainstWall();
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




    private RaycastHit CastDetection(Vector3 origin, Vector3 direction, float distance, ArmSide side)
    {
        RaycastHit hit;
        Physics.SphereCast(origin,.1f,direction, out hit,distance,_obstaclesLayers);

        if (hit.collider == null)
        {
            Debug.DrawLine(origin, origin + direction * distance,Color.red);

            if (side == ArmSide.Right)
                _isRightArmDetectingObstacle = false;
            else if (side == ArmSide.Left)
                _isLeftArmDetectingObstacle= false;
        }
            
        else
        {
            if (side == ArmSide.Right)
                _isRightArmDetectingObstacle = true;
            else if (side == ArmSide.Left)
                _isLeftArmDetectingObstacle = true;

            Debug.DrawLine(origin, hit.point, Color.green);
        }

        return hit;
    }


    private void PositionHandsAgainstWall()
    {

        RaycastHit rightDetection = CastDetection(_rightShoulder.position, transform.TransformDirection(Vector3.forward + (Vector3.up * _againstWallTargetTweak)), 1, ArmSide.Right);
        RaycastHit leftDetection = CastDetection(_leftShoulder.position, transform.TransformDirection(Vector3.forward + (Vector3.up * _againstWallTargetTweak)), 1, ArmSide.Left);


        if (_isRightArmDetectingObstacle && _playerController.IsForwardsPressed())
        {
            _rightArmHint.position = _againstWallRightHintPosition.position;
            _rightArmTarget.transform.position = rightDetection.point;

            Vector3 rightHandRotation = new Vector3(0, 0, 180);
            _rightArmTarget.rotation = Quaternion.Euler(rightHandRotation);
            _rightTwoBoneIK.weight += Time.deltaTime * _transitionDuration;
        }
        else
        {
            _rightTwoBoneIK.weight -= Time.deltaTime * _transitionDuration;
        }

        if (_isLeftArmDetectingObstacle && _playerController.IsForwardsPressed())
        {
            _leftArmHint.position = _againstWallLeftHintPosition.position;
            _leftArmTarget.transform.position = leftDetection.point;

            Vector3 leftHandRotation = Vector3.zero;
            _leftArmTarget.rotation = Quaternion.Euler(leftHandRotation);
            _leftTwoBoneIK.weight += Time.deltaTime * _transitionDuration;
        }
        else
        {
            _leftTwoBoneIK.weight -= Time.deltaTime * _transitionDuration;
        }


        


    }



    //externals






}
