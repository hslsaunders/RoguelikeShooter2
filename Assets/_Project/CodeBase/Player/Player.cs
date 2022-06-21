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
            
            controller.AimTarget = Utils.WorldMousePos;
            controller.moveInput = GameControls.DirectionalInput;
            
            if (GameControls.Jump.IsPressed)
                controller.Jump();
        }
    }
}