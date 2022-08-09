using System;
using System.Collections.Generic;
using _Project.CodeBase.Gameplay.HoldableClasses;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses.ArmActions
{
    [Serializable]
    public class UnequipAction : GrabAction
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

        protected override Transform GetTargetTransform(int handIndex) => _targetHolster;

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
            
            base.ActionEnd(false, false);
            
            animationController.HolsterHoldableAndDisconnectArms(armControllers, holdable, _targetHolster, this);
            
            ClearArmControllerActions();
            RemoveActionFromStackAndReset();
        }
    }
}