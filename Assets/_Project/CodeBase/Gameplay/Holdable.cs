using UnityEngine;
using UnityEngine.Events;

namespace _Project.CodeBase.Gameplay
{
    public class Holdable : MonoBehaviour
    {
        public bool oneHanded;
        public Transform primaryPivot;
        public Transform secondaryPivot;
        [Range(0f, 90)] public float highestAimAngle;
        [Range(0f, 90)] public float lowestAimAngle;
        [SerializeField] protected bool _fullAuto;
        [SerializeField] protected float _fireDelay;
        [Range(.1f, 3f)] public float weight;
        [HideInInspector] public UnityEvent onFire;
        public BezierCurve holdCurve;
        private float _lastFireTime;
        private bool _triggerDown;
        private void OnValidate()
        {
            if (_fireDelay <= 0f)
                _fireDelay = 0f;
            
            holdCurve.SetOriginTransforms();
        }
        

        public void SetFireTriggerState(bool down)
        {
            if (_triggerDown && down && !_fullAuto) return;
            
            _triggerDown = down;

            if (!_triggerDown || Time.time < _lastFireTime + _fireDelay) return;

            onFire.Invoke();

            _lastFireTime = Time.time;
            
            Fire();
        }
        
        public virtual void Fire() {}

        public Vector2 GetHoldPosFromAimAngleRatio(float ratio) => holdCurve.GetWorldCurvePoint(ratio);

        public Vector2 GetLocalHoldPosFromAimAngleRatio(float ratio)
        {
            Vector2 localCurvePos = holdCurve.GetLocalCurvePoint(ratio);
            return localCurvePos;
            //if (_holdCurve.originTransform == null)
            //    return localCurvePos;
            //return _holdCurve.originTransform.TransformPoint(localCurvePos);
        }
    }
}