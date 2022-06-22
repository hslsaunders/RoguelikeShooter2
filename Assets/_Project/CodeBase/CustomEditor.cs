using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.CodeBase
{
    public class CustomEditor<T> : Editor where T : MonoBehaviour
    {
        protected bool _debug = true;
        public T CastedTarget { get; private set; }

        private void OnEnable()
        {
            CastedTarget = (T)target;
        }

        protected virtual void OnSceneGUI()
        {
            
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            AddBoolField(ref _debug, "Show Debugs");
            
            if (_debug)
                DrawInspectorDebug();

            if (GUI.changed)
                SceneView.RepaintAll();
        }
        protected virtual void DrawInspectorDebug()
        {
        }

        public void AddCircleHandle(ref Vector2 targetPoint, ref float _debugSize)
        {
            _debugSize = Handles.RadiusHandle(Quaternion.identity, targetPoint, _debugSize);
            targetPoint = Handles.PositionHandle(targetPoint, Quaternion.identity);
        }

        protected void AddObjectFieldNoFormat<K>(ref K obj, string label, params GUILayoutOption[] options) where K : Object
        {
            obj = (K)EditorGUILayout.ObjectField(label, obj, typeof(K), true, options);
        }
        
        protected void AddObjectField<K>(ref K obj, string label) where K : Object
        {
            EditorGUILayout.BeginHorizontal();
            AddObjectFieldNoFormat(ref obj, label);
            EditorGUILayout.EndHorizontal();
        }

        protected void AddBoolFieldNoFormat(ref bool boolValue, string label, params GUILayoutOption[] options)
        {
            EditorGUILayout.PrefixLabel(label);
            boolValue = EditorGUILayout.Toggle(boolValue, options);
        }

        protected void AddBoolField(ref bool boolValue, string label, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal();
            AddBoolFieldNoFormat(ref boolValue, label, options);
            EditorGUILayout.EndHorizontal();
        }

        #region IntFields

        protected void AddIntFieldNoFormat(ref int intValue, string label) => 
            intValue = EditorGUILayout.IntField(label, intValue);

        protected void AddIntField(ref int intValue, string label)
        {
            EditorGUILayout.BeginHorizontal();
            AddIntFieldNoFormat(ref intValue, label);
            EditorGUILayout.EndHorizontal();
        }
        
        protected void AddIntField(ref int intValue, string label, ref bool toggleValue)
        {
            EditorGUILayout.BeginHorizontal();
            toggleValue = EditorGUILayout.Toggle(toggleValue);
            if (toggleValue)
                intValue = EditorGUILayout.IntField(label, intValue);
            EditorGUILayout.EndHorizontal();
        }

        #endregion
        
        #region FloatFields

        protected void AddFloatFieldNoFormat(ref float floatValue, string label) =>
            floatValue = EditorGUILayout.FloatField(label, floatValue);
        protected void AddFloatField(ref float floatValue, string label)
        {
            EditorGUILayout.BeginHorizontal();
            AddFloatFieldNoFormat(ref floatValue, label);
            EditorGUILayout.EndHorizontal();
        }
        
        protected void AddFloatField(ref float floatValue, string label, ref bool toggleValue)
        {
            EditorGUILayout.BeginHorizontal();
            toggleValue = EditorGUILayout.Toggle(toggleValue);
            if (toggleValue)
                floatValue = EditorGUILayout.FloatField(label, floatValue);
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion

        #region Sliders

        #region Int Sliders
        protected void AddIntSliderNoFormat(ref int intValue, string label, int min, int max)
        {
            EditorGUILayout.PrefixLabel(label);
            intValue = EditorGUILayout.IntSlider(intValue, min, max);
        }
        
        protected void AddIntSlider(ref int intValue, string label, int min, int max)
        {
            EditorGUILayout.BeginHorizontal();
            AddIntSliderNoFormat(ref intValue, label, min, max);
            EditorGUILayout.EndHorizontal();
        }
        
        protected void AddIntSlider(ref int intValue, string label, int min, int max, ref bool toggleValue)
        {
            EditorGUILayout.BeginHorizontal();
            toggleValue = EditorGUILayout.Toggle(toggleValue);
            if (toggleValue)
                AddIntSliderNoFormat(ref intValue, label, min, max);
            EditorGUILayout.EndHorizontal();
        }
        #endregion
        
        #region Float Sliders

        protected void AddFloatSliderNoFormat(ref float floatValue, string label, float min, float max)
        {
            if (label != "") 
                EditorGUILayout.PrefixLabel(label);
            floatValue = EditorGUILayout.Slider(floatValue, min, max);
        }
        
        protected void AddFloatSlider(ref float floatValue, string label, float min, float max)
        {
            EditorGUILayout.BeginHorizontal();
            AddFloatSliderNoFormat(ref floatValue, label, min, max);
            EditorGUILayout.EndHorizontal();
        }

        protected void AddFloatSlider(ref float floatValue, string label, float min, float max, ref bool toggleValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            toggleValue = EditorGUILayout.Toggle(toggleValue);
            if (toggleValue)
                AddFloatSliderNoFormat(ref floatValue, "", min, max);
            EditorGUILayout.EndHorizontal();
        }

        #endregion
        
        #endregion
    }
}