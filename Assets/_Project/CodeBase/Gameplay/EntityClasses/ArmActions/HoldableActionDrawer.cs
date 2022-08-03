using UnityEditor;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.HoldableClasses.ArmActions
{
    //[CustomPropertyDrawer(typeof(ArmAction))]
    public class HoldableActionDrawer : PropertyDrawer
    {
        
        /*
        private float totalHeight;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return totalHeight + 6f; //EditorGUIUtility.singleLineHeight * numProperties + 6;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            float propertyHeight = 18f;
            float currentY = position.y;
            totalHeight = 0f;
            
            EditorGUI.LabelField(new Rect(position.x, currentY, position.width, propertyHeight), label);

            EditorGUI.indentLevel++;
            
            AddRegularProperty(position, ref currentY, propertyHeight, "actionType", property);
            AddRegularProperty(position, ref currentY, propertyHeight, "numHandsRequired", property);
            
            AddRegularProperty(position, ref currentY, propertyHeight, "test", property);
            ArmActionType type = (ArmActionType)property.FindPropertyRelative("actionType").intValue;
            switch (type)
            {
                case ArmActionType.Animation:
                    
                    AddRegularProperty(position, ref currentY, propertyHeight, "animateTestValue", property);
                    AddRegularProperty(position, ref currentY, propertyHeight, "testValue", property);
                    SerializedProperty animations = property.FindPropertyRelative("animations");
                    currentY += propertyHeight;
                    totalHeight += propertyHeight; // for animations label 

                    if (animations.isExpanded)
                    {
                        if (animations.arraySize >= 1)
                            totalHeight += 235f * animations.arraySize;
                        else
                            totalHeight += 65f;
                    }

                    var animationsRect = new Rect(position.x, currentY, position.width, propertyHeight);
                    EditorGUI.PropertyField(animationsRect, property.FindPropertyRelative("animations"));
                    break;
                case ArmActionType.Target:
                    //EditorGUI.EndProperty();
                    //SerializedProperty targetLocation = property.FindPropertyRelative("targetLocation");
                    //EditorGUI.BeginProperty(position, label, targetLocation);
                    break;
            }
            
            EditorGUI.EndProperty();
            EditorGUI.indentLevel--;
            
            
            
            /*
            ArmActionType actionType;
            int numHandsRequired;
            List<SerializableAnimation> animations = new List<SerializableAnimation>();
            TransformOrientation targetLocation;
            bool test;
            bool animateTestValue;
            float testValue;
            #1#
        }

        private void AddRegularProperty(Rect startingRect, ref float currentY, float propertyHeight, string propertyName, SerializedProperty property)
        {
            currentY += propertyHeight;
            totalHeight += propertyHeight + 2f;
            var propertyRect = new Rect(startingRect.x, currentY, startingRect.width, propertyHeight);
            EditorGUI.PropertyField(propertyRect, property.FindPropertyRelative(propertyName));
        }
        */
        
    }
}