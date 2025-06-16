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
    private List<Vector3> _visualLedgePoints = new();
    [SerializeField] private float _clearVisualGizmosTick = .5f;
 
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

    [Header("Ledge Area Scanning")]
    [SerializeField] private float _castSize = .25f;
    [SerializeField] private float _castDistance = .33f;
    [SerializeField] private float _normalCastDistance = 1;
    private List<Vector3> _horizontalClimbLedgePoints = new();
    private RaycastHit _horizonClimbBodyDetection;



    //Monobehaviours

    private void Start()
    {
        PopulateDebugMarkers();
        InvokeRepeating(nameof(ClearVisualGizmos), _clearVisualGizmosTick, _clearVisualGizmosTick);
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (Vector3 point in _visualLedgePoints)
        {
            Gizmos.DrawWireSphere(point, _castSize);
        }
    }




    //Internals
    private void ClearVisualGizmos()
    {
        _visualLedgePoints.Clear();
    }

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
                bool isLedge = IsWallPointLedgeable(_detectedWallPoint);

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

    public Vector3 GetFaceNormal(Vector3 point, Vector3 castDirection)
    {
        RaycastHit detection;
        Vector3 startPoint = point + (-castDirection.normalized * .25f); //Move outwards a bit to not start the cast directly on the wall
        Physics.Raycast(startPoint, castDirection, out detection, _normalCastDistance, _wallLayers);
        
        if (detection.collider != null)
        {
            //Debug.DrawLine(point, point + detection.normal.normalized * 4, Color.magenta, .5f);
            return detection.normal.normalized;
        }
            
        else return Vector3.negativeInfinity;
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

    private bool IsWallPointLedgeable(Vector3 point)
    {
        //apply the spacing
        point += Vector3.up * _verticalLedgeSpacing * 2;
        point += transform.TransformDirection(Vector3.forward * _OntoLedgeSpacing);

        Collider[] hits = Physics.OverlapSphere(point,_ledgeCastRadius, _wallLayers);

        if (hits.Length == 0)
            return true;
        else return false;
    }

    private bool IsWallPointLedgeable(Vector3 point, Vector3 forwardsDirection)
    {
        //apply the spacing
        point += Vector3.up * _verticalLedgeSpacing * 2;
        point += forwardsDirection.normalized * _OntoLedgeSpacing;

        Collider[] hits = Physics.OverlapSphere(point, _ledgeCastRadius, _wallLayers);

        if (hits.Length == 0)
            return true;
        else return false;
    }

    public List<Vector3> ScanForLedgePointsAlongLine(Vector3 startPoint, Vector3 endPoint, Vector3 forwardsDirection, int numberOfCasts)
    {
        //ignore invalid points
        if (startPoint.x == float.NegativeInfinity || endPoint.x == float.NegativeInfinity || forwardsDirection.x == float.NegativeInfinity)
            return new List<Vector3>();

        int currentCastsMade = 0;
        List<Vector3> detectedLedges = new();
        Vector3 currentCastPoint;
        RaycastHit castDetection;
        Collider[] overlapDetections;
        bool isLedge = false;
        Vector3 hitPoint = Vector3.negativeInfinity;



        while (currentCastsMade < numberOfCasts)
        {
            //Debug.Log($"Entered Scan Interaion loop. Iteration: {currentCastsMade}");

            //reset the detection util
            isLedge = false;
            float time = (float)currentCastsMade / (float)numberOfCasts;
            hitPoint = Vector3.negativeInfinity;

            //for each iteration, move in even steps from the startPoint to the endPoint, in a straight line
            currentCastPoint = Vector3.Lerp(startPoint, endPoint, time);

            overlapDetections = Physics.OverlapSphere(currentCastPoint, _castSize, _wallLayers);
            Physics.SphereCast(currentCastPoint, _castSize, forwardsDirection.normalized, out castDetection, _castDistance, _wallLayers);
            //Debug.DrawLine(currentCastPoint, currentCastPoint + (forwardsDirection * _castDistance * 5), Color.magenta, .5f);

            

            //first check the overlap. Was anything detected?
            if (overlapDetections.Length > 0)
            {
                //make sure the point is a ledge
                hitPoint = overlapDetections[0].ClosestPoint(currentCastPoint);
                if (IsWallPointLedgeable(hitPoint, forwardsDirection))
                {
                    //set for drawing the debug points later
                    isLedge = true;


                    //add the point if it isn't already added
                    if (!detectedLedges.Contains(hitPoint))
                        detectedLedges.Add(hitPoint);

                }
            }

            //did we detect anything with the cast?
            else if (castDetection.collider != null)
            {
                //make sure the point is a ledge
                hitPoint = castDetection.point;
                if (IsWallPointLedgeable(hitPoint, forwardsDirection))
                {
                    //set for drawing the debug points later
                    isLedge = true;


                    //add the point if it isn't already added
                    if (!detectedLedges.Contains(hitPoint))
                        detectedLedges.Add(hitPoint);

                }
            }

            if (_showDebug && hitPoint.x != float.NegativeInfinity && isLedge)
            {
                if (!_visualLedgePoints.Contains(hitPoint))
                    _visualLedgePoints.Add(hitPoint);
            }

            //update our step
            currentCastsMade++;

        }

        return detectedLedges;
        
    }

    public bool IsHorizontalClimbingAvailable(Vector3 climbDirection, float castDistance, Vector3 forwardsDirection, Vector3 ledgeOrigin, out List<Vector3> foundLedgePoints)
    {
        
        //First determine if there's space for the player's body to move in the climb direction
        Physics.CapsuleCast(_bottomPlayerCapsuleOrigin.position, _topPlayerCapsuleOrigin.position, _capsuleRadius, climbDirection,out _horizonClimbBodyDetection, castDistance, _wallLayers);
        _horizontalClimbLedgePoints.Clear();
        foundLedgePoints = _horizontalClimbLedgePoints; //we're doing this to avoid creating the creation of new lists. This might be executed each frame

        //return false if any collider was detected
        if (_horizonClimbBodyDetection.collider!= null)
            return false;

        //Next, determine if there's any ledgePoints within the castDistance's span
        _horizontalClimbLedgePoints = ScanForLedgePointsAlongLine(ledgeOrigin, ledgeOrigin + climbDirection.normalized * castDistance, forwardsDirection, 3);

        if (_horizontalClimbLedgePoints.Count > 0)
        {
            Debug.Log($"found LedgePoints while checking wallClimb avaialability: {_horizontalClimbLedgePoints.Count}");
            foundLedgePoints = _horizontalClimbLedgePoints;
            return true;
        }
            
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
