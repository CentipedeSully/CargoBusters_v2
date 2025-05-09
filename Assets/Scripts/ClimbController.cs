using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;



public enum ClimbState
{
    Unset,
    NotClimbing,
    LowTransition,
    MidTransition,
    HighEnterHang,
    HighHangIdle,
    HighHangHorizontalMovement,
    HighHangVerticalMovement,
    HighHangExit

}

public class ClimbController : MonoBehaviour
{
    //declarations
    [SerializeField] private CharacterController _charController;
    [SerializeField] private ClimbState _currentClimbState = ClimbState.Unset;
    [SerializeField] private Vector3 _endPoint = Vector3.zero;
    [SerializeField] private float _minimumTransitionSpeed = 2f;
    [SerializeField] private float _transitionSpeed;

    [SerializeField] private float _totalDistanceToEndPoint;
    [SerializeField] private float _currentDistanceTraversed;


    //private Vector3[] transitionPoints; // for multipoint transitions




    //Monobehaviours
    private void Start()
    {
        _currentClimbState = ClimbState.NotClimbing;
        
    }

    private void Update()
    {
        ManageTransition();
    }


    //Internals
    private void ManageTransition()
    {
        if (_currentClimbState == ClimbState.LowTransition)
        {
            if (_charController.transform.position == _endPoint || _currentDistanceTraversed >= _totalDistanceToEndPoint)
            {
                _currentClimbState = ClimbState.NotClimbing;
            }
            else
            {
                if (transform.position != _endPoint && _charController != null)
                {
                    Vector3 moveVector = (_endPoint - transform.position).normalized * _transitionSpeed * Time.deltaTime;
                    _charController.Move(moveVector);
                    _currentDistanceTraversed += moveVector.magnitude;
                }
            }
        }
        
    }



    //Externals
    public ClimbState GetClimbState()
    {
        return _currentClimbState;
    }

    public void EnterLowTransition(Vector3 endPoint, float speed)
    {
        if (_currentClimbState == ClimbState.NotClimbing)
        {
            _endPoint = endPoint;
            Debug.Log($"Start point: {transform.position}/nEnp Point: {_endPoint}");

            _transitionSpeed = Mathf.Max(speed, _minimumTransitionSpeed);
            _currentClimbState = ClimbState.LowTransition;

            _totalDistanceToEndPoint = Vector3.Distance(transform.position,_endPoint);
            _currentDistanceTraversed = 0;
        }
        

    }





}
