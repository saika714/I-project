using System.IO;
using System.Linq;
using SoftballConnect.Content;
using SoftballConnect.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace SoftballConnect.Editor
{
    public static class ProjectSetup
    {
        const string Root = "Assets/_SoftballConnect/";
        [MenuItem("Softball Connect/Create Missing MVP Assets")]
        public static void CreateMissingAssets()
        {
            if (!UnityEngine.Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            foreach (string folder in new[] { "Config", "Art/Materials", "Scenes" })
                Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
            var theme = AssetDatabase.LoadAssetAtPath<VisualThemeConfig>(Root + "Config/DefaultVisualTheme.asset");
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<VisualThemeConfig>();
                // Runtime presentation builds its own models from the theme palette.
                AssetDatabase.CreateAsset(theme, Root + "Config/DefaultVisualTheme.asset");
            }
            if (theme.SurfaceMaterial == null)
                theme.SurfaceMaterial = Material("ArcadeSurface", "Universal Render Pipeline/Simple Lit");
            if (theme.TrailMaterial == null)
                theme.TrailMaterial = Material("ArcadeTrail", "Universal Render Pipeline/Particles/Unlit");
            if (theme.SkyMaterial == null)
            {
                theme.SkyMaterial = Material("StadiumSky", "Skybox/Procedural");
                theme.SkyMaterial.SetFloat("_AtmosphereThickness", 0.8f);
                theme.SkyMaterial.SetFloat("_Exposure", 1.1f);
                theme.SkyMaterial.SetColor("_SkyTint", new Color(0.5f, 0.53f, 0.56f));
                EditorUtility.SetDirty(theme.SkyMaterial);
            }
            theme.TrailMaterial.SetFloat("_Surface", 1);
            theme.TrailMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            theme.TrailMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            theme.TrailMaterial.SetFloat("_ZWrite", 0);
            theme.TrailMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            theme.TrailMaterial.SetOverrideTag("RenderType", "Transparent");
            theme.TrailMaterial.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(theme.TrailMaterial);
            EditorUtility.SetDirty(theme);
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "Config/DefaultGameConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>(); config.Visuals = theme;
                config.DesktopActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "Input/SoftballDesktop.inputactions");
                config.Localization = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "Localization/ui.json");
                AssetDatabase.CreateAsset(config, Root + "Config/DefaultGameConfig.asset");
            }
            string[] names = { "Bootstrap", "MainMenu", "Stadium_PC" };
            for (int i = 0; i < names.Length; i++)
            {
                string path = Root + "Scenes/" + names[i] + ".unity";
                if (File.Exists(path)) continue;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var bootstrap = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
                bootstrap.Role = (SceneRole)i; bootstrap.Config = config;
                EditorSceneManager.SaveScene(scene, path);
            }
            var required = names.Select(n => Root + "Scenes/" + n + ".unity").ToArray();
            EditorBuildSettings.scenes = required.Select(p => new EditorBuildSettingsScene(p, true))
                .Concat(EditorBuildSettings.scenes.Where(s => !required.Contains(s.path)).Select(s =>
                    new EditorBuildSettingsScene(s.path, s.enabled))).ToArray();
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previousScene)) EditorSceneManager.OpenScene(previousScene);
            Debug.Log("Softball Connect MVP assets ready. Open Bootstrap and press Play.");
        }
        static Material Material(string name, string shader)
        {
            string path = Root + "Art/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(shader)) { name = name };
            AssetDatabase.CreateAsset(material, path); return material;
        }
    }
}
