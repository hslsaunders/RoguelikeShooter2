using _Project.CodeBase.Gameplay.EntityClasses;
using UnityEngine;

namespace _Project.CodeBase.Player
{
    public class Player : MonoSingleton<Player>
    {
        public Entity entity;
        private bool _controllingAim = true;
        private void Start()
        {
            if (entity == null)
                Debug.LogWarning("Player's Entity is null");
        }

        private void Update()
        {
            if (entity == null) return;
            
            if (GameControls.DebugResetPosition.IsPressed)
            {
                entity.transform.position = Vector3.zero;
            }

            if (GameControls.ToggleControlAim.IsPressed)
            {
                _controllingAim = !_controllingAim;
                if (!_controllingAim)
                    Debug.Break();
            }

            if (_controllingAim)
                entity.AimTarget = Utils.WorldMousePos;
            entity.moveInput = GameControls.DirectionalInput;

            if (GameControls.Jump.IsPressed)
                entity.Controller.Jump();

            if (GameControls.Shoot.IsHeld)
                entity.TryShoot();
            else
                entity.StopShooting();
        }
    }
}