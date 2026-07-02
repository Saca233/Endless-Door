using System.IO;
using UnityEditor;
using UnityEngine;

namespace OwariNakiTobira.Editor
{
    public static class StoryPrototypeMenu
    {
        private const string DialogueAssetPath = "Assets/_Project/ScriptableObjects/SampleDialogueSequence.asset";
        private const string StoryAssetPath = "Assets/_Project/ScriptableObjects/SampleStorySequence.asset";
        private const string DoorPrefabPath = "Assets/_Project/Prefabs/PrototypeDoor.prefab";

        [MenuItem("Tools/OwariNakiTobira/Story/Create Sample Dialogue Sequence")]
        public static void CreateSampleDialogueSequenceMenu()
        {
            DialogueSequence asset = CreateSampleDialogueSequence(true);
            Selection.activeObject = asset;
        }

        [MenuItem("Tools/OwariNakiTobira/Story/Create Sample Story Sequence")]
        public static void CreateSampleStorySequenceMenu()
        {
            StorySequence asset = CreateSampleStorySequence(true);
            Selection.activeObject = asset;
        }

        [MenuItem("Tools/OwariNakiTobira/Story/Create Story Trigger Volume")]
        public static void CreateStoryTriggerVolume()
        {
            GameObject trigger = new GameObject("StoryTriggerVolume");
            BoxCollider collider = trigger.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(2f, 2f, 1f);
            trigger.AddComponent<StoryTriggerVolume>();
            Selection.activeGameObject = trigger;
        }

        [MenuItem("Tools/OwariNakiTobira/Story/Create Prototype Door")]
        public static void CreatePrototypeDoor()
        {
            if (File.Exists(DoorPrefabPath) && !EditorUtility.DisplayDialog("Overwrite Prototype Door", "PrototypeDoor prefab already exists. Overwrite it?", "Overwrite", "Cancel"))
            {
                return;
            }

            EnsureParentFolder(DoorPrefabPath);
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "PrototypeDoor";
            door.transform.localScale = new Vector3(0.35f, 2.2f, 1.2f);
            door.AddComponent<DoorController>();
            PrefabUtility.SaveAsPrefabAssetAndConnect(door, DoorPrefabPath, InteractionMode.UserAction);
            Selection.activeGameObject = door;
            AssetDatabase.Refresh();
        }

        private static DialogueSequence CreateSampleDialogueSequence(bool allowOverwrite)
        {
            if (File.Exists(DialogueAssetPath))
            {
                if (!allowOverwrite || !EditorUtility.DisplayDialog("Overwrite Sample Dialogue", "SampleDialogueSequence already exists. Overwrite it?", "Overwrite", "Cancel"))
                {
                    return AssetDatabase.LoadAssetAtPath<DialogueSequence>(DialogueAssetPath);
                }
            }

            EnsureParentFolder(DialogueAssetPath);
            DialogueSequence sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            sequence.SetLines(new[]
            {
                new DialogueLineData("\u6708\u5d0e", "\u3053\u3053\u306f\u2026\u2026\u30c7\u30b9\u30af\u30c8\u30c3\u30d7\u306e\u4e2d\uff1f", "confused"),
                new DialogueLineData("\u30b7\u30b9\u30c6\u30e0", "\u30a6\u30a3\u30f3\u30c9\u30a6\u3092\u52d5\u304b\u3059\u3068\u3001\u4e16\u754c\u306e\u5f62\u304c\u5909\u308f\u308a\u307e\u3059\u3002", "neutral"),
                new DialogueLineData("\u6708\u5d0e", "\u306a\u3089\u3001\u6249\u307e\u3067\u306e\u9053\u3082\u4f5c\u308c\u308b\u306f\u305a\u3002", "determined")
            });
            ReplaceAsset(sequence, DialogueAssetPath);
            return sequence;
        }

        private static StorySequence CreateSampleStorySequence(bool allowOverwrite)
        {
            if (File.Exists(StoryAssetPath))
            {
                if (!allowOverwrite || !EditorUtility.DisplayDialog("Overwrite Sample Story", "SampleStorySequence already exists. Overwrite it?", "Overwrite", "Cancel"))
                {
                    return AssetDatabase.LoadAssetAtPath<StorySequence>(StoryAssetPath);
                }
            }

            DialogueSequence dialogue = CreateSampleDialogueSequence(false);
            EnsureParentFolder(StoryAssetPath);
            StorySequence sequence = ScriptableObject.CreateInstance<StorySequence>();
            sequence.SetCommands(new[]
            {
                new StorySequenceCommandData(StoryCommandType.LockPlayer),
                StorySequenceCommandData.ShowDialogue(dialogue),
                StorySequenceCommandData.SetBoolFlag("sample.intro_seen", true, true),
                StorySequenceCommandData.Wait(0.25f),
                new StorySequenceCommandData(StoryCommandType.UnlockPlayer)
            });
            ReplaceAsset(sequence, StoryAssetPath);
            return sequence;
        }

        private static void ReplaceAsset(Object asset, string path)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void EnsureParentFolder(string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
    }
}
