
using System;
using UnityEngine;

public class DirectionUtil
{
    private const float TOLERANCE = 0.05f;

    // Do not instantiate
    private DirectionUtil() {}

    public static int GetAnimationSuffixForDirection(Vector3 origin, Vector3 target)
    {
        if (Math.Abs(target.x - origin.x) < TOLERANCE)
        {
            return target.y > origin.y ? 7 : 3;
        } 
        if (Math.Abs(target.y - origin.y) < TOLERANCE)
        {
            return target.x > origin.x ? 1 : 5;
        }
        if (target.x > origin.x)
        {
            return target.y > origin.y ? 0 : 2;
        }

        return target.y > origin.y ? 6 : 4;
    }
}
