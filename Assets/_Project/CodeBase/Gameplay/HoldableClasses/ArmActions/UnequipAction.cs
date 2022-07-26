using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.HoldableClasses.ArmActions
{
    [Serializable]
    public class UnequipAction : ArmAction
    {
        private Transform _targetHolster;
        private Dictionary<Transform, Holdable> _holsters;

        public override string ActionString() => $"Unequip Action on {holdable.name}";

        public override void StartAction()
        {
            base.StartAction();
            
            Debug.Log($"starting unequip action, grabbing {holdable.name} with " +
                      $"{armControllers.GetEnumeratedString(controller => controller.HandName)}");

            _holsters = animationController.GetHolsters(holdable);
            foreach ((Transform holster, Holdable holdableInHolster) in _holsters)
            {
                if (holdableInHolster) continue;
                
                _targetHolster = holster;
                _holsters[holster] = holdable;
                break;
            }

            holdable.beingEquippedOrUnequipped = true;
        }

        public override void Tick()
        {
            base.Tick();
            for (int i = 0; i < handOrientations.Count; i++)
            {
                TransformOrientation arm = handOrientations[i];
                Transform targetTransform = _targetHolster;
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

            _holsters[_targetHolster] = null;
            holdable.beingEquippedOrUnequipped = false;
        }

        public override void ActionEnd(bool clearArmActions = false, bool removeActionFromStackAndReset = false)
        {
            Debug.Log($"ending unequip action, grabbing {holdable.name} with " +
                      $"{armControllers.GetEnumeratedString(controller => controller.HandName)}");
            
            base.ActionEnd(clearArmActions, removeActionFromStackAndReset);
            
            animationController.HolsterHoldableAndDisconnectArms(armControllers, holdable, _targetHolster, this);
            
            ClearArmControllerActions();
            RemoveActionFromStackAndReset();
        }
    }
}