using _Project.CodeBase.Gameplay.Entity;
using _Project.Codebase.Misc;
using UnityEngine;

namespace _Project.CodeBase.Player
{
    public class Player : MonoSingleton<Player>
    {
        public EntityController controller;

        private void Start()
        {
            if (controller == null)
                Debug.LogWarning("Player's Entity Controller is null");
        }

        private void Update()
        {
            if (controller == null) return;
            
            if (GameControls.DebugResetPosition.IsPressed)
            {
                controller.transform.position = Vector3.zero;
                controller.velocity = Vector3.zero;
            }
            
            controller.AimTarget = Utils.WorldMousePos;
            controller.moveInput = GameControls.DirectionalInput;
            
            if (GameControls.Jump.IsPressed)
                controller.Jump();

            if (GameControls.Shoot.IsHeld)
                controller.TryShoot();
            else
                controller.StopShooting();
        }
    }
}