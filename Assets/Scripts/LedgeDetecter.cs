using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class LedgeDetecter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _parentRotationObject;
    [SerializeField] private LedgeDetectionManager _detectionManager;
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private DrawRectGizmo _ledgeCastGizmo;
    [SerializeField] private DrawRectGizmo _minimalLedgeSpaceGizmo;

    //[SerializeField] private float _detectionPointGizmoSize;


    [Header("Ledge Parameters")]
    [Tooltip("The shape that'll be used for ledge detection. " +
        "Anything that makes contact with this will be tested for 'ledge'ibiliy")]
    [SerializeField] private Vector3 _ledgeCastSize;
    [Tooltip("The minimal open area that the ledge should provide to be considered a ledge")]
    [SerializeField] private Vector3 _minimalOpenSpaceSize;
    [SerializeField] private float _maxLedgeDistanceFromDetector = .5f;


    [Header("Gizmo Settings")]
    [SerializeField] bool _showPositionGizmo = false;
    [Tooltip("The color of the cast visual during playmode. Keep it invisible unless you need to know its size.")]
    [SerializeField] private Color _runtimeLedgeCastColor;
    [SerializeField] private Color _validOpeningColor;
    [SerializeField] private Color _invalidOpeningColor;



    private Vector3 _detectionAreaSize;
    private Vector3 _castStart;
    private float _castDistance;
    private Collider[] _openSpaceDetections;
    private RaycastHit[] _castResults;

    public delegate void LedgeDetectedEvent(Vector3 position);
    public event LedgeDetectedEvent OnLedgeDetected;
    public event LedgeDetectedEvent OnNoLedgeDetected;



    //monobehaviours
    private void Start()
    {
        _minimalLedgeSpaceGizmo.SetColor(_invalidOpeningColor);
    }

    private void Update()
    {
        if (_showPositionGizmo)
            ShowPositionGizmo();
        else HidePositionGizmo();
    }

    //Internals



    //externals
    public void UpdateCastSettings(Vector3 detectionAreaHeight, Vector3 ledgeCastSize, Vector3 minimalLedgeSpaceSize)
    {
        _detectionAreaSize = detectionAreaHeight;

        _ledgeCastSize = ledgeCastSize;
        _ledgeCastGizmo.SetSize(_ledgeCastSize);

        if (Application.isPlaying == true)
        {
            _ledgeCastGizmo.SetColor(_runtimeLedgeCastColor);
        }
            

        _minimalOpenSpaceSize = minimalLedgeSpaceSize;
        _minimalLedgeSpaceGizmo.SetSize(_minimalOpenSpaceSize);
    }

    public void DetectLedge()
    {
        _castResults = null;
        _openSpaceDetections = null;
        HidePositionGizmo();
        _minimalLedgeSpaceGizmo.SetPosition(transform.position);

        //update cast height
        _castStart = transform.position;
        _castStart.y += (_detectionAreaSize.y / 2) - (_ledgeCastSize.y / 2);

        //update cast distance
        _castDistance = _detectionAreaSize.y - _ledgeCastSize.y;


        //perform the ledgecast to find the highest reach
        _castResults = Physics.BoxCastAll(_castStart, _ledgeCastSize / 2, transform.TransformDirection(Vector3.down), _parentRotationObject.rotation, _castDistance, _groundLayers);

        if (_castResults.Length > 0)
        {
            /* Debug what's being hit
            string resultsString = $"{gameObject.name} Detector Cast Results: \n";
            foreach (RaycastHit result in _castResults)
                resultsString += result.collider.name + '\n';

            Debug.Log(resultsString); 
            */
            
            //determine if any opening space is at the ledge's position
            Vector3 openingCastOrigin = _castResults[0].point;

            if (Vector3.Distance(transform.position, openingCastOrigin) > _maxLedgeDistanceFromDetector)
            {
                //Debug.Log("Ignoring cast result. too far from detector");
                return;
            }


            //offset the openingCast's starting height by half it's height
            openingCastOrigin.y += (_minimalOpenSpaceSize.y / 2) + 0.01f;

            _openSpaceDetections = Physics.OverlapBox(openingCastOrigin, _minimalOpenSpaceSize/2, _parentRotationObject.rotation, _groundLayers);


            if (_openSpaceDetections.Length == 0)
            {
                _minimalLedgeSpaceGizmo.SetPosition(openingCastOrigin);
                ShowPositionGizmo();
                OnLedgeDetected?.Invoke(openingCastOrigin);
            }
        }
    }

    public bool IsObstructionDetected()
    {
        
        return Physics.CheckBox(transform.position, _detectionAreaSize / 2,_parentRotationObject.rotation,_groundLayers);
    }

    public void ShowPositionGizmo()
    {
        _minimalLedgeSpaceGizmo.SetColor(_validOpeningColor);
    }

    public void HidePositionGizmo()
    {
        _minimalLedgeSpaceGizmo.SetColor(_invalidOpeningColor);
    }

    public float GetMaxLedgeDistance()
    {
        return _maxLedgeDistanceFromDetector;
    }

}
