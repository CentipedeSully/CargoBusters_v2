using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;


public enum LedgeType
{
    unset,
    high,
    mid,
    low
}


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
    [SerializeField] private GameObject _wallMarkerPrefab;
    [SerializeField] private GameObject _standableMarkerPrefab;
    [SerializeField] private GameObject _ledgeMarkerPrefab;
    [SerializeField] private Transform _debugMarkersContainer;
    [SerializeField] private int _maxWallMarkers = 20;
    [SerializeField] private int _maxLedgeMarkers = 20;
    private List<GameObject> _debugMarkers = new();
    private List<GameObject> _debugStandableMarkers = new();
    private List<GameObject> _debugLedgeMarkers = new();
    private int _usedWallMarkers = 0;
    private int _usedLedgeMarkers = 0;
 
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

    [Header("Ledge Point Detection")]
    [Tooltip("The spherical space that must exist above an obstacle to be considered a ledgeable position")]
    [SerializeField] private float _ledgeCastRadius = .1f;
    [SerializeField] private Transform _ledgeLevelParent;
    [Tooltip("Ledge Points at or beneath this height are considered low")]
    [SerializeField] private Transform _lowLedgeCutoff;
    [Tooltip("Ledge Points at or above this height are considered high")]
    [SerializeField] private Transform _HighLedgeCutoff;
    [SerializeField] private List<Vector3> _ledgePositions = new();





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

            _ledgePositions.Clear();

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
            while (markerCount < _maxWallMarkers)
            {
                latestCreatedObject = Instantiate(_wallMarkerPrefab, _debugMarkersContainer);
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

            //create a large amount of ledge detection markers
            markerCount = _debugLedgeMarkers.Count;
            while (markerCount < _maxLedgeMarkers)
            {
                latestCreatedObject = Instantiate(_ledgeMarkerPrefab, _debugMarkersContainer);
                latestCreatedObject.SetActive(false);
                _debugLedgeMarkers.Add(latestCreatedObject);
                markerCount++;
            }
        }
        
    }

    private void ClearMarkers()
    {
        foreach (GameObject marker in _debugMarkers)
            marker.SetActive(false);

        foreach (GameObject marker in _debugStandableMarkers)
            marker.SetActive(false);

        foreach (GameObject marker in _debugLedgeMarkers)
            marker.SetActive(false);

        _usedLedgeMarkers = 0;
        _usedWallMarkers = 0;
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

        while (currentCastOrigin.y > _lowestWallDetectionBound.position.y)
        {
            //perform the cast
            WallCastForwards(currentCastOrigin, _capsuleRadius);

            //Get the detection that's closest to our origin
            Vector3 closestPoint = Utils.GetClosestPoint(_wallCastOrigin.position, _wallDetectionResults);

            //Validate the point before building our analysis
            if (closestPoint.x != float.NegativeInfinity &&               //existence check
                    closestPoint.y >= _lowestWallDetectionBound.position.y && //lower bound check
                    closestPoint.y <= _maxLedgeReachOrigin.position.y)     //upper bound check
            {
                //save the valid point
                _detectedWallPoint = closestPoint;

                //test point for ledgeability (meaning is it a grab point)
                bool isLedge = IsPointLedgeable(_detectedWallPoint);

                if (isLedge)
                {
                    //save the ledge point if it isn't already saved
                    if (!_ledgePositions.Contains(_detectedWallPoint))
                        _ledgePositions.Add(_detectedWallPoint);

                    DetermineLedgeType(_detectedWallPoint);
                }


                //draw the debug visuals
                if (_showDebug)
                {
                    /*Completed Standability Mechanic
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
                    */

                    //reflect the visual as a ledge point if it's a ledge point
                    if (isLedge)
                    {
                        //make sure we have enough free ledge markers
                        if (_usedLedgeMarkers < _debugLedgeMarkers.Count)
                        {
                            //tag the collision point with a Ledge marker
                            _debugLedgeMarkers[_usedLedgeMarkers].transform.position = _detectedWallPoint;
                            _debugLedgeMarkers[_usedLedgeMarkers].SetActive(true);
                            _usedLedgeMarkers++;
                        }

                        else Debug.LogWarning("Ran out of ledge Markers to display all detected ledges. Some may not be reflected visually");
                    }

                    //otherwise make its visual a regular wall point
                    else
                    {
                        //make sure we have enough wall points
                        if (_usedWallMarkers < _debugMarkers.Count)
                        {
                            //tag the collision point with a marker
                            _debugMarkers[_usedWallMarkers].transform.position = _detectedWallPoint;
                            _debugMarkers[_usedWallMarkers].SetActive(true);
                            _usedWallMarkers++;

                        }

                        else Debug.LogWarning("Ran out of wall Markers to display all detected wall collisions. Some may not be reflected visually");
                    }
                }



            }


            //setp the cast origin down for the next cast
            currentCastOrigin.y -= _verticalCastStepSize;

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

    private bool IsPointLedgeable(Vector3 point)
    {
        //apply the spacing
        point += Vector3.up * _verticalLedgeSpacing * 2;
        point += transform.TransformDirection(Vector3.forward * _OntoLedgeSpacing);

        Collider[] hits = Physics.OverlapSphere(point,_ledgeCastRadius, _wallLayers);

        if (hits.Length == 0)
            return true;
        else return false;
    }




    //Externals
    public bool IsPointStandable(Vector3 point)
    {
        return TestPointForStandability(point);
    }

    public bool IsLedgeAvailable()
    {
        return _ledgePositions.Count > 0;
    }

    public Vector3 GetClosestLedgePoint()
    {
        if (_ledgePositions.Count < 1)
            return Vector3.negativeInfinity;
        else
        {
            return Utils.GetClosestPoint(transform.position, _ledgePositions);
        }
    }

    public LedgeType DetermineLedgeType(Vector3 point)
    {
        if (point.y == float.NegativeInfinity)
        {
            Debug.LogError("Attempted to calculate a ledgeType for an invalid point. returning UNSET as the ledgeType");
            return LedgeType.unset;
        }

        //make the ledge local
        point = _ledgeLevelParent.InverseTransformPoint(point);
        //Debug.Log($"Localized Point: {point}");



        if (point.y <= _lowLedgeCutoff.localPosition.y)
        {
            //Debug.Log($"Low, {point.y}\n At Or Below {_lowLedgeCutoff.localPosition.y}");
            return LedgeType.low;
        }

        else if (point.y >= _HighLedgeCutoff.localPosition.y)
        {
            //Debug.Log($"High, {point.y}\n At Or Above {_HighLedgeCutoff.localPosition.y}");
            return LedgeType.high;
        }

        else
        {
            //Debug.Log($"Mid, {point.y}\n Between {_lowLedgeCutoff.localPosition.y} and {_HighLedgeCutoff.localPosition.y}");
            return LedgeType.mid;
        }
    }

}
