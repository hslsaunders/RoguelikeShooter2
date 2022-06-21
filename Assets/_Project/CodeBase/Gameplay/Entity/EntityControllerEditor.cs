using _Project.Codebase.Misc;
using UnityEditor;

namespace _Project.CodeBase.Gameplay.Entity
{
    [CustomEditor(typeof(EntityController))]
    [CanEditMultipleObjects]
    public class EntityControllerEditor : CustomEditor<EntityController>
    {
        private float _debugSize = .05f;
        public void OnSceneGUI()
        {
            Utils.AddCircleHandle(ref CastedTarget.targetOffset, ref _debugSize);
        }

        protected override void DrawInspectorDebug()
        {
            base.DrawInspectorDebug();
        }
    }
}