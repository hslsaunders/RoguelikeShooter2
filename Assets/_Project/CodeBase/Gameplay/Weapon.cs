using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.CodeBase.Gameplay
{
    public class Weapon : Holdable
    {
        [SerializeField] private GameObject _projectilePrefab;
        public Transform _shootTransform;
        [Range(0f, 3f)] public float recoilStrength;
        [Range(0f, 90f)] public float maxSpread;
        [Range(0f, 1f)] public float spreadGrowthRate;
        [HideInInspector] public int teamId;
        public float Spread => maxSpread * _spreadEffect; 
        public float minDistToAimPivot;
        private float _spreadEffect = 0f;
        private float _fireAngle;

        private const float SPREAD_DECAY_RATE = 2f;
        private const float PING_PONG_RATE = 1f;

        private void OnValidate()
        {
            if (_fireDelay <= 0f)
                _fireDelay = 0f;
            
            holdCurve.SetOriginTransforms();
        }

        protected override void Update()
        {
            base.Update();
            _spreadEffect = Mathf.Clamp(_spreadEffect - SPREAD_DECAY_RATE * Time.deltaTime, 0f, 1f);
            
            //float pingPongValue = Mathf.PingPong(PING_PONG_RATE * Time.deltaTime, 1f);
            //_fireAngle = pingPongValue.Remap(0f, 1f, -Spread * 2f, Spread * 2f);
        }

        public override void Fire()
        {
            Projectile newProj = Instantiate(_projectilePrefab).GetComponent<Projectile>();
            newProj.teamId = teamId;

            float spread = Spread;
            float spreadAngle = Random.Range(-spread, spread);
            Vector2 shootDir = Utils.RotateDirectionByAngle(-_shootTransform.up, spreadAngle);
            newProj.transform.right = shootDir;
            //Debug.DrawRay(_shootTransform.position, shootDir * 1f, Color.red, .5f);
            _spreadEffect = Mathf.Clamp01(_spreadEffect + spreadGrowthRate);
            
            newProj.transform.eulerAngles = newProj.transform.eulerAngles.SetX(0f);
            newProj.transform.eulerAngles = newProj.transform.eulerAngles.SetY(0f);
            newProj.transform.position = _shootTransform.position;
        }
    }
}