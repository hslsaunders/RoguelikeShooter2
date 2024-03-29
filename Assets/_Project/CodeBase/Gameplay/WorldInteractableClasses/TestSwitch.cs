﻿using UnityEngine;

namespace _Project.CodeBase.Gameplay.WorldInteractableClasses
{
    public class TestSwitch : WorldSwitchInteractable
    {
        private SpriteRenderer _spriteRenderer;

        protected override void Start()
        {
            base.Start();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public override void Activate()
        {
            base.Activate();

            _spriteRenderer.color = Color.red;
        }

        public override void Deactivate()
        {
            base.Deactivate();

            _spriteRenderer.color = Color.white;
        }
    }
}