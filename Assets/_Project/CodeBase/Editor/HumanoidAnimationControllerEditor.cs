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

            foreach (ArmTransform armTransform in CastedTarget.armTransforms)
            {
                if (!armTransform.testArmLength) continue;
                //Debug.Log($"{armTransform.armLength}");
                DrawAngleHandle(armTransform.armRoot.position, 0f, .1f, 
                    ref armTransform.armLength, false);
                armTransform.IKTransform.IKTarget.position =
                    armTransform.armRoot.position + new Vector3(armTransform.armLength, 0f, 0f);
            }
        }
    }
}