using UnityEditor;
using UnityEngine;

namespace _Project.CodeBase.UI
{
    public class Crosshair : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            
        }

        private void Update()
        {
            Vector2 mousePos = Input.mousePosition.SetZ(0f);
            _rectTransform.position = mousePos;
            Cursor.visible = !Utils.MouseInWindow();
            // _rectTransform.sizeDelta = 
        }
    }
}