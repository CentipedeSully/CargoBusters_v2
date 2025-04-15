using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawRectGizmo : MonoBehaviour
{
    public bool ShowGizmo = true;
    [SerializeField] private Color _color; 
    [SerializeField] private Vector3 _size;







    private void OnDrawGizmos()
    {
        if (ShowGizmo)
            DrawRect();
    }

    private void DrawRect()
    {
        Vector3 rightPoint = Vector3.right * _size.x / 2;
        Vector3 leftPoint = Vector3.left * _size.x / 2;
        Vector3 frontPoint = Vector3.forward * _size.z / 2;
        Vector3 backPoint = Vector3.back * _size.z / 2;
        Vector3 topPoint = Vector3.up * _size.y / 2;
        Vector3 bottomPoint = Vector3.down * _size.y / 2;

        Gizmos.color = _color;


        //Top edges
        //--top back
        Gizmos.DrawLine( transform.TransformPoint(topPoint + backPoint + leftPoint),  transform.TransformPoint(topPoint + backPoint + rightPoint));

        //--top front
        Gizmos.DrawLine(transform.TransformPoint(topPoint + frontPoint + leftPoint), transform.TransformPoint(topPoint + frontPoint + rightPoint));

        //--top left
        Gizmos.DrawLine(transform.TransformPoint(topPoint + frontPoint + leftPoint), transform.TransformPoint(topPoint + backPoint + leftPoint));

        //--top right
        Gizmos.DrawLine(transform.TransformPoint(topPoint + frontPoint + rightPoint), transform.TransformPoint(topPoint + backPoint + rightPoint));



        //bottom edges
        //--bot back
        Gizmos.DrawLine(transform.TransformPoint(bottomPoint + backPoint + leftPoint), transform.TransformPoint(bottomPoint + backPoint + rightPoint));

        //--bot front
        Gizmos.DrawLine(transform.TransformPoint(bottomPoint + frontPoint + leftPoint), transform.TransformPoint(bottomPoint + frontPoint + rightPoint));

        //--bot left
        Gizmos.DrawLine(transform.TransformPoint(bottomPoint + frontPoint + leftPoint), transform.TransformPoint(bottomPoint + backPoint + leftPoint));

        //--bot right
        Gizmos.DrawLine(transform.TransformPoint(bottomPoint + frontPoint + rightPoint), transform.TransformPoint(bottomPoint + backPoint + rightPoint));



        //vertical edges
        //--backRight
        Gizmos.DrawLine(transform.TransformPoint(topPoint + backPoint + rightPoint), transform.TransformPoint(bottomPoint + backPoint + rightPoint));

        //--backLeft
        Gizmos.DrawLine(transform.TransformPoint(topPoint + backPoint + leftPoint), transform.TransformPoint(bottomPoint + backPoint + leftPoint));

        //--frontRight
        Gizmos.DrawLine(transform.TransformPoint(topPoint + frontPoint + rightPoint), transform.TransformPoint(bottomPoint + frontPoint + rightPoint));

        //--frontLeft
        Gizmos.DrawLine(transform.TransformPoint(topPoint + frontPoint + leftPoint), transform.TransformPoint(bottomPoint + frontPoint + leftPoint));


    }



    public void SetSize(Vector3 newSize)
    {
        _size = newSize;
    }

    public void SetColor(Color newColor)
    {
        _color = newColor;
    }

    public void SetPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }
}
