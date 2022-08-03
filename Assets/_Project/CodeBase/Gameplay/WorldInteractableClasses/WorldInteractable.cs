using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.WorldInteractableClasses
{
    public abstract class WorldInteractable : MonoBehaviour
    {
        public static List<WorldInteractable> interactables = new List<WorldInteractable>();
        public bool Toggled { get; private set; }
        public Action OnActivate;
        public Action OnDeactivate;

        private void Start()
        {
            interactables.Add(this);
        }

        private void OnDestroy()
        {
            interactables.Remove(this);
        }

        public virtual void Interact() {}
        
        public virtual void Activate()
        {
            Toggled = true;
            OnActivate.Invoke();
        }

        public virtual void Deactivate()
        {
            Toggled = false;
            OnDeactivate.Invoke();
        }
    }
}