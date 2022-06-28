using UnityEngine;

namespace _Project.CodeBase
{
    public static class GameControls
    {
        // Gameplay
        //    Movement
        public static readonly KeyBind Up = new KeyBind(KeyCode.W);
        public static readonly KeyBind Down = new KeyBind(KeyCode.S);
        public static readonly KeyBind Left = new KeyBind(KeyCode.A);
        public static readonly KeyBind Right = new KeyBind(KeyCode.D);
        public static readonly KeyBind Jump = new KeyBind(KeyCode.Space);
        public static readonly KeyBind Walk = new KeyBind(KeyCode.LeftControl);
        
        //    Combat
        public static readonly KeyBind Shoot = new KeyBind(KeyCode.Mouse0);
        public static readonly KeyBind ToggleControlAim = new KeyBind(KeyCode.Escape);
        

        public static readonly KeyBind InteractUI = new KeyBind(KeyCode.Mouse0);
        public static readonly KeyBind CancelInteractUI = new KeyBind(KeyCode.Mouse1);

        public static readonly KeyBind DebugResetPosition = new KeyBind(KeyCode.R);
        public static float MouseYInput => Input.GetAxisRaw("Mouse Y");
        public static float MouseXInput => Input.GetAxisRaw("Mouse X");
        public static Vector2 MouseInput => new Vector2(MouseXInput, MouseYInput);
        public static Vector2 DirectionalInput => GetDirectionalAxis(Up, Down, Left, Right);

        public static Vector2 GetDirectionalAxis(KeyBind up, KeyBind down, KeyBind left, KeyBind right) => 
            new Vector2(GetAxis(right, left), GetAxis(up, down)).normalized;
        
        public static float GetAxis(KeyBind positive, KeyBind negative)
        {
            float axis = 0;
            if (positive.IsHeld)
                axis += 1f;
            if (negative.IsHeld)
                axis -= 1f;
            return axis;
        }
    }
}