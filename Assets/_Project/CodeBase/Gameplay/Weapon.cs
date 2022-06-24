using UnityEngine;
using UnityEngine.Events;

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
        [Range(0f, 90)] public float highestAimAngle;
        [Range(0f, 90)] public float lowestAimAngle;
        [SerializeField] private bool _fullAuto;
        [SerializeField] private float _fireDelay;
        [HideInInspector] public LayerMask hitMask;
        [HideInInspector] public UnityEvent onFire;
        public float minDistToAimPivot;
        private bool _triggerDown;
        private float _lastFireTime;
        
        public BezierCurve _holdCurve;
        
        private void OnValidate()
        {
            if (_fireDelay <= 0f)
                _fireDelay = 0f;
            
            _holdCurve.SetOriginTransforms();
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
            newProj.transform.right = -_shootTransform.up;
            newProj.transform.position = _shootTransform.position;
        }
        

        public Vector2 GetHoldPosFromAimAngleRatio(float ratio) => _holdCurve.GetWorldCurvePoint(ratio);

        public Vector2 GetLocalHoldPosFromAimAngleRatio(float ratio)
        {
            Vector2 localCurvePos = _holdCurve.GetLocalCurvePoint(ratio);
            if (_holdCurve.originTransform == null)
                return localCurvePos;
            return _holdCurve.originTransform.TransformPoint(localCurvePos);
        }
    }
}