using UnityEngine;

namespace _Project.CodeBase
{
    public class PrefabReferenceService : GameService<PrefabReferenceService>
    {
        [field: SerializeField] public GameObject BulletImpactFleshParticleSystem { get; private set; }
    }
}