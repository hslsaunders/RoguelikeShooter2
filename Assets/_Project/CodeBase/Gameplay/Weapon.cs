using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace _Project.CodeBase.Gameplay
{
    [ExecuteAlways]
    public class Weapon : MonoBehaviour
    {
        public Transform primaryPivot;
        public Transform secondaryPivot;
        [SerializeField] private GameObject _projectilePrefab;
        public Transform _shootTransform;
        [Range(.1f, 3f)] public float weight;
        [Range(0f, 3f)] public float recoilStrength;
        [Range(0f, 90f)] public float maxSpread;
        [Range(0f, 1f)] public float spreadGrowthRate; 
        [Range(0f, 90)] public float highestAimAngle;
        [Range(0f, 90)] public float lowestAimAngle;
        [SerializeField] private bool _fullAuto;
        [SerializeField] private float _fireDelay;
        [HideInInspector] public LayerMask hitMask;
        [HideInInspector] public UnityEvent onFire;
        public float Spread => maxSpread * _spreadEffect; 
        public float minDistToAimPivot;
        private bool _triggerDown;
        private float _lastFireTime;
        private float _spreadEffect = 0f;
        private float _fireAngle;

        private const float SPREAD_DECAY_RATE = 2f;
        private const float PING_PONG_RATE = 1f;
        
        public BezierCurve _holdCurve;
        
        private void OnValidate()
        {
            if (_fireDelay <= 0f)
                _fireDelay = 0f;
            
            _holdCurve.SetOriginTransforms();
        }

        private void Update()
        {
            _spreadEffect = Mathf.Clamp(_spreadEffect - SPREAD_DECAY_RATE * Time.deltaTime, 0f, 1f);
            float pingPongValue = Mathf.PingPong(PING_PONG_RATE * Time.deltaTime, 1f);
            _fireAngle = pingPongValue.Remap(0f, 1f, -Spread * 2f, Spread * 2f);
        }

        public void SetFireTriggerState(bool down)
        {
            if (_triggerDown && down && !_fullAuto) return;
            
            _triggerDown = down;

            if (!_triggerDown || Time.time < _lastFireTime + _fireDelay) return;

            onFire.Invoke();

            _lastFireTime = Time.time;
            
            Projectile newProj = Instantiate(_projectilePrefab).GetComponent<Projectile>();
            newProj.hitmask = hitMask;

            float spread = Spread;
            float spreadAngle = Random.Range(-spread, spread);
            Vector2 shootDir = Utils.RotateDirectionByAngle(-_shootTransform.up, spreadAngle);
            newProj.transform.right = shootDir;
            Debug.DrawRay(_shootTransform.position, shootDir * 1f, Color.red, .5f);
            _spreadEffect = Mathf.Clamp01(_spreadEffect + spreadGrowthRate);
            
            newProj.transform.eulerAngles = newProj.transform.eulerAngles.SetX(0f);
            newProj.transform.eulerAngles = newProj.transform.eulerAngles.SetY(0f);
            newProj.transform.position = _shootTransform.position;
        }
        

        public Vector2 GetHoldPosFromAimAngleRatio(float ratio) => _holdCurve.GetWorldCurvePoint(ratio);

        public Vector2 GetLocalHoldPosFromAimAngleRatio(float ratio)
        {
            Vector2 localCurvePos = _holdCurve.GetLocalCurvePoint(ratio);
            return localCurvePos;
            //if (_holdCurve.originTransform == null)
            //    return localCurvePos;
            //return _holdCurve.originTransform.TransformPoint(localCurvePos);
        }
    }
}