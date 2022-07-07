using _Project.CodeBase.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace _Project.CodeBase.Editor
{
    [CustomEditor(typeof(Holdable))]
    [CanEditMultipleObjects]
    public class HoldableEditor : CustomEditor<Holdable>
    {
        private Transform _bezierStartTransform;
        private Transform _bezierEndTransform;
        
        protected bool _mirrorStartAndEnd;
        protected float _circleSize = DEFAULT_CIRCLE_SIZE;
        protected bool _displayBezierSample;
        protected float _bezierSample;
        protected int _numSteps = 20;
        protected float _aimRangeSliderDist = DEFAULT_LINE_SIZE;
        protected bool _clampToInsideAimRange;
        private const float AIM_ANGLE_SLIDER_LENGTH = .15f;

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (!debug) return;

            Transform originTransform = CastedTarget.holdCurve.originTransform
                ? CastedTarget.holdCurve.originTransform
                : CastedTarget.transform;
            Handles.matrix = Matrix4x4.TRS(originTransform.position, Quaternion.identity, 
                CastedTarget.holdCurve.OriginLossyScale);
            
            DrawStartAndEndHandles();

            Handles.color = Color.yellow;
            foreach ((BezierCurve.ControlPoint item, int index) cp in CastedTarget.holdCurve.controlPoints.WithIndex())
            {
                _circleSize = Handles.RadiusHandle(Quaternion.identity, cp.item.point, _circleSize);
                cp.item.point = Handles.PositionHandle(cp.item.point, Quaternion.identity);
                Handles.Label(cp.item.point + new Vector2(_circleSize, _circleSize), (cp.index + 1).ToString());
            }
            
            Handles.color = Color.green;
            if (_displayBezierSample)
            {
                Vector2 samplePoint = CastedTarget.holdCurve.GetLocalCurvePoint(_bezierSample);
                _circleSize = Handles.RadiusHandle(Quaternion.identity, samplePoint, _circleSize);
                Handles.DrawWireDisc(samplePoint, Vector3.back, _circleSize);
            }

            Vector2 lastPoint = CastedTarget.holdCurve.GetLocalCurvePoint(0f);
            float stepSize = 1f / (_numSteps - 1);
            for (int i = 1; i <= _numSteps; i++)
            {
                Vector2 samplePoint = CastedTarget.holdCurve.GetLocalCurvePoint(stepSize * i);
                Handles.DrawLine(lastPoint, samplePoint);
                lastPoint = samplePoint;
            }

            DrawAngleHandle(Vector2.zero, CastedTarget.highestAimAngle, AIM_ANGLE_SLIDER_LENGTH,
                ref _aimRangeSliderDist);
            DrawAngleHandle(Vector2.zero, -CastedTarget.lowestAimAngle, AIM_ANGLE_SLIDER_LENGTH,
                ref _aimRangeSliderDist);

            Handles.color = Color.green;
            /*
            Handles.matrix = Matrix4x4.identity;
            Vector2 lineDir = CastedTarget.holdCurve.originTransform ? 
                (CastedTarget.holdCurve.originTransform.position - CastedTarget.transform.position).normalized
                : -CastedTarget.transform.right;
            //Handles.DrawLine(CastedTarget.transform.position, 
            //    (Vector2)CastedTarget.transform.position + (lineDir * CastedTarget.minDistToAimPivot));
            */

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(CastedTarget);
                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(CastedTarget.gameObject.scene);
                SceneView.RepaintAll();
            }
        }

        private void HandleStartOrEndHandle(ref Vector2 oneEnd, ref Vector2 otherEnd, string label)
        {
            EditorGUI.BeginChangeCheck();
            AddCircleHandle(ref oneEnd, ref _circleSize);
            float angle = oneEnd.y > 0f
                ? CastedTarget.highestAimAngle
                : CastedTarget.lowestAimAngle;
            /*
            
            float maxY = oneEnd.x * Mathf.Tan(angle * Mathf.Deg2Rad);

            if (_clampToInsideAimRange && oneEnd.x > 0f && angle < 90f - .0001f)
                oneEnd.y = Mathf.Min(Mathf.Abs(oneEnd.y), maxY) * Mathf.Sign(oneEnd.y);
            */

            float cushionDistance = 45f;
            
            Vector2 angleDir = Utils.AngleToDirection(angle);
            Vector2 aimLine = angleDir * cushionDistance;
            Vector2 perpPoint = oneEnd + Vector2.Perpendicular(aimLine) * -cushionDistance;
            if (oneEnd.x > 0f && Utils.LineSegmentIntersect(Vector2.zero, angleDir, oneEnd.Abs(), perpPoint, 
                out Vector2 intersectionPoint))
            {
                oneEnd = intersectionPoint.SetY(intersectionPoint.y * Mathf.Sign(oneEnd.y));
            }
            
            if (EditorGUI.EndChangeCheck() && _mirrorStartAndEnd)
            {
                otherEnd = oneEnd.SetY(-oneEnd.y);
            }
            Handles.Label(oneEnd + new Vector2(_circleSize, _circleSize), label);
        }
        private void DrawStartAndEndHandles()
        {
            Handles.color = Color.red;

            HandleStartOrEndHandle(ref CastedTarget.holdCurve.start.point, 
                ref CastedTarget.holdCurve.end.point, 
                "Start");
            HandleStartOrEndHandle(ref CastedTarget.holdCurve.end.point, 
                ref CastedTarget.holdCurve.start.point, 
                "End");
        }

        protected override void DrawInspectorDebug()
        {
            base.DrawInspectorDebug();

            if (GUILayout.Button("Save Local Weapon Orientation"))
            {
                CastedTarget.localHoldPosition = CastedTarget.transform.localPosition;
                CastedTarget.localHoldRotation = CastedTarget.transform.localRotation;
                Debug.Log($"Saved {CastedTarget.name} Hold Position: {CastedTarget.localHoldPosition}, " +
                          $"Rotation: {CastedTarget.localHoldRotation}");
            }
            
            AddBoolField(ref _mirrorStartAndEnd, "Mirror Start And End");
            AddBoolField(ref _clampToInsideAimRange, "Clamp Start/End Inside Aim Range");
            AddFloatField(ref _circleSize, "Circle Size");
            AddIntSlider(ref _numSteps, "Curve Vertices", CastedTarget.holdCurve.NumControlPoints + 1, 100);
            AddFloatSlider(ref _bezierSample, "Bezier Sample Point", 0f, 1f, ref _displayBezierSample);
        }
    }
}