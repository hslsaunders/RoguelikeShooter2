using UnityEngine;

namespace _Project.CodeBase.Gameplay.HoldableClasses
{
    [System.Serializable]
    public class TransformOrientation
    {
        public Transform parent;
        public Vector2 position;
        public Vector3 rotation;
        public TransformOrientation startingOrientation;
        

        public TransformOrientation(Transform transform)
        {
            position = transform.position;
            rotation = transform.rotation.eulerAngles;
        }

        public TransformOrientation(Vector2 position, Vector3 rotation, Transform parent)
        {
            this.parent = parent;
            this.position = position;
            this.rotation = rotation;
            startingOrientation = new TransformOrientation(this);
        }

        public TransformOrientation(Vector2 position, Vector3 rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public TransformOrientation(TransformOrientation orientation)
        {
            position = orientation.position;
            rotation = orientation.rotation;
            parent = orientation.parent;
        }

        public void SetOrientation(Vector2 pos, Vector3 rotation)
        {
            position = pos;
            this.rotation = rotation;
        }
    }
}