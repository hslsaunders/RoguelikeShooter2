using System.Collections.Generic;
using System.Diagnostics;
using _Project.CodeBase.Gameplay.EntityClasses;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace _Project.CodeBase.Player
{
    public class PlayerManager : MonoSingleton<PlayerManager>
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

            if (GameControls.EquipWeaponOne.IsPressed)
                entity.EquipWeapon(0);
            if (GameControls.EquipWeaponTwo.IsPressed)
                entity.EquipWeapon(1);
            if (GameControls.EquipWeaponThree.IsPressed)
                entity.EquipWeapon(2);
            
            if (GameControls.EquipPrimaryHoldable.IsPressed)
                entity.EquipHoldable(0);
            
            if (GameControls.FirePrimary.IsHeld)
            {
                entity.FirePrimary();
            }
            else
            {
                entity.StopFiringPrimary();
            }
            
            if (GameControls.FireSecondaries.IsHeld)
            {
                entity.FireSecondaries();
            }
            else
            {
                entity.StopFireSecondaries();
            }
        }
    }
}