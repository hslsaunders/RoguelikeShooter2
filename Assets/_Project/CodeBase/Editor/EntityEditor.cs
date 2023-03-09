﻿using _Project.CodeBase.Gameplay.EntityClasses;
using UnityEditor;
using UnityEngine;

namespace _Project.CodeBase.Editor
{
    [CustomEditor(typeof(Entity))]
    [CanEditMultipleObjects]
    public class EntityEditor : CustomEditor<Entity>
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
            
            foreach (ArmTransform armTransform in CastedTarget.armTransforms)
            {
                if (!armTransform.testArmLength) continue;
                //Debug.Log($"{armTransform.armLength}");
                if (DrawAngleHandle(armTransform.root.position, 0f, .1f, 
                    ref armTransform.armLength, false))
                    armTransform.IKTransform.IKTarget.position =
                        armTransform.root.position + new Vector3(armTransform.armLength, 0f, 0f);

                if (DrawRadiusHandle(armTransform.root.position, ref armTransform.minArmDist))
                    armTransform.IKTransform.IKTarget.position =
                        armTransform.root.position + new Vector3(armTransform.minArmDist, 0f, 0f);
            }
        }

        protected override void DrawInspectorDebug()
        {
            base.DrawInspectorDebug();

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