using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{

    public static Vector3 GetClosestPoint(Vector3 origin, RaycastHit[] castResults)
    {
        if (castResults.Length > 0)
        {

            //validate each result to make sure its points actually exist
            List<Vector3> validPoints = new List<Vector3>();

            foreach (RaycastHit hit in castResults)
            {
                if (hit.collider != null)
                {
                    validPoints.Add(hit.point);
                }
            }

            //now fond the closest point amid the valid points
            return GetClosestPoint(origin, validPoints);

        }

        else return Vector3.negativeInfinity;
    }

    public static Vector3 GetClosestPoint(Vector3 origin, Vector3[] points)
    {
        if (points.Length > 0)
        {
            //save the closest hit only
            Vector3 closest = points[0];

            foreach (Vector3 point in points)
            {
                if (closest == point)
                    continue;

                if (Vector3.Distance(origin, point) < Vector3.Distance(origin, closest))
                {
                    closest = point;
                }
            }

            return closest;
        }
        else
        {
            return Vector3.negativeInfinity;
        }
    }

    public static Vector3 GetClosestPoint(Vector3 origin, List<Vector3> points)
    {
        //Debug.Log($"Points Detected: {points.Count}");
        if (points.Count > 0)
        {

            //save the closest hit only
            Vector3 closest = points[0];

            foreach (Vector3 point in points)
            {
                if (closest == point)
                    continue;

                if (Vector3.Distance(origin, point) < Vector3.Distance(origin, closest))
                {
                    closest = point;
                }
            }

            return closest;
        }
        else
        {
            return Vector3.negativeInfinity;
        }
    }


    public static void TestGetClosestPoint()
    {
        Vector3 origin = Vector3.up * 2; //Vector.up is closest
        Vector3[] pointsArry = { Vector3.zero, Vector3.up, Vector3.down, Vector3.up * 99 };
        List<Vector3> pointsList = new List<Vector3>();
        pointsList.Add(Vector3.zero);
        pointsList.Add(Vector3.up);
        pointsList.Add(Vector3.down);
        pointsList.Add(Vector3.up * 99);

        Debug.LogWarning($"" +
            $"Test Results:\n" +
            $"Origin point: (0,2,0)\n" +
            $"Expected Result: (0,1,0)\n" +
            $"Closest Point (List): {GetClosestPoint(origin, pointsList)}\n" +
            $"Closest Point (Array): {GetClosestPoint(origin, pointsArry)}\n");

    }

}
