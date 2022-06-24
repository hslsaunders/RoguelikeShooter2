using UnityEngine;

namespace _Project.CodeBase.Gameplay
{
    public class TimeController : MonoBehaviour
    {
        [SerializeField] private float _timeScale;
        private void Update()
        {
            Time.timeScale = _timeScale;
        }
    }
}