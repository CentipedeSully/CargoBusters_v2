using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class LedgeDetecter : MonoBehaviour
{
    [Header("Detecting Ledges")]
    [SerializeField] private Transform _playerRotatingObject;
    [SerializeField] private bool _openSpaceDetected = false;
    private bool _wallDetected = false;
    [SerializeField] private bool _isLedgeDetected = false;

    [Tooltip("The starting, highest point the player can reach onto")]
    [SerializeField] private Transform _ledgeStart;
    [Tooltip("The minimal open area that the ledge should provide to be considered a climbable ledge")]
    [SerializeField] private Vector3 _openingSize;
    private Vector3 _downcastStart;
    private Vector3 _downcastCollisionPosition;
    private Vector3 _openSpacePosition;
    [Tooltip("The solid ground detection area")]
    [SerializeField] private Vector3 _downcastSize;
    [Tooltip("How far down should the cast look for a solid ledge position")]
    [SerializeField] private float _maxDowncastDistance;
    [SerializeField] private LayerMask _climbLayers;
    private Collider[] _openSpaceDetections;
    private RaycastHit _downCastDetection;

    [Header("Gizmo Settings")]
    [SerializeField] private bool _drawLedgeDetectionGizmos = true;
    [SerializeField] private DrawRectGizmo _openAreaGizmo;
    [SerializeField] private DrawRectGizmo _downcastStartGizmo;
    [SerializeField] private DrawRectGizmo _upperBoundGizmo;
    [SerializeField] private DrawRectGizmo _lowerBoundGizmo;
    [SerializeField] private Color _boundsColor = Color.magenta;
    [SerializeField] private Color _successColor = Color.green;
    [SerializeField] private Color _failureColor = Color.red;






    //monobehaviours
    private void Update()
    {
        LedgeCast();
    }


    private void OnDrawGizmos()
    {
        if (_drawLedgeDetectionGizmos)
            DrawLedgeGizmos();
    }




    //Internals
    private void LedgeCast() 
    {
        //offset the start by the sum of (half the openspace size.y) and (half downcast size.y)
        _downcastStart = _ledgeStart.position + new Vector3(0, -_openingSize.y / 2, 0) + new Vector3(0, -_downcastSize.y / 2, 0);


        //detect for the 'ledge' object
        Physics.BoxCast(_downcastStart, _downcastSize / 2, Vector3.down, out _downCastDetection, _playerRotatingObject.rotation, _maxDowncastDistance, _climbLayers);
        if (_downCastDetection.collider != null)
        {
            _wallDetected = true;
            _downcastCollisionPosition = new Vector3(_downcastStart.x, _downcastStart.y - _downCastDetection.distance, _downcastStart.z);

        }
        else
        {
            _wallDetected = false;
            _downcastStartGizmo.SetPosition(_downcastStart);
        }

        //detect for an open space above the wall detection area
        if (_wallDetected)
        {
            _openSpacePosition = _downcastCollisionPosition + new Vector3(0, _downcastSize.y / 2, 0) + new Vector3(0, _openingSize.y / 2, 0);
            _openSpaceDetections = Physics.OverlapBox(_openSpacePosition, _openingSize / 2, _playerRotatingObject.rotation, _climbLayers);
            if (_openSpaceDetections.Length > 0)
            {
                _openSpaceDetected = false;
            }
            else
            {
                _openSpaceDetected = true;
            }
        }

        else
            _openSpaceDetected = false;


        if (_wallDetected && _openSpaceDetected)
            _isLedgeDetected = true;
        else
        {
            _isLedgeDetected = false;
        }


        


    }

    private void DrawLedgeGizmos()
    {
        


        _openAreaGizmo.SetSize(_openingSize);

        if (_openSpaceDetected)
        {
            _openAreaGizmo.SetPosition(_openSpacePosition);
            _openAreaGizmo.SetColor(_successColor);
        }

        else
        {
            _openAreaGizmo.SetPosition(_ledgeStart.position);
            _openAreaGizmo.SetColor(_failureColor);
        }


        _downcastStartGizmo.SetSize(_downcastSize);

        if (_wallDetected)
        {
            _downcastStartGizmo.SetPosition(_downcastCollisionPosition);
            _downcastStartGizmo.SetColor(_successColor);
        }
        else
        {

            _downcastStartGizmo.SetPosition(_downcastStart);
            _downcastStartGizmo.SetColor(_failureColor);
        }


        _upperBoundGizmo.SetColor(_boundsColor);
        _lowerBoundGizmo.SetColor(_boundsColor);
        _upperBoundGizmo.SetPosition(_ledgeStart.position + Vector3.up * _openingSize.y / 2);
        _lowerBoundGizmo.SetPosition(_downcastStart + Vector3.down * _maxDowncastDistance + Vector3.down * _downcastSize.y/2);
    }

    

}
