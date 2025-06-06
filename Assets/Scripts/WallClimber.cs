using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class WallClimber : MonoBehaviour
{
    //Declarations
    [Header("General Settings")]
    [SerializeField] private LayerMask _wallLayers;
    [SerializeField] private Transform _wallCastOrigin;
    [SerializeField] private bool _isWallClimbingEnabled = true;
    [SerializeField] private Transform _maxLedgeReachOrigin;
    [SerializeField] private Transform _topPlayerCapsuleOrigin;
    [SerializeField] private Transform _bottomPlayerCapsuleOrigin;
    [SerializeField] private float _capsuleRadius = .5f;
    private int _detectionArraySize = 10;

    [Header("Forwards Wall Detection")]
    [SerializeField] private float _forwardsWallCastDistance;
    [SerializeField] private Vector3 _detectedWallPoint = Vector3.negativeInfinity;
    private RaycastHit[] _wallDetectionResults;

    [Header("standable Ledge Detection")]

    [Tooltip("The highest point that a standable position may be checked." +
        " No standablility checks are made beyond this height. The cast's size is" +
        "the player's bottom + top capsule by the capsule's radius")]
    [SerializeField] private Transform _highestStandableOrigin;
    
    [Tooltip("The lowest point that a standable position may be checked. " +
        "Any detections that're under this point will be ignored. They can be " +
        "navigated via normal movement")]
    [SerializeField] private Transform _lowestLedgeOrigin;
    
    [Tooltip("how far the cast should step up the detected wall to find a standable position. " +
        "Smaller values improve accuracy at the cost of performance. This calculation happens every frame " +
        "if a wall has been detected.")]
    [SerializeField] private float _castStepSize = .05f;

    private RaycastHit[] _ledgeDetectionResults;





    //Monobehaviours
    private void Update()
    {
        if (_isWallClimbingEnabled)
        {
            DetectWall();
            FindStandableLedge();
        }
            
    }




    //Internals
    private void DetectWall()
    {
        _wallDetectionResults = new RaycastHit[_detectionArraySize];

        //Wall Detect forwards from the player's feet to the player's highest ledge-grab reach
        Physics.CapsuleCastNonAlloc(_maxLedgeReachOrigin.position,_bottomPlayerCapsuleOrigin.position, _capsuleRadius, transform.TransformDirection(Vector3.forward), _wallDetectionResults, _forwardsWallCastDistance, _wallLayers);

        //Get the detection that's closest to our origin
        _detectedWallPoint = Utils.GetClosestPoint(_wallCastOrigin.position, _wallDetectionResults);
    }

    private void FindStandableLedge()
    {
        //validate a wall has been detected
        if (_detectedWallPoint != Vector3.negativeInfinity)
        {
            
        }
    }




    //Externals




}
