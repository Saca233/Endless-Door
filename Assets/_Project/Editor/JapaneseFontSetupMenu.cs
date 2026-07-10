using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace OwariNakiTobira.Editor
{
    public static class JapaneseFontSetupMenu
    {
        private const string DesktopHostScenePath = "Assets/_Project/Scenes/DesktopHostPrototype.unity";
        private const string FontFolder = "Assets/_Project/Settings/Fonts";
        private const string ImportedFontPath = FontFolder + "/NotoSansJP-VF.ttf";
        private const string FontAssetPath = FontFolder + "/NotoSansJP_Dynamic.asset";

        private static readonly string[] CandidateSystemFonts =
        {
            "C:/Windows/Fonts/NotoSansJP-VF.ttf",
            "C:/Windows/Fonts/UDEVGothic-Regular.ttf",
            "C:/Windows/Fonts/meiryo.ttc",
            "C:/Windows/Fonts/YuGothR.ttc",
            "C:/Windows/Fonts/msgothic.ttc"
        };

        [MenuItem("Tools/OwariNakiTobira/Setup Japanese TMP Font")]
        public static void SetupJapaneseTmpFont()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ClearBrokenBootTextFontReferences();
            TMP_FontAsset fontAsset = RecreateJapaneseFontAsset();
            if (fontAsset == null)
            {
                Debug.LogError("[Japanese TMP Font] Could not create Japanese TMP font asset. Import a Japanese .ttf/.otf manually and assign it to BootLogText.");
                return;
            }

            AssignFontToDesktopHostBootText(fontAsset);
            Debug.Log("[Japanese TMP Font] Japanese TMP font setup complete.");
        }

        private static void ClearBrokenBootTextFontReferences()
        {
            if (!File.Exists(DesktopHostScenePath))
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(DesktopHostScenePath, OpenSceneMode.Single);
            TextMeshProUGUI[] textComponents = FindComponentsInScene<TextMeshProUGUI>(scene);
            bool changed = false;
            for (int i = 0; i < textComponents.Length; i++)
            {
                if (textComponents[i] == null || textComponents[i].name != "BootLogText")
                {
                    continue;
                }

                textComponents[i].font = null;
                EditorUtility.SetDirty(textComponents[i]);
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, DesktopHostScenePath);
            }
        }

        private static TMP_FontAsset RecreateJapaneseFontAsset()
        {
            if (File.Exists(FontAssetPath))
            {
                AssetDatabase.DeleteAsset(FontAssetPath);
                AssetDatabase.Refresh();
            }

            EnsureImportedFontFile();
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ImportedFontPath);
            if (sourceFont == null)
            {
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);

            fontAsset.name = "NotoSansJP_Dynamic";
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            SaveFontSubAssets(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        }

        private static void SaveFontSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            Texture2D[] atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures != null)
            {
                for (int i = 0; i < atlasTextures.Length; i++)
                {
                    if (atlasTextures[i] == null)
                    {
                        continue;
                    }

                    atlasTextures[i].name = fontAsset.name + " Atlas " + i;
                    AssetDatabase.AddObjectToAsset(atlasTextures[i], fontAsset);
                }
            }

            EditorUtility.SetDirty(fontAsset);
        }

        private static void EnsureImportedFontFile()
        {
            Directory.CreateDirectory(FontFolder);
            if (File.Exists(ImportedFontPath))
            {
                return;
            }

            string sourcePath = FindSystemJapaneseFont();
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogError("[Japanese TMP Font] No Japanese system font found.");
                return;
            }

            File.Copy(sourcePath, ImportedFontPath, false);
            AssetDatabase.ImportAsset(ImportedFontPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static string FindSystemJapaneseFont()
        {
            for (int i = 0; i < CandidateSystemFonts.Length; i++)
            {
                if (File.Exists(CandidateSystemFonts[i]))
                {
                    return CandidateSystemFonts[i];
                }
            }

            return string.Empty;
        }

        private static void AssignFontToDesktopHostBootText(TMP_FontAsset fontAsset)
        {
            if (!File.Exists(DesktopHostScenePath))
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(DesktopHostScenePath, OpenSceneMode.Single);
            TextMeshProUGUI[] textComponents = FindComponentsInScene<TextMeshProUGUI>(scene);
            for (int i = 0; i < textComponents.Length; i++)
            {
                if (textComponents[i] == null || textComponents[i].name != "BootLogText")
                {
                    continue;
                }

                textComponents[i].font = fontAsset;
                EditorUtility.SetDirty(textComponents[i]);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DesktopHostScenePath);
            AssetDatabase.SaveAssets();
        }

        private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            System.Collections.Generic.List<T> components = new System.Collections.Generic.List<T>();
            for (int i = 0; i < roots.Length; i++)
            {
                components.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return components.ToArray();
        }
    }
}
