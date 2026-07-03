using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class Level02RuntimeResetTests
    {
        [Test]
        public void LevelEntryPointResetClearsCompletionTrigger()
        {
            GameObject levelObject = new GameObject("Level");
            GameObject triggerObject = new GameObject("Completion");
            try
            {
                LevelEntryPoint entryPoint = levelObject.AddComponent<LevelEntryPoint>();
                BoxCollider completionCollider = triggerObject.AddComponent<BoxCollider>();
                completionCollider.isTrigger = true;
                LevelCompletionTrigger completionTrigger = triggerObject.AddComponent<LevelCompletionTrigger>();
                AssignObjectArray(entryPoint, "completionTriggers", completionTrigger);

                completionTrigger.TryComplete();
                Assert.IsTrue(completionTrigger.Fired);

                entryPoint.RuntimeReset();

                Assert.IsFalse(completionTrigger.Fired);
            }
            finally
            {
                Object.DestroyImmediate(levelObject);
                Object.DestroyImmediate(triggerObject);
            }
        }

        private static void AssignObjectArray(Object target, string propertyName, params Object[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
