using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

namespace _Project.CodeBase.Gameplay
{
    [CustomEditor(typeof(Weapon))]
    [CanEditMultipleObjects]
    public class WeaponEditor : CustomEditor<Weapon>
    {
        private Transform _bezierStartTransform;
        private Transform _bezierEndTransform;
        
        private bool _mirrorStartAndEnd;
        private float _circleSize = DEFAULT_CIRCLE_SIZE;
        private bool _displayBezierSample;
        private float _bezierSample;
        private int _numSteps = 20;
        private float _aimRangeSliderDist = DEFAULT_LINE_SIZE;
        private float _spreadSliderDist = DEFAULT_LINE_SIZE;

        private const float AIM_ANGLE_SLIDER_LENGTH = .15f;
        private const float DEFAULT_CIRCLE_SIZE = .05f;
        private const float DEFAULT_LINE_SIZE = 1f;
        private void UpdateBezier()
        {
            if (_bezierStartTransform != null)
            {
                CastedTarget._holdCurve.start.WorldPoint = _bezierStartTransform.position;
            }
            
            if (_bezierEndTransform != null)
            {
                CastedTarget._holdCurve.end.WorldPoint = _bezierEndTransform.position;
            }
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (!_debug) return;

            Transform originTransform = CastedTarget._holdCurve.originTransform
                ? CastedTarget._holdCurve.originTransform
                : CastedTarget.transform;
            Handles.matrix = Matrix4x4.TRS(originTransform.position, Quaternion.identity, 
                CastedTarget._holdCurve.OriginLossyScale);
            
            DrawStartAndEndHandles();

            Handles.color = Color.yellow;
            foreach ((BezierCurve.ControlPoint item, int index) cp in CastedTarget._holdCurve.controlPoints.WithIndex())
            {
                _circleSize = Handles.RadiusHandle(Quaternion.identity, cp.item.point, _circleSize);
                cp.item.point = Handles.PositionHandle(cp.item.point, Quaternion.identity);
                Handles.Label(cp.item.point + new Vector2(_circleSize, _circleSize), (cp.index + 1).ToString());
            }
            
            Handles.color = Color.green;
            if (_displayBezierSample)
            {
                Vector2 samplePoint = CastedTarget._holdCurve.GetLocalCurvePoint(_bezierSample);
                _circleSize = Handles.RadiusHandle(Quaternion.identity, samplePoint, _circleSize);
                Handles.DrawWireDisc(samplePoint, Vector3.back, _circleSize);
            }

            Vector2 lastPoint = CastedTarget._holdCurve.GetLocalCurvePoint(0f);
            float stepSize = 1f / (_numSteps - 1);
            for (int i = 1; i <= _numSteps; i++)
            {
                Vector2 samplePoint = CastedTarget._holdCurve.GetLocalCurvePoint(stepSize * i);
                Handles.DrawLine(lastPoint, samplePoint);
                lastPoint = samplePoint;
            }

            DrawAngleHandle(Vector2.zero, CastedTarget.highestAimAngle, ref _aimRangeSliderDist);
            DrawAngleHandle(Vector2.zero, -CastedTarget.lowestAimAngle, ref _aimRangeSliderDist);

            if (CastedTarget._shootTransform != null)
            {
                Handles.matrix = Matrix4x4.TRS(CastedTarget._shootTransform.position, 
                    CastedTarget._shootTransform.rotation * Quaternion.Euler(0f, 0f, -90f), 
                    Vector3.one);

                DrawAngleHandle(Vector2.zero, CastedTarget.maxSpread,
                    ref _spreadSliderDist);
                DrawAngleHandle(Vector2.zero, -CastedTarget.maxSpread,
                    ref _spreadSliderDist);
                Handles.color = Color.yellow;
                DrawAngleHandle(Vector2.zero, CastedTarget.Spread, ref _spreadSliderDist);
                DrawAngleHandle(Vector2.zero, -CastedTarget.Spread, ref _spreadSliderDist);
            }

            Handles.color = Color.green;
            /*
            Handles.matrix = Matrix4x4.identity;
            Vector2 lineDir = CastedTarget._holdCurve.originTransform ? 
                (CastedTarget._holdCurve.originTransform.position - CastedTarget.transform.position).normalized
                : -CastedTarget.transform.right;
            Handles.DrawLine(CastedTarget.transform.position, 
                (Vector2)CastedTarget.transform.position + (lineDir * CastedTarget.minDistToAimPivot));
                */

            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        private void DrawAngleHandle(Vector2 source, float angle, ref float sliderDist)
        {
            Vector2 lineTip = source + Utils.AngleToDirection(angle) * sliderDist;

            EditorGUI.BeginChangeCheck();
            Vector2 handlePos = Handles.Slider(lineTip,
                lineTip, AIM_ANGLE_SLIDER_LENGTH, Handles.ArrowHandleCap, 0f);

            if (EditorGUI.EndChangeCheck())
                sliderDist = handlePos.magnitude;

            lineTip = source + Utils.AngleToDirection(angle) * sliderDist;
            
            Handles.DrawLine(source, lineTip);
        }

        private void HandleStartOrEndHandle(ref Vector2 oneEnd, ref Vector2 otherEnd, string label)
        {
            EditorGUI.BeginChangeCheck();
            AddCircleHandle(ref oneEnd, ref _circleSize);
            if (EditorGUI.EndChangeCheck() && _mirrorStartAndEnd)
            {
                otherEnd = oneEnd.SetY(-oneEnd.y);
            }
            Handles.Label(oneEnd + new Vector2(_circleSize, _circleSize), label);
        }
        private void DrawStartAndEndHandles()
        {
            Handles.color = Color.red;

            HandleStartOrEndHandle(ref CastedTarget._holdCurve.start.point, ref CastedTarget._holdCurve.end.point, 
                "Start");
            HandleStartOrEndHandle(ref CastedTarget._holdCurve.end.point, ref CastedTarget._holdCurve.start.point, 
                "End");
        }

        protected override void DrawInspectorDebug()
        {
            base.DrawInspectorDebug();
            
            AddBoolField(ref _mirrorStartAndEnd, "Mirror Start And End");
            AddFloatField(ref _circleSize, "Circle Size");
            AddIntSlider(ref _numSteps, "Curve Vertices", CastedTarget._holdCurve.NumControlPoints + 1, 100);
            AddFloatSlider(ref _bezierSample, "Bezier Sample Point", 0f, 1f, ref _displayBezierSample);
        }
    }
}