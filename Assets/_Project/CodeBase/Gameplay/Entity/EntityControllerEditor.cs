using UnityEditor;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.Entity
{
    [CustomEditor(typeof(EntityController))]
    [CanEditMultipleObjects]
    public class EntityControllerEditor : CustomEditor<EntityController>
    {
        private float _debugSize = .05f;
        private bool _overrideMoveInput;
        private Transform _targetTransform;
        private Vector2 _worldSpaceTargetPos;
        private bool _worldPositionStays;
        protected override bool MakeInspectorDebugToggleable => false;

        protected override void OnSceneGUI()
        {
            EditorGUI.BeginChangeCheck();
            AddCircleHandle(ref _worldSpaceTargetPos, ref _debugSize);

            if (EditorGUI.EndChangeCheck())
            {
                CastedTarget.AimTarget = _worldSpaceTargetPos;
                SceneView.RepaintAll();
            }
            else
                _worldSpaceTargetPos = CastedTarget.AimTarget;
                
        }

        protected override void DrawInspectorDebug()
        {
            base.DrawInspectorDebug();
            
            //AddObjectField(ref CastedTarget.weaponController.weapon, "Weapon");
            AddFloatSlider(ref CastedTarget.moveInput.x, "Override Input", -1f, 1f, ref _overrideMoveInput);
            AddBoolField(ref CastedTarget.overriddenTriggerDownValue, ref CastedTarget.overrideTriggerDown,
                "Override Trigger Down Value");

            EditorGUILayout.BeginHorizontal();
            AddObjectFieldNoFormat(ref _targetTransform, "New\\Current Target Transform", 
                GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth / 2f));
            EditorGUI.BeginDisabledGroup(true);
            AddObjectFieldNoFormat(ref CastedTarget.targetTransform, "", 
                GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth / 2f));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (GUILayout.Button("Sync"))
            {
                if (_worldPositionStays && _targetTransform != CastedTarget.targetTransform)
                {
                    if (_targetTransform != null)
                        CastedTarget.targetOffset = _targetTransform.InverseTransformPoint(CastedTarget.AimTarget);
                    else if (CastedTarget.targetTransform != null)
                        CastedTarget.targetOffset += (Vector2)CastedTarget.targetTransform.position;
                }
                CastedTarget.targetTransform = _targetTransform;
            }

            EditorGUIUtility.labelWidth = 125f;
            AddBoolFieldNoFormat(ref _worldPositionStays, "World Position Stays");
            EditorGUILayout.EndHorizontal();
        }
    }
}