using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OwariNakiTobira.Editor
{
    public static class Stage2CoverPuzzleSetupMenu
    {
        private const string Stage2ScenePath = "Assets/_Project/Scenes/stage2.unity";
        private const string StageRootName = "Stage2_Basic";
        private const string GameplayWorldName = "GameplayWorld";
        private const string ObstaclesName = "Obstacles";

        [MenuItem("Tools/OwariNakiTobira/Setup Stage2 Cover Puzzle Support")]
        public static void SetupStage2CoverPuzzleSupport()
        {
            if (!File.Exists(Stage2ScenePath))
            {
                Debug.LogError("[Stage2 Cover Puzzle] Missing stage2 scene at " + Stage2ScenePath);
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(Stage2ScenePath, OpenSceneMode.Single);
            Transform stageRoot = FindRoot(scene, StageRootName);
            if (stageRoot == null)
            {
                Debug.LogError("[Stage2 Cover Puzzle] Could not find Stage2_Basic root.");
                return;
            }

            Transform gameplayWorld = GetOrCreateChild(stageRoot, GameplayWorldName);
            Transform obstacles = GetOrCreateChild(gameplayWorld, ObstaclesName);
            AssignLayerRecursively(obstacles.gameObject, GameplayWorldName);
            EnsureBinder(stageRoot);
            int repaired = RepairPuzzleTargets(gameplayWorld);
            bool createdSample = EnsureAtLeastOnePuzzleWall(obstacles);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Stage2ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Stage2 Cover Puzzle] Setup complete. Repaired targets: {repaired}. Created sample wall: {createdSample}.");
        }

        private static void EnsureBinder(Transform stageRoot)
        {
            if (stageRoot.GetComponent<StageCoverPuzzleAutoBinder>() == null)
            {
                stageRoot.gameObject.AddComponent<StageCoverPuzzleAutoBinder>();
                EditorUtility.SetDirty(stageRoot.gameObject);
            }
        }

        private static int RepairPuzzleTargets(Transform root)
        {
            int repaired = 0;
            WindowPuzzleTarget[] existingTargets = root.GetComponentsInChildren<WindowPuzzleTarget>(true);
            for (int i = 0; i < existingTargets.Length; i++)
            {
                ConfigureTarget(existingTargets[i]);
                repaired++;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                GameObject targetObject = renderers[i].gameObject;
                if (!targetObject.name.StartsWith("CoverPuzzleWall", System.StringComparison.Ordinal)
                    || targetObject.GetComponent<WindowPuzzleTarget>() != null
                    || targetObject.GetComponent<Collider>() == null)
                {
                    continue;
                }

                WindowPuzzleTarget target = targetObject.AddComponent<WindowPuzzleTarget>();
                ConfigureTarget(target);
                repaired++;
            }

            return repaired;
        }

        private static bool EnsureAtLeastOnePuzzleWall(Transform obstacles)
        {
            if (obstacles.GetComponentInChildren<WindowPuzzleTarget>(true) != null)
            {
                return false;
            }

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "CoverPuzzleWall_Stage2_01";
            wall.transform.SetParent(obstacles, false);
            wall.transform.position = new Vector3(6.1f, 0.15f, 0f);
            wall.transform.localScale = new Vector3(0.8f, 2.5f, 1.2f);
            AssignLayerRecursively(wall, GameplayWorldName);
            SetRendererColor(wall, new Color(0.2f, 0.23f, 0.3f, 1f));

            WindowPuzzleTarget target = wall.AddComponent<WindowPuzzleTarget>();
            ConfigureTarget(target);
            return true;
        }

        private static void ConfigureTarget(WindowPuzzleTarget target)
        {
            if (target == null)
            {
                return;
            }

            target.SetAffectedObjects(
                target.GetComponentsInChildren<Renderer>(true),
                target.GetComponentsInChildren<Collider>(true));
            EditorUtility.SetDirty(target);
        }

        private static Transform FindRoot(Scene scene, string rootName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == rootName)
                {
                    return roots[i].transform;
                }
            }

            return roots.Length > 0 ? roots[0].transform : null;
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void SetRendererColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            renderer.sharedMaterial = new Material(shader)
            {
                color = color
            };
        }

        private static void AssignLayerRecursively(GameObject root, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                return;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                children[i].gameObject.layer = layer;
            }
        }
    }
}
