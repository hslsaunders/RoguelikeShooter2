using _Project.CodeBase.Gameplay;
using _Project.CodeBase.Gameplay.HoldableClasses;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _Project.CodeBase.Editor
{
    [CustomEditor(typeof(Weapon), true)]
    [CanEditMultipleObjects]
    public class WeaponEditor : HoldableEditor
    {
        private float _spreadSliderDist = DEFAULT_LINE_SIZE;
        private Weapon recastedTarget;

        protected override void OnEnable()
        {
            base.OnEnable();

            recastedTarget = (Weapon)target;
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (!debug) return;
            
            if (recastedTarget._shootTransform != null)
            {
                Handles.matrix = Matrix4x4.TRS(recastedTarget._shootTransform.position, 
                    recastedTarget._shootTransform.rotation * Quaternion.Euler(0f, 0f, -90f), 
                    Vector3.one);

                DrawAngleHandle(Vector2.zero, recastedTarget.maxSpread, .15f,
                    ref _spreadSliderDist);
                DrawAngleHandle(Vector2.zero, -recastedTarget.maxSpread,.15f, 
                    ref _spreadSliderDist);
                Handles.color = Color.yellow;
                DrawAngleHandle(Vector2.zero, recastedTarget.Spread, .15f, ref _spreadSliderDist);
                DrawAngleHandle(Vector2.zero, -recastedTarget.Spread, .15f, ref _spreadSliderDist);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(recastedTarget);
                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(recastedTarget.gameObject.scene);
                SceneView.RepaintAll();
            }
        }
    }
}