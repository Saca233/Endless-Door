using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OwariNakiTobira.Editor
{
    public static class DoorStage2TransitionSetupMenu
    {
        private const string DesktopHostScenePath = "Assets/_Project/Scenes/DesktopHostPrototype.unity";
        private const string Stage2ScenePath = "Assets/_Project/Scenes/stage2.unity";
        private const string TriggerName = "DoorToStage2InteractTrigger";

        [MenuItem("Tools/OwariNakiTobira/Setup Door To Stage2 Transition")]
        public static void SetupDoorToStage2Transition()
        {
            if (!File.Exists(DesktopHostScenePath))
            {
                Debug.LogError("[Door Stage2] Missing DesktopHostPrototype scene.");
                return;
            }

            if (!File.Exists(Stage2ScenePath))
            {
                Debug.LogError("[Door Stage2] Missing stage2 scene at " + Stage2ScenePath);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(DesktopHostScenePath, OpenSceneMode.Single);
            List<string> repairs = new List<string>();
            Transform environment = FindSceneTransform(scene, "Environment");
            Transform door = FindSceneTransform(scene, "door") ?? FindSceneTransform(scene, "Door");

            if (environment == null)
            {
                Debug.LogError("[Door Stage2] Could not find GameplayWorld/Environment.");
                return;
            }

            if (door == null)
            {
                Debug.LogError("[Door Stage2] Could not find object named 'door'.");
                return;
            }

            GameObject trigger = GetOrCreateTrigger(environment, repairs);
            ConfigureTrigger(trigger, door, repairs);
            EnsureSceneInBuildSettings(Stage2ScenePath, repairs);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DesktopHostScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("[Door Stage2] Setup complete.\n- " + string.Join("\n- ", repairs));
        }

        private static GameObject GetOrCreateTrigger(Transform environment, List<string> repairs)
        {
            Transform existing = FindInChildrenByName(environment, TriggerName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject trigger = new GameObject(TriggerName, typeof(BoxCollider));
            trigger.transform.SetParent(environment, false);
            repairs.Add("Created " + TriggerName);
            return trigger;
        }

        private static void ConfigureTrigger(GameObject trigger, Transform door, List<string> repairs)
        {
            trigger.transform.position = door.position + Vector3.left * 0.85f + Vector3.up * 0.5f;
            trigger.transform.rotation = Quaternion.identity;
            trigger.transform.localScale = Vector3.one;

            BoxCollider box = trigger.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = trigger.AddComponent<BoxCollider>();
            }

            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(1.4f, 2.4f, 1.8f);

            DoorInteractSceneTransition transition = trigger.GetComponent<DoorInteractSceneTransition>();
            if (transition == null)
            {
                transition = trigger.AddComponent<DoorInteractSceneTransition>();
                repairs.Add("Added DoorInteractSceneTransition");
            }

            SerializedObject serializedObject = new SerializedObject(transition);
            SetString(serializedObject, "targetSceneName", "stage2");
            SetEnum(serializedObject, "loadSceneMode", (int)LoadSceneMode.Single);
            SetBool(serializedObject, "triggerOnce", true);
            SetString(serializedObject, "requiredPlayerName", "PrototypePlayer");
            SetBool(serializedObject, "keepPersistentDesktopHost", true);
            SetString(serializedObject, "stageRootName", "Stage2_Basic");
            SetVector3(serializedObject, "fallbackStageSpawnPosition", new Vector3(-6f, 0.05f, 0f));
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(box);
            EditorUtility.SetDirty(transition);
            repairs.Add("Configured trigger in front of door");
        }

        private static void EnsureSceneInBuildSettings(string scenePath, List<string> repairs)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    if (!scenes[i].enabled)
                    {
                        scenes[i].enabled = true;
                        EditorBuildSettings.scenes = scenes;
                        repairs.Add("Enabled stage2 in Build Settings");
                    }

                    return;
                }
            }

            EditorBuildSettingsScene[] next = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
            {
                next[i] = scenes[i];
            }

            next[next.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = next;
            repairs.Add("Added stage2 to Build Settings");
        }

        private static Transform FindSceneTransform(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindInChildrenByName(roots[i].transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindInChildrenByName(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindInChildrenByName(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetEnum(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }

        private static void SetVector3(SerializedObject serializedObject, string propertyName, Vector3 value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
            }
        }
    }
}
