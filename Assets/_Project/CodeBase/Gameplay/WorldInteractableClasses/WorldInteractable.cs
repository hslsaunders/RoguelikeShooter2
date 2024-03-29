﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.CodeBase.Gameplay.WorldInteractableClasses
{
    public abstract class WorldInteractable : MonoBehaviour
    {
        public static List<WorldInteractable> interactables = new List<WorldInteractable>();
        public int handsRequired;
        public Transform interactTransform;
        public bool BeingInteractedWith { get; protected set; }
        public bool Toggled { get; private set; }
        public UnityEvent onActivate;
        public UnityEvent onDeactivate;
        public UnityEvent onFinishInteract;

        protected virtual void Start()
        {
            if (!interactables.Contains(this))
                interactables.Add(this);
        }

        protected virtual void OnDestroy()
        {
            if (interactables.Contains(this))
                interactables.Remove(this);
        }

        public virtual void Interact() {}
        
        public virtual void Activate()
        {
            Toggled = true;
            onActivate?.Invoke();
        }

        public virtual void Deactivate()
        {
            Toggled = false;
            onDeactivate?.Invoke();
        }

        public virtual void FinishInteraction()
        {
            BeingInteractedWith = false;
            onFinishInteract?.Invoke();
        }

        public virtual void CancelInteraction()
        {
            BeingInteractedWith = false;
        }

        public virtual void FlipActivateState()
        {
            if (Toggled)
                Deactivate();
            else
                Activate();
        }
    }
}