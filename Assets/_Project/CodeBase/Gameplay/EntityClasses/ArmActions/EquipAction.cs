using System;
using System.Collections.Generic;
using _Project.CodeBase.Gameplay.HoldableClasses;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses.ArmActions
{
    [Serializable]
    public class EquipAction : GrabAction
    {
        public override string ActionString() => $"Equip Action on {holdable.name}";
        public override void StartAction()
        {
            base.StartAction();
                
            holdable.SetToBestHoldOrigin(armControllers, true);
            Debug.Log($"starting equip action, grabbing {holdable.name} with " +
                      $"{armControllers.GetEnumeratedString(controller => controller.HandName)}. running: {Running}");
        }

        protected override Transform GetTargetTransform(int handIndex) => holdable.holdPivots[
            holdable.NumHandsCurrentlyAssigned == 0 ? handIndex : holdable.NumHandsCurrentlyAssigned];
        public override void CancelAction()
        {
            base.CancelAction();
            
            holdable.SetToBestHoldOrigin();
            holdable.beingEquippedOrUnequipped = false;
        }

        public override void ActionEnd(bool clearArmActions = true, bool removeActionFromStackAndReset = false)
        {
            base.ActionEnd(true, false);
            Debug.Log($"ending {ActionString()} with " +
                      $"{armControllers.GetEnumeratedString(controller => controller.HandName)}. running: {Running}");
            Dictionary<Transform, Holdable> holsters = entity.GetHolsters(holdable);
            if (holsters.TryGetKey(holdable, out Transform holster))
            {
                holsters[holster] = null;
            }
            
            animationController.SetArmsToHoldable(armControllers, holdable);
            
            RemoveActionFromStackAndReset();
        }
    }
}