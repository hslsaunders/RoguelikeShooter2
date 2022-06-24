using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.CodeBase
{
    [Serializable]
    public class BezierCurve
    {
        public Transform originTransform;
        public ControlPoint start = new ControlPoint();
        public ControlPoint end = new ControlPoint();
        public List<ControlPoint> controlPoints = new List<ControlPoint>();
        public int NumControlPoints => controlPoints.Count;
        public Vector3 OriginLossyScale => originTransform ? originTransform.lossyScale : Vector3.one;
        public void SetOriginTransforms()
        {
            start.TrySetOrigin(originTransform);
            end.TrySetOrigin(originTransform);
            
            foreach (ControlPoint cp in controlPoints)
                cp.TrySetOrigin(originTransform);
        }

        public Vector2 GetLocalCurvePoint(float t)
        {
            t = Mathf.Clamp(t, 0f, 1f);
            Vector2 vectorSum = SumLocalPoints(t);
            int numPoints = NumControlPoints;

            float weightDivisor = 0f;
            weightDivisor += start.weight * Mathf.Pow(1 - t, numPoints + 1);
            weightDivisor += end.weight * Mathf.Pow(t, numPoints + 1);

            for (int index = 0; index < numPoints; index++)
                weightDivisor += (numPoints + 1) 
                                 * Mathf.Pow(1 - t, numPoints - index) * Mathf.Pow(t, index + 1)
                                 * controlPoints[index].weight;

            if (Math.Abs(weightDivisor) < .0001f)
                return start.point;
            return vectorSum / weightDivisor;
        }

        
        public Vector2 GetWorldCurvePoint(float t)
        {
            t = Mathf.Clamp(t, 0f, 1f);
            Vector2 vectorSum = SumWorldPoints(t);
            int numPoints = NumControlPoints;

            float weightDivisor = 0f;
            weightDivisor += start.weight * Mathf.Pow(1 - t, numPoints + 1);
            weightDivisor += end.weight * Mathf.Pow(t, numPoints + 1);

            for (int index = 0; index < numPoints; index++)
                weightDivisor += (numPoints + 1) 
                                 * Mathf.Pow(1 - t, numPoints - index) * Mathf.Pow(t, index + 1)
                                 * controlPoints[index].weight;

            if (Math.Abs(weightDivisor) < .0001f)
                return start.WorldPoint;
            return vectorSum / weightDivisor;
        }

        private Vector2 SumWorldPoints(float t)
        {
            Vector2 sum = Vector2.zero;
            int numPoints = NumControlPoints;

            sum += Mathf.Pow(1 - t, numPoints + 1) * start.weight * start.WorldPoint; 
            sum += Mathf.Pow(t, numPoints + 1) * end.weight * end.WorldPoint;

            int index = 0;
            
            for (; index < numPoints; index++)
            {
                ControlPoint point = controlPoints[index];
                sum += (numPoints + 1) * Mathf.Pow(1 - t, numPoints - index) * Mathf.Pow(t, index + 1)
                       * point.weight * point.WorldPoint;
            }
            
            return sum;
        }
        
        private Vector2 SumLocalPoints(float t)
        {
            Vector2 sum = Vector2.zero;
            int numPoints = NumControlPoints;

            sum += Mathf.Pow(1 - t, numPoints + 1) * start.weight * start.point; 
            sum += Mathf.Pow(t, numPoints + 1) * end.weight * end.point;

            int index = 0;
            
            for (; index < numPoints; index++)
            {
                ControlPoint point = controlPoints[index];
                sum += (numPoints + 1) * Mathf.Pow(1 - t, numPoints - index) * Mathf.Pow(t, index + 1)
                       * point.weight * point.point;
            }
            
            return sum;
        }

        [Serializable]
        public class ControlPoint
        {
            public Vector2 point;

            public Vector2 WorldPoint
            {
                get => (point * (_hasOriginTransform ? _originTransform.lossyScale : Vector3.one))
                       + (_hasOriginTransform ? (Vector2)_originTransform.position : Vector2.zero);
                set => point = 
                    (value - (_hasOriginTransform ? (Vector2) _originTransform.position : Vector2.zero))
                    / (_hasOriginTransform ? _originTransform.lossyScale : Vector3.one);
            }
            public float weight = 1f;
            private Transform _originTransform;
            private bool _hasOriginTransform;

            public void TrySetOrigin(Transform origin)
            {
                if (_originTransform != origin)
                {
                    _originTransform = origin;
                    if (origin != null)
                        _hasOriginTransform = true;
                    else
                        _hasOriginTransform = false;
                }
            }
        }
    }
}