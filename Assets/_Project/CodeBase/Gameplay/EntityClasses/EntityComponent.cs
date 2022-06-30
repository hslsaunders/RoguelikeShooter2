using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    [RequireComponent(typeof(Entity))]
    public class EntityComponent<T> : MonoBehaviour
    {
        protected Entity entity;

        protected void Awake()
        {
            entity = GetComponent<Entity>();
        }

        protected virtual void Start() {}
        protected virtual void Update() {}
        protected virtual void FixedUpdate() {}
    }
}