using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FloatComparer
{
    public static bool IsFirstLessThanSecond(float x, float y)
    {
        int first = Mathf.RoundToInt(x * 100);
        int second = Mathf.RoundToInt(y * 100);

        Debug.Log($"first: {first}\nsecond: {second}\nIs First Less: {first < second}");
        return first < second;
    }

    public static bool IsEqual(float x, float y)
    {
        int first = Mathf.RoundToInt(x * 100);
        int second = Mathf.RoundToInt(y * 100);

        Debug.Log($"first: {first}\nsecond: {second}\nIs Equal: {first == second}");
        return first == second;
    }
}
