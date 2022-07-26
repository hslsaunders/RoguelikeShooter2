using System;
using System.Collections.Generic;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace _Project.CodeBase.Gameplay.HoldableClasses.ArmActions
{
    [Serializable]
    public class EquipAction : ArmAction
    {
        public override string ActionString() => $"Equip Action on {holdable.name}";
        public override void StartAction()
        {
            base.StartAction();
                
            holdable.SetToBestHoldOrigin(armControllers, true);
            Debug.Log($"starting equip action, grabbing {holdable.name} with " +
                      $"{armControllers.GetEnumeratedString(controller => controller.HandName)}. running: {Running}");
        }

        public override void Tick()
        {
            base.Tick();
            for (int i = 0; i < handOrientations.Count; i++)
            {
                TransformOrientation arm = handOrientations[i];
                Transform targetTransform = holdable.holdPivots[i];
                Vector2 targetPos = targetTransform.position - arm.parent.position;
                targetPos.x *= entity.FlipMultiplier;

                arm.position = Vector2.MoveTowards(arm.position, targetPos, 5f * Time.deltaTime);
                
                float distProgress = 1 - Vector2.Distance(arm.position, targetPos) /
                                     Vector2.Distance(arm.startingOrientation.position, targetPos);

                arm.rotation = arm.rotation.SetZ(entity.FlipMultiplier * Mathf.LerpAngle(arm.startingOrientation.rotation.z,
                                                     targetTransform.transform.rotation.z, distProgress));

                if (i == 0 && Vector2.Distance(arm.position, targetPos) < .001f)
                {
                    ActionEnd();
                }
            }
        }

        public override void CancelAction()
        {
            base.CancelAction();
            
            holdable.SetToBestHoldOrigin();
            holdable.beingEquippedOrUnequipped = false;
        }

        public override void ActionEnd(bool clearArmActions = true, bool removeActionFromStackAndReset = false)
        {
            base.ActionEnd(clearArmActions, removeActionFromStackAndReset);
            Dictionary<Transform, Holdable> holsters = animationController.GetHolsters(holdable);
            if (holsters.TryGetKey(holdable, out Transform holster))
            {
                holsters[holster] = null;
            }
            
            Debug.Log($"ending {ActionString()} with " +
                      $"{armControllers.GetEnumeratedString(controller => controller.HandName)}. running: {Running}");
            animationController.SetArmsToHoldable(armControllers, holdable);
            
            RemoveActionFromStackAndReset();
        }
    }
}