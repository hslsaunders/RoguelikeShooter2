using UnityEditor;
using UnityEngine;

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

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            DebugField();
            
            if (_debug)
            {
                DrawInspectorDebug();
            }

            if (GUI.changed)
                SceneView.RepaintAll();
        }

        protected void AddIntField(ref int intValue, string label)
        {
            EditorGUILayout.BeginHorizontal();
            intValue = EditorGUILayout.IntField(label, intValue);
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
        
        protected void AddFloatField(ref float floatValue, string label)
        {
            EditorGUILayout.BeginHorizontal();
            
            floatValue = EditorGUILayout.FloatField(label, floatValue);
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
        
        protected void AddBoolField(ref bool boolValue, string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            boolValue = EditorGUILayout.Toggle(boolValue);
            EditorGUILayout.EndHorizontal();
        }
        
        protected void AddIntSlider(ref int intValue, string label, int min, int max)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            intValue = EditorGUILayout.IntSlider(intValue, min, max);
            EditorGUILayout.EndHorizontal();
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

        private void AddFloatSliderNoFormat(ref float floatValue, string label, float min, float max)
        {
            if (label != "") 
                EditorGUILayout.PrefixLabel(label);
            floatValue = EditorGUILayout.Slider(floatValue, min, max);
        }
        
        private void DebugField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Show Debugs");
            _debug = EditorGUILayout.Toggle(_debug);
            EditorGUILayout.EndHorizontal();
        }
        
        protected virtual void DrawInspectorDebug()
        {
            
        }
    }
}