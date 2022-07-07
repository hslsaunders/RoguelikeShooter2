using _Project.CodeBase.Navmesh;
using UnityEditor;
using UnityEngine;

namespace _Project.CodeBase.Editor
{
    [CustomEditor(typeof(NavmeshManager))]
    [CanEditMultipleObjects]
    public class NavmeshManagerEditor : CustomEditor<NavmeshManager>
    {
        protected override bool MakeInspectorDebugToggleable => false;
        private Vector2 pathFindStart;
        private Vector2 pathFindEnd;

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            AddPositionHandle(ref pathFindStart);
            AddPositionHandle(ref pathFindEnd);

            if (GUI.changed)
            {
                CastedTarget.pathFindStart = CastedTarget.GetNodeGridPos(pathFindStart);
                CastedTarget.pathFindEnd = CastedTarget.GetNodeGridPos(pathFindEnd);
            }
            else
            {
                pathFindStart = CastedTarget.GetWorldPosFromGridPos(CastedTarget.pathFindStart);
                pathFindEnd = CastedTarget.GetWorldPosFromGridPos(CastedTarget.pathFindEnd);
            }
        }
    }
}