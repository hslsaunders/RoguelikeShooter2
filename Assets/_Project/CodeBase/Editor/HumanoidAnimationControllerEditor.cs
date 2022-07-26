using _Project.CodeBase.Gameplay.EntityClasses;
using UnityEditor;
using UnityEngine;

namespace _Project.CodeBase.Editor
{
    [CustomEditor(typeof(HumanoidAnimationController))]
    [CanEditMultipleObjects]
    public class HumanoidAnimationControllerEditor : CustomEditor<HumanoidAnimationController>
    {
        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
            
            Debug.Log("run");
            foreach (ArmTransform armTransform in CastedTarget.armTransforms)
            {
                Debug.Log($"{armTransform.armLength}");
                DrawAngleHandle(armTransform.armRoot.position, 0f, .15f, 
                    ref armTransform.armLength);
            }
        }
    }
}