﻿using System.Collections.Generic;
 using _Project.CodeBase.Gameplay.HoldableClasses;
 using UnityEngine;

 namespace _Project.CodeBase.Gameplay.EntityClasses.ArmActions
{
    public class GrabAction : ArmAction
    {
        protected virtual bool CancelActionIfTargetOutsideRange => false;
        private List<TransformOrientation> _armsReachedTarget = new List<TransformOrientation>();
        public override string ActionString() => "Grab action";

        public override void Tick()
        {
            base.Tick();
            if (_armsReachedTarget.Count == handOrientations.Count)
            {
                OnReachTarget();
            }
            
            for (int i = 0; i < handOrientations.Count; i++)
            {
                if (!Running) return;
                
                TransformOrientation arm = handOrientations[i];
                MoveHand(arm, i);
            }
        }

        protected virtual void OnReachTarget() => ActionEnd();

        protected virtual void MoveHand(TransformOrientation arm, int handIndex)
        {
            Transform targetTransform = GetTargetTransform(handIndex);
            Vector2 targetPos = targetTransform.position - arm.parent.position;
            targetPos.x *= entity.FlipMultiplier;
            
            Debug.DrawLine(arm.position * entity.HorizontalFlipMultiplier
                           + (Vector2)arm.parent.position, targetPos * entity.HorizontalFlipMultiplier + (Vector2)arm.parent.position);
            
            arm.position = Vector2.MoveTowards(arm.position, targetPos, 
                ArmController.HAND_MOVE_SPEED * Time.deltaTime);
            if (CancelActionIfTargetOutsideRange && !armControllers[handIndex].IsPointInArmLength(targetPos))
            {
                CancelAction();
                return;
            }

            arm.position = armControllers[handIndex].ClampVectorToArmLength(arm.position);
            
            float distProgress = 1 - Vector2.Distance(arm.position, targetPos) /
                Vector2.Distance(arm.startingOrientation.position, targetPos);

            arm.rotation = arm.rotation.SetZ(entity.FlipMultiplier * Mathf.LerpAngle(arm.startingOrientation.rotation.z,
                targetTransform.transform.rotation.z, distProgress));

            if (distProgress > .95f && !_armsReachedTarget.Contains(arm))
            {
                _armsReachedTarget.Add(arm);
            }
        }

        protected virtual Transform GetTargetTransform(int handIndex)
        {
            return armControllers[handIndex].armTransform.tip;
        }

        protected override void PreActionInitialize()
        {
            base.PreActionInitialize();
            
            _armsReachedTarget.Clear();
        }
    }
}