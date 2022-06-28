using UnityEngine;

namespace _Project.CodeBase
{
    [ExecuteAlways]
    public class Laser : MonoBehaviour
    {
        public LayerMask hitMask;
        public bool laserActive;
        [SerializeField] private Transform _laserSource;
        [SerializeField] private float _distance;
        private LineRenderer _lineRenderer;

        private void Start()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        private void LateUpdate()
        {
            _lineRenderer.enabled = laserActive;
            if (!laserActive) return;
            
            Vector2 secondPoint;

            Vector2 direction = (_laserSource.right * _laserSource.lossyScale.x).normalized;
            
            RaycastHit2D hitInfo = Physics2D.Raycast(_laserSource.position, direction,
                _distance, hitMask);
            if (hitInfo)
                secondPoint = hitInfo.point;
            else
                secondPoint = (Vector2)_laserSource.transform.position + (direction * _distance);
            _lineRenderer.SetPositions(new Vector3[] {_laserSource.transform.position, secondPoint});
        }
    }
}