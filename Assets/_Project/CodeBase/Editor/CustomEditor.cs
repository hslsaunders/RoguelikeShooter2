using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.CodeBase.Editor
{
    public class CustomEditor<T> : UnityEditor.Editor where T : MonoBehaviour
    {
        protected bool debug = true;
        public T CastedTarget { get; private set; }
        protected virtual bool MakeInspectorDebugToggleable => true;
        
        protected const float DEFAULT_CIRCLE_SIZE = .05f;
        protected const float DEFAULT_LINE_SIZE = 1f;

        protected virtual void OnEnable()
        {
            CastedTarget = (T)target;
        }

        protected virtual void OnSceneGUI() {}
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (MakeInspectorDebugToggleable)
                AddBoolField(boolValue: ref debug, label: "Show Debugs");
            
            if (!MakeInspectorDebugToggleable || debug)
                DrawInspectorDebug();

            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }
        protected virtual void DrawInspectorDebug()
        {
        }

        protected void AddPositionHandle(ref Vector2 targetPoint)
        {
            targetPoint = Handles.PositionHandle(position: targetPoint, rotation: Quaternion.identity);
        }
        
        protected void AddCircleHandle(ref Vector2 targetPoint, ref float _debugSize)
        {
            _debugSize = Handles.RadiusHandle(rotation: Quaternion.identity, position: targetPoint, radius: _debugSize);
            targetPoint = Handles.PositionHandle(position: targetPoint, rotation: Quaternion.identity);
        }

        protected void DrawAngleHandle(Vector2 source, float angle, float angleSliderLength, ref float sliderDist)
        {
            Vector2 lineTip = source + Utils.AngleToDirection(angle: angle) * sliderDist;

            EditorGUI.BeginChangeCheck();
            Vector2 handlePos = Handles.Slider(position: lineTip,
                direction: lineTip, size: angleSliderLength, capFunction: Handles.ArrowHandleCap, snap: 0f);

            if (EditorGUI.EndChangeCheck())
                sliderDist = handlePos.magnitude;

            lineTip = source + Utils.AngleToDirection(angle: angle) * sliderDist;
            
            Handles.DrawLine(p1: source, p2: lineTip);
        }
        
        protected void AddObjectFieldNoFormat<K>(ref K obj, string label, params GUILayoutOption[] options) where K : Object
        {
            obj = (K)EditorGUILayout.ObjectField(label: label, obj: obj, objType: typeof(K), allowSceneObjects: true, options: options);
        }
        
        protected void AddObjectField<K>(ref K obj, string label) where K : Object
        {
            EditorGUILayout.BeginHorizontal();
            AddObjectFieldNoFormat(obj: ref obj, label: label);
            EditorGUILayout.EndHorizontal();
        }
        
        protected void AddBoolFieldNoFormat(ref bool boolValue, string label, params GUILayoutOption[] options)
        {
            EditorGUILayout.PrefixLabel(label: label);
            boolValue = EditorGUILayout.Toggle(value: boolValue, options: options);
        }

        protected void AddBoolField(ref bool boolValue, string label, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal();
            AddBoolFieldNoFormat(boolValue: ref boolValue, label: label, options: options);
            EditorGUILayout.EndHorizontal();
        }

        protected void AddBoolField(ref bool boolValue, ref bool toggleValue, string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label: label);
            toggleValue = EditorGUILayout.Toggle(value: toggleValue);
            if (toggleValue)
                boolValue = EditorGUILayout.Toggle(value: boolValue);
            EditorGUILayout.EndHorizontal();
        }

        #region IntFields

        protected void AddIntFieldNoFormat(ref int intValue, string label) => 
            intValue = EditorGUILayout.IntField(label: label, value: intValue);

        protected void AddIntField(ref int intValue, string label)
        {
            EditorGUILayout.BeginHorizontal();
            AddIntFieldNoFormat(intValue: ref intValue, label: label);
            EditorGUILayout.EndHorizontal();
        }
        
        protected void AddIntField(ref int intValue, string label, ref bool toggleValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label: label);
            toggleValue = EditorGUILayout.Toggle(value: toggleValue);
            if (toggleValue)
                intValue = EditorGUILayout.IntField(label: label, value: intValue);
            EditorGUILayout.EndHorizontal();
        }

        #endregion
        
        #region FloatFields

        protected void AddFloatFieldNoFormat(ref float floatValue, string label) =>
            floatValue = EditorGUILayout.FloatField(label: label, value: floatValue);
        protected void AddFloatField(ref float floatValue, string label)
        {
            EditorGUILayout.BeginHorizontal();
            AddFloatFieldNoFormat(floatValue: ref floatValue, label: label);
            EditorGUILayout.EndHorizontal();
        }
        
        protected void AddFloatField(ref float floatValue, string label, ref bool toggleValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label: label);
            toggleValue = EditorGUILayout.Toggle(value: toggleValue);
            if (toggleValue)
                floatValue = EditorGUILayout.FloatField(value: floatValue);
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion

        #region Sliders

        #region Int Sliders
        protected void AddIntSliderNoFormat(ref int intValue, string label, int min, int max)
        {
            EditorGUILayout.PrefixLabel(label: label);
            intValue = EditorGUILayout.IntSlider(value: intValue, leftValue: min, rightValue: max);
        }
        
        protected void AddIntSlider(ref int intValue, string label, int min, int max)
        {
            EditorGUILayout.BeginHorizontal();
            AddIntSliderNoFormat(intValue: ref intValue, label: label, min: min, max: max);
            EditorGUILayout.EndHorizontal();
        }
        
        protected void AddIntSlider(ref int intValue, string label, int min, int max, ref bool toggleValue)
        {
            EditorGUILayout.BeginHorizontal();
            toggleValue = EditorGUILayout.Toggle(value: toggleValue);
            if (toggleValue)
                AddIntSliderNoFormat(intValue: ref intValue, label: label, min: min, max: max);
            EditorGUILayout.EndHorizontal();
        }
        #endregion
        
        #region Float Sliders

        protected void AddFloatSliderNoFormat(ref float floatValue, string label, float min, float max)
        {
            if (label != "") 
                EditorGUILayout.PrefixLabel(label: label);
            floatValue = EditorGUILayout.Slider(value: floatValue, leftValue: min, rightValue: max);
        }
        
        protected void AddFloatSlider(ref float floatValue, string label, float min, float max)
        {
            EditorGUILayout.BeginHorizontal();
            AddFloatSliderNoFormat(floatValue: ref floatValue, label: label, min: min, max: max);
            EditorGUILayout.EndHorizontal();
        }

        protected void AddFloatSlider(ref float floatValue, string label, float min, float max, ref bool toggleValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label: label);
            toggleValue = EditorGUILayout.Toggle(value: toggleValue);
            if (toggleValue)
                AddFloatSliderNoFormat(floatValue: ref floatValue, label: "", min: min, max: max);
            EditorGUILayout.EndHorizontal();
        }

        #endregion
        
        #endregion
    }
}