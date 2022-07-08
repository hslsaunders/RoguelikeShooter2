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

            if (!CastedTarget.testPathfinder) return;
            
            AddPositionHandle(ref pathFindStart);
            AddPositionHandle(ref pathFindEnd);

            if (GUI.changed)
            {
                CastedTarget.testPathFindStart = CastedTarget.GetNodeGridPos(pathFindStart);
                CastedTarget.testPathFindEnd = CastedTarget.GetNodeGridPos(pathFindEnd);
            }
            else
            {
                pathFindStart = CastedTarget.NodePosToWorldPos(CastedTarget.testPathFindStart);
                pathFindEnd = CastedTarget.NodePosToWorldPos(CastedTarget.testPathFindEnd);
            }
        }
    }
}