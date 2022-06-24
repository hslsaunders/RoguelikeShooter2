using _Project.CodeBase.Gameplay;
using _Project.Codebase.Misc;
using UnityEngine;

namespace _Project.CodeBase.Player
{
    [ExecuteAlways]
    public class Weapon : MonoBehaviour
    {
        public Transform secondaryPivot;
        [SerializeField] private GameObject _projectilePrefab;
        public Transform _shootTransform;
        [Range(0f, 90)] public float highestAimAngle;
        [Range(0f, 90)] public float lowestAimAngle;
        [SerializeField] private bool _fullAuto;
        [SerializeField] private float _fireDelay;
        [HideInInspector] public LayerMask hitMask;

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

            _lastFireTime = Time.time;
            
            Projectile newProj = Instantiate(_projectilePrefab).GetComponent<Projectile>();
            newProj.hitmask = hitMask;
            newProj.transform.right = -_shootTransform.up;
            newProj.transform.position = _shootTransform.position;
        }
        

        public Vector2 GetHoldPosFromAimAngleRatio(float ratio) => _holdCurve.GetWorldCurvePoint(ratio);
    }
}