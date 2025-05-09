using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public enum LedgeType
{
    unset,
    high,
    mid,
    low
}
public class LedgeDetectionManager : MonoBehaviour
{
    //Declarations
    [Header("Detection Area Settings")]
    [Tooltip("The necessary empty volume that should be above a valid ledge")]
    [SerializeField] private Vector3 _validOpeningSize;
    [Tooltip("The size of the ledgeCast. Anything detected within this area will represent a possible ledge")]
    [SerializeField] private Vector3 _ledgeCastSize;
    [SerializeField] private Transform _highLedgeDetectionCenter;
    [SerializeField] private LedgeDetecter _highDetector;
    [SerializeField] private Vector3 _highDetectionSize;
    [SerializeField] private Transform _midLedgeDetectionCenter;
    [SerializeField] private LedgeDetecter _midDetector;
    [SerializeField] private Vector3 _midDetectionSize;
    [SerializeField] private Transform _lowLedgeDetectionCenter;
    [SerializeField] private LedgeDetecter _lowDetector;
    [SerializeField] private Vector3 _lowDetectionSize;

    [Header("Status")]
    [SerializeField] private bool _isDetectionActive = false;
    private Vector3 _ledgePosition;
    private bool _highLedgeDetected = false;
    private bool _midLedgeDetected = false;
    private bool _lowLedgeDetected = false;

    [Header("Gizmo Settings")]
    [SerializeField] private Color _inactiveColor;
    [SerializeField] private Color _missColor;
    [SerializeField] private Color _highAreaColor;
    [SerializeField] private Color _midAreaColor;
    [SerializeField] private Color _lowAreaColor;
    
    [SerializeField] private DrawRectGizmo _highGizmo;
    [SerializeField] private DrawRectGizmo _midGizmo;
    [SerializeField] private DrawRectGizmo _lowGizmo;


    public delegate void LedgeDetectionEvent(LedgeType type, Vector3 position);
    public event LedgeDetectionEvent OnLedgeDetected;


    //Monobehviours
    private void OnEnable()
    {
        _highDetector.OnLedgeDetected += ListenForHighLedge;
        _midDetector.OnLedgeDetected += ListenForMidLedge;
        _lowDetector.OnLedgeDetected += ListenForLowLedge;
    }

    private void OnDisable()
    {
        _highDetector.OnLedgeDetected -= ListenForHighLedge;
        _midDetector.OnLedgeDetected -= ListenForMidLedge;
        _lowDetector.OnLedgeDetected -= ListenForLowLedge;
    }

    private void OnDrawGizmos()
    {
        DrawDetectionAreas();
    }

    private void Update()
    {
        UpdateDetectors();
        DetectForObstructions();
    }


    //Internals
    private void UpdateDetectors()
    {
        //make sure all sizes settings are reflected correct
        if (_highGizmo != null)
        {
            _highGizmo.SetSize(_highDetectionSize);
            _highDetector.UpdateCastSettings(_highDetectionSize, _ledgeCastSize, _validOpeningSize);
        }

        if (_midGizmo != null)
        {
            _midGizmo.SetSize(_midDetectionSize);
            _midDetector.UpdateCastSettings(_midDetectionSize, _ledgeCastSize, _validOpeningSize);
        }

        if (_lowGizmo != null)
        {
            _lowGizmo.SetSize(_lowDetectionSize);
            _lowDetector.UpdateCastSettings(_lowDetectionSize, _ledgeCastSize, _validOpeningSize);
        }
    }
    private void DetectForObstructions()
    {
        if (_isDetectionActive)
        {
            _highLedgeDetected = false;
            _midLedgeDetected = false;
            _lowLedgeDetected = false;

            _highGizmo.SetColor(_missColor);
            _midGizmo.SetColor(_missColor);
            _lowGizmo.SetColor(_missColor);

            _highDetector.HidePositionGizmo();
            _midDetector.HidePositionGizmo();
            _lowDetector.HidePositionGizmo();


            if (_highDetector.IsObstructionDetected())
            {
                _highGizmo.SetColor(_highAreaColor);

                _highDetector.DetectLedge();
                if (_highLedgeDetected)
                {
                    OnLedgeDetected?.Invoke(LedgeType.high, _ledgePosition);
                    return;
                }
            }

            
            if (_midDetector.IsObstructionDetected())
            {
                _midGizmo.SetColor(_midAreaColor);

                _midDetector.DetectLedge();
                if (_midLedgeDetected)
                {
                    OnLedgeDetected?.Invoke(LedgeType.mid, _ledgePosition);
                    return;
                }
            }


            if (_lowDetector.IsObstructionDetected())
            {
                _lowGizmo.SetColor (_lowAreaColor);

                _lowDetector.DetectLedge();
                if (_lowLedgeDetected)
                {
                    OnLedgeDetected?.Invoke(LedgeType.low, _ledgePosition);
                    return;
                }
            }
        }
    }

    private void ListenForHighLedge(Vector3 ledgePosition)
    {
        _highLedgeDetected = true;
        _ledgePosition = ledgePosition;
    }
    private void ListenForMidLedge(Vector3 ledgePosition)
    {
        _midLedgeDetected = true;
        _ledgePosition = ledgePosition;
    }
    private void ListenForLowLedge(Vector3 ledgePosition)
    {
        _lowLedgeDetected = true;
        _ledgePosition = ledgePosition;
    }



    //Externals
    public void ActivateDetection(bool newState)
    {
        _isDetectionActive = newState;
    }

    public bool IsLedgeStillValid(LedgeType type, Vector3 position)
    {
        switch(type)
        {
            case LedgeType.high:
                if (Vector3.Distance(_highDetector.transform.position, position) > _highDetector.GetMaxLedgeDistance())
                    return false;
                else return true;

            case LedgeType.mid:
                if (Vector3.Distance(_midDetector.transform.position, position) > _midDetector.GetMaxLedgeDistance())
                    return false;
                else return true;

            case LedgeType.low:
                if (Vector3.Distance(_lowDetector.transform.position, position) > _lowDetector.GetMaxLedgeDistance())
                    return false;
                else return true;

            default:
                return false;
        }
    }



    //Gizmo 
    private void DrawDetectionAreas()
    {
        //reflect the proper colors based on the activity and status
        if (!_isDetectionActive)
        {
            _highGizmo.SetColor(_inactiveColor);
            _midGizmo.SetColor(_inactiveColor);
            _lowGizmo.SetColor(_inactiveColor);
        }
        
        
    }


}
