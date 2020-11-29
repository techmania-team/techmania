using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPointsOnCurveProvider
{
    List<Vector2> GetPointsOnCurve();
}
