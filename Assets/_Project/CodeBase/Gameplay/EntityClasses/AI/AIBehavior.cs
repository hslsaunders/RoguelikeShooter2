using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses.AI
{
    public abstract class AIBehavior
    {
        public Entity entity;
        protected readonly AIController controller;

        private const int NUM_VISION_RAYCASTS = 4;

        public AIBehavior(AIController controller)
        {
            this.controller = controller;
        }
        
        public virtual void OnEnter() {}
        public virtual void Tick(float deltaTime) {}
        public virtual void OnExit() {}

        protected static bool IsEnemyInVision(int teamId, Vector2 visionSourcePos, Vector2 visionDirection, float maxDistance, 
            float upperVisionAngle, float lowerVisionAngle)
        {
            List<Entity> enemies = Teams.GetEnemyOfTeamIdList(teamId);
            
            foreach (Entity entity in enemies)
            {
                Vector2 centerOfEntity = entity.GetCenterOfEntity;
                //Debug.DrawLine(visionSourcePos, centerOfEntity);
                float distToCenter = Vector2.Distance(centerOfEntity, visionSourcePos);
                if (distToCenter > maxDistance) continue;

                float stepSize = Entity.HEIGHT / (NUM_VISION_RAYCASTS - 1);
                for (float yOffset = 0f; yOffset <= Entity.HEIGHT; yOffset += stepSize)
                {
                    Vector2 checkPos = entity.transform.position + new Vector3(0f, yOffset, 0f);
                    Debug.DrawLine(visionSourcePos, checkPos);
                }
            }

            return false;
        }
    }
}