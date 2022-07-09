using System.Collections.Generic;
using _Project.CodeBase.Navmesh;
using _Project.CodeBase.Player;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses.AI
{
    public class ChaseTargetBehavior : AIBehavior
    {
        private Entity _targetEntity;
        private List<Vector2> _pathPoints = new List<Vector2>();
        private Stack<Vector2> _path = new Stack<Vector2>();
        private NavmeshManager _navmeshManager;
        private Vector2 moveTarget;

        private float _lastRepathTime;
        
        private const float REPATH_RATE = .5f;
        public override void OnEnter()
        {
            base.OnEnter();
            _navmeshManager = NavmeshManager.Get();
            _targetEntity = PlayerManager.Singleton.entity;
            _lastRepathTime = Time.time;
        }

        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            
            if (Time.time > _lastRepathTime + REPATH_RATE)
            {
                if (entity.TryGetNearestGroundTile(out NavmeshNode groundNode) && groundNode.groundWalkable)
                {
                    if (_targetEntity.TryGetNearestGroundTile(out NavmeshNode groundTile))
                    {
                        PathFinder pathFinder = new PathFinder(groundNode.gridPos, groundTile.gridPos, 
                            true, _pathPoints);
                        pathFinder.OnFinishPath += OnFinishPath;
                    }
                }
            }

            entity.AimTarget = _targetEntity.transform.position + new Vector3(0f, Entity.HEIGHT / 2f);

            if (_path.Count > 0)
            {
                Debug.DrawLine(entity.transform.position, moveTarget, Color.red);
                Vector2Int moveTargetGridPos = _navmeshManager.GetNodeGridPos(moveTarget);

                if (entity.TryGetNearestGroundTile(out NavmeshNode groundNode) && groundNode.gridPos == moveTargetGridPos)
                {
                    SelectNextPathPoint();
                }

                entity.moveInput = Vector2.right * Mathf.Sign(moveTarget.x - entity.transform.position.x);
            }
            else
                entity.moveInput = Vector2.zero;
        }

        private void SelectNextPathPoint()
        {
            _path.Pop();
            _path.TryPeek(out moveTarget);
        }
        
        private void OnFinishPath()
        {
            _path = new Stack<Vector2>(_pathPoints);
            
            SelectNextPathPoint();
        }

        public ChaseTargetBehavior(AIController controller) : base(controller)
        {
        }
    }
}