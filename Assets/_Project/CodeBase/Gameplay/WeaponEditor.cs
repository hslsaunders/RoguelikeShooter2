using _Project.Codebase.Misc;
using UnityEditor;
using UnityEngine;

namespace _Project.CodeBase.Player
{
    [CustomEditor(typeof(Weapon))]
    [CanEditMultipleObjects]
    public class WeaponEditor : CustomEditor<Weapon>
    {
        private Transform _bezierStartTransform;
        private Transform _bezierEndTransform;
        
        private bool _mirrorStartAndEnd;
        private float _circleSize = DEFAULT_CIRCLE_SIZE;
        private float _lineSize = 1f;
        private bool _displayBezierSample;
        private float _bezierSample;
        private int _numSteps = 20;
        private Vector2 _topAimRangeSliderPos;
        private Vector2 _bottomAimRangeSliderPos;

        private const float AIM_ANGLE_SLIDER_LENGTH = .15f;
        private const float DEFAULT_CIRCLE_SIZE = .05f;
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
            
            Handles.matrix = Matrix4x4.TRS(CastedTarget._holdCurve.originTransform.position, Quaternion.identity, 
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

            if (CastedTarget._holdCurve.originTransform != null)
            {
                DrawAimAngleHandle(CastedTarget.highestAimAngle, ref _topAimRangeSliderPos);
                DrawAimAngleHandle(-CastedTarget.lowestAimAngle, ref _bottomAimRangeSliderPos);
            }

            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        private void DrawAimAngleHandle(float angle, ref Vector2 sliderPos)
        {
            Vector2 lineTip = Utils.AngleToDirection(angle) * _lineSize;

            sliderPos = lineTip;//Utils.ClampVector(lineTip, Vector2.zero, sliderClamp);
            
            EditorGUI.BeginChangeCheck();
            
            float sliderDist = Handles.Slider(sliderPos,
                lineTip, AIM_ANGLE_SLIDER_LENGTH, Handles.ArrowHandleCap, 0f).magnitude;

            if (EditorGUI.EndChangeCheck())
                _lineSize = sliderDist;
            
            Handles.DrawLine(Vector2.zero, lineTip);
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
            AddFloatField(ref _lineSize, "Line Size");
            AddIntSlider(ref _numSteps, "Curve Vertices", CastedTarget._holdCurve.NumControlPoints + 1, 100);
            AddFloatSlider(ref _bezierSample, "Bezier Sample Point", 0f, 1f, ref _displayBezierSample);
        }
    }
}