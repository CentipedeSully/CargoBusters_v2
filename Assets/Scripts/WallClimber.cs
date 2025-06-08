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

    [Header("Debug Visuals")]
    [SerializeField] private bool _showDebug = false;
    [SerializeField] private GameObject _debugMarkerPrefab;
    [SerializeField] private GameObject _standableMarkerPrefab;
    [SerializeField] private Transform _debugMarkersContainer;
    [SerializeField] private int _maxMarkers = 20;
    private List<GameObject> _debugMarkers = new();
    private List<GameObject> _debugStandableMarkers = new();
 
    [Header("Forwards Wall Detection")]
    [Tooltip("Wall casting scans forwards in steps, from the player's max ledge reach down to a specified cutoff." +
        "Smaller values improve accuracy at the cost of performance. This calculation happens many times each frame " +
        "if a wall has been detected.")]
    [SerializeField] private float _verticalCastStepSize = .05f;
    [Tooltip("Wall casting stops once this position is passed over." +
        "Useful to ignore easily-traverseable low terrain")]
    [SerializeField] private Transform _lowestWallDetectionBound;
    [SerializeField] private float _forwardsWallCastDistance;
    [SerializeField] private Vector3 _detectedWallPoint = Vector3.negativeInfinity;
    private RaycastHit[] _wallDetectionResults;

    [Header("Standable Ledge Detection")]
    [Tooltip("The necessary space upwards that the standCast will be performed from the potential ledge point.")]
    [SerializeField] private float _verticalLedgeSpacing = 0.1f;
    [Tooltip("The space forwards ONTO THE LEDGE that the standCast will be performed from the potential ledge point." +
        "Emulates how far the player would transition onto the ledge")]
    [SerializeField] private float _OntoLedgeSpacing = 0.1f;


    private RaycastHit[] _ledgeDetectionResults;





    //Monobehaviours

    private void Start()
    {
        PopulateDebugMarkers();
    }

    private void Update()
    {
        if (_isWallClimbingEnabled)
        {
            if (_showDebug)
                ClearMarkers();

            if (IsObstacleDetected())
                CreateForwardsObstacleAnalysis();
            
        }
            
    }




    //Internals
    private void PopulateDebugMarkers()
    {
        if (_showDebug)
        {
            //create a large amount of wall detection markers
            int markerCount = _debugMarkers.Count;
            GameObject latestCreatedObject;
            while (markerCount < _maxMarkers)
            {
                latestCreatedObject = Instantiate(_debugMarkerPrefab, _debugMarkersContainer);
                latestCreatedObject.SetActive(false);
                _debugMarkers.Add(latestCreatedObject);
                markerCount++;
            }

            //create a few standable markers
            for (int count = 0; count < 3; count++)
            {
                latestCreatedObject = Instantiate(_standableMarkerPrefab, _debugMarkersContainer);
                latestCreatedObject.SetActive(false);
                _debugStandableMarkers.Add(latestCreatedObject);
            }
        }
        
    }

    private void ClearMarkers()
    {
        foreach (GameObject marker in _debugMarkers)
            marker.SetActive(false);

        foreach (GameObject marker in _debugStandableMarkers)
            marker.SetActive(false);
    }

    private void WallCastForwards(Vector3 start, float size)
    {
        _wallDetectionResults = new RaycastHit[_detectionArraySize];
        Physics.SphereCastNonAlloc(start, size, transform.TransformDirection(Vector3.forward), _wallDetectionResults, _forwardsWallCastDistance, _wallLayers);
    }

    
    private bool IsObstacleDetected() //Performs a cheap cast to determine if we need to build an obstacle analysis 
    {
        return Physics.CapsuleCast(_maxLedgeReachOrigin.position, _bottomPlayerCapsuleOrigin.position, _capsuleRadius, transform.TransformDirection(Vector3.forward), _forwardsWallCastDistance, _wallLayers);
    }

    private void CreateForwardsObstacleAnalysis() //performs a relatively expensive analysis of any obstacles ahead of the entitiy
    {
        

        //Start detecting walls from the highest point first
        Vector3 currentCastOrigin = _maxLedgeReachOrigin.position;
        int iterationCount = 1;

        while (currentCastOrigin.y > _lowestWallDetectionBound.position.y)
        {
            //perform the cast
            WallCastForwards(currentCastOrigin, _capsuleRadius);

            //Get the detection that's closest to our origin
            _detectedWallPoint = Utils.GetClosestPoint(_wallCastOrigin.position, _wallDetectionResults);

            

            if (_showDebug)
            {
                //tag the hit with a debug marker, if it's a valid detection
                if (_detectedWallPoint.x != float.NegativeInfinity &&               //existence check
                    _detectedWallPoint.y >= _lowestWallDetectionBound.position.y && //lower bound check
                    _detectedWallPoint.y <= _maxLedgeReachOrigin.position.y)     //upper bound check
                {
                    //make sure we have enough markers.
                    if (iterationCount <= _debugMarkers.Count)
                    {
                        //tag the collision point with a marker
                        _debugMarkers[iterationCount - 1].transform.position = _detectedWallPoint;
                        _debugMarkers[iterationCount - 1].SetActive(true);
                    }

                    //test the point for standability
                    bool isPointStandable = TestPointForStandability(_detectedWallPoint);
                    
                    
                    if (isPointStandable)
                    {
                        //apply spacing to the point
                        Vector3 position = _detectedWallPoint + (Vector3.up * _verticalLedgeSpacing) + (transform.TransformDirection(Vector3.forward * _OntoLedgeSpacing));

                        //offset the point by half the player's height
                        position += Vector3.up * ((_topPlayerCapsuleOrigin.position.y - _bottomPlayerCapsuleOrigin.position.y)/2 + _capsuleRadius);

                        _debugStandableMarkers[0].transform.position = position;
                        _debugStandableMarkers[0].SetActive(true);
                    }
                    
                }
            }
            

            //setp the cast origin down for the next cast
            currentCastOrigin.y -= _verticalCastStepSize;

            //increment iteration count, used by the debug marker collection
            iterationCount++;

        }

    }

    private bool TestPointForStandability(Vector3 point)
    {
        //apply the spacing
        point += Vector3.up * _verticalLedgeSpacing;
        point += transform.TransformDirection(Vector3.forward * _OntoLedgeSpacing);

        Vector3 FeetPosition = point + (Vector3.up * _capsuleRadius);
        Vector3 TopPosition = point + (Vector3.up * (_topPlayerCapsuleOrigin.position.y - _bottomPlayerCapsuleOrigin.position.y + _capsuleRadius));

        //create an offset cast
        Collider[] hits = Physics.OverlapCapsule(FeetPosition, TopPosition, _capsuleRadius, _wallLayers);

        if (hits.Length == 0)
            return true;
        else return false;
    }

    private void TestPointForLedgeability(Vector3 point)
    {

    }




    //Externals




}
