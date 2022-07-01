using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class EntityComponent : MonoBehaviour
    {
        protected Entity entity;

        protected virtual void OnValidate()
        {
            if (entity == null)
                entity = GetComponent<Entity>();
        }

        protected void Awake()
        {
        }
        protected virtual void Start() {}
        protected virtual void Update() {}
        protected virtual void FixedUpdate() {}
    }
}