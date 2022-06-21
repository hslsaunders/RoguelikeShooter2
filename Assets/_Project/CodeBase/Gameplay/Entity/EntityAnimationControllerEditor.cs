using System;
using _Project.CodeBase.Gameplay.Entity;
using UnityEditor;
using UnityEngine;

namespace _Project.CodeBase.Player
{
    [CustomEditor(typeof(HumanoidAnimationController))]
    public class EntityAnimationControllerEditor : Editor
    {
        private bool _debug;
        private string _timeDeltaString;
        private HumanoidAnimationController _humanoidAnimationController;

        private void OnValidate()
        {
            if (_humanoidAnimationController == null)
                _humanoidAnimationController = (HumanoidAnimationController)target;
        }
    }
}