using System;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.WorldInteractableClasses
{
    public abstract class WorldInteractable : MonoBehaviour
    {
        public bool Toggled { get; private set; }
        public Action OnActivate;
        public Action OnDeactivate;

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