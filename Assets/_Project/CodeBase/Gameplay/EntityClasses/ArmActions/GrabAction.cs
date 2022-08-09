﻿using _Project.CodeBase.Gameplay.HoldableClasses;
 using UnityEngine;

 namespace _Project.CodeBase.Gameplay.EntityClasses.ArmActions
{
    public class GrabAction : ArmAction
    {
        public override string ActionString() => $"Grab action on {holdable.name}";

        public override void Tick()
        {
            base.Tick();
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
            
            arm.position = Vector2.MoveTowards(arm.position, targetPos, 5f * Time.deltaTime);
            arm.position = armControllers[handIndex].ClampVectorToArmLength(arm.position);
            
            float distProgress = 1 - Vector2.Distance(arm.position, targetPos) /
                Vector2.Distance(arm.startingOrientation.position, targetPos);

            arm.rotation = arm.rotation.SetZ(entity.FlipMultiplier * Mathf.LerpAngle(arm.startingOrientation.rotation.z,
                targetTransform.transform.rotation.z, distProgress));

            if (handIndex == 0 && Vector2.Distance(arm.position, targetPos) < .001f)
            {
                OnReachTarget();
            }
        }

        protected virtual Transform GetTargetTransform(int handIndex)
        {
            return armControllers[handIndex].armTransform.handTransform;
        }
    }
}