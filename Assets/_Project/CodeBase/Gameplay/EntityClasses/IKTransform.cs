using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    [System.Serializable]
    public class IKTransform
    {
        [field: SerializeField] public Transform IKTarget { get; private set; }
        [field: SerializeField] public Transform AnimationTarget { get; private set; }
        [field: SerializeField] public bool DisableTranslation { get; private set; }
        [field: SerializeField] public bool DisableRotation { get; private set; }
    }
}