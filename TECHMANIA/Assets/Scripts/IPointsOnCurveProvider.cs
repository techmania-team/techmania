using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPointsOnCurveProvider
{
    IList<Vector2> GetPointsOnCurve();
}
