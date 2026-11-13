using System.Collections.Generic;
using BrainJump;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrainJump.Editor
{
    public static partial class SceneBuilder
    {
        internal static List<AnswerPlatform> BuiltPlatforms;
        internal static GameObject BuiltPlayer;
        internal static Transform BuiltSpawn;
        internal static CameraFollow BuiltCameraFollow;

        [MenuItem("BrainJump/Build Prototype Scene")]
        public static void BuildAll()
        {
            BuildEnvironment();
            BuildPlayerCameraAndDecor();
            BuildUIAndManagers();
            AssetDatabase.SaveAssets();
            Debug.Log("BrainJump: prototype scene built. Open MainScene and press Play.");
        }

        public static void BuildEnvironment()
        {
            if (GraphicsSettings.currentRenderPipeline == null || Shader.Find("Universal Render Pipeline/Lit") == null)
            {
                Debug.LogError("BrainJump: URP is not the active render pipeline. Create the project from the Unity Hub '3D (URP)' template in Unity 6.3 LTS, or assign a Universal Render Pipeline Asset in Project Settings > Graphics, then run BrainJump > Build Prototype Scene again.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(scene);

            Material matGrass = CreateMaterial("Mat_Grass", ColorFromHex("#4C9670"));
            Material matRock = CreateMaterial("Mat_Rock", ColorFromHex("#33475E"));
            Material matGold = CreateMaterial("Mat_Gold", ColorFromHex("#F2B85B"));
            Material matCoral = CreateMaterial("Mat_Coral", ColorFromHex("#E98270"));
            Material matSky = CreateMaterial("Mat_Sky", ColorFromHex("#75B9D4"));
            Material matOffWhite = CreateMaterial("Mat_OffWhite", ColorFromHex("#F7F4EC"));
            CreateMaterial("Mat_Dark", ColorFromHex("#172B42"));
            CreateMaterial("Mat_Cloud", Color.white);
            CreateMaterial("Mat_Trunk", ColorFromHex("#7A5230"));

            Material matCrystal = CreateMaterial("Mat_Crystal", ColorFromHex("#75B9D4"));
            if (matCrystal != null)
            {
                matCrystal.EnableKeyword("_EMISSION");
                matCrystal.SetColor("_EmissionColor", ColorFromHex("#75B9D4") * 0.6f);
            }

            Light directionalLight = null;
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }

            if (directionalLight == null)
            {
                GameObject lightGO = new GameObject("Directional Light");
                directionalLight = lightGO.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }

            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            directionalLight.color = ColorFromHex("#FFF2DD");
            directionalLight.intensity = 1.1f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ColorFromHex("#75B9D4");
            RenderSettings.ambientEquatorColor = ColorFromHex("#F7F4EC");
            RenderSettings.ambientGroundColor = ColorFromHex("#172B42");
            RenderSettings.ambientIntensity = 0.6f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = ColorFromHex("#BFE3EE");
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.008f;

            GameObject island = new GameObject("Island");
            island.transform.position = Vector3.zero;

            GameObject grassTop = MakePrimitive(
                PrimitiveType.Cylinder,
                "GrassTop",
                island.transform,
                new Vector3(0f, -0.075f, 0f),
                new Vector3(5f, 0.075f, 5f),
                matGrass,
                true);

            BoxCollider grassCollider = grassTop.AddComponent<BoxCollider>();
            grassCollider.size = new Vector3(10f, 0.15f, 10f);

            MakePrimitive(
                PrimitiveType.Sphere,
                "RockBody",
                island.transform,
                new Vector3(0f, -2.2f, 0f),
                new Vector3(4.8f, 2.4f, 4.8f),
                matRock,
                false);

            MakePrimitive(
                PrimitiveType.Sphere,
                "RockTip",
                island.transform,
                new Vector3(0f, -3.8f, 0f),
                new Vector3(2.0f, 1.8f, 2.0f),
                matRock,
                false);

            GameObject platformsRoot = new GameObject("Platforms");
            BuiltPlatforms = new List<AnswerPlatform>();

            string[] platformNames = { "Platform_Left", "Platform_Center", "Platform_Right" };
            Vector3[] platformPositions =
            {
                new Vector3(-3.4f, -0.4f, 7.5f),
                new Vector3(0f, -0.4f, 8.5f),
                new Vector3(3.4f, -0.4f, 7.5f)
            };
            Color[] platformColors =
            {
                ColorFromHex("#F2B85B"),
                ColorFromHex("#E98270"),
                ColorFromHex("#75B9D4")
            };
            Material[] platformBaseMaterials = { matGold, matCoral, matSky };

            for (int i = 0; i < platformNames.Length; i++)
            {
                GameObject platformGO = MakePrimitive(
                    PrimitiveType.Cylinder,
                    platformNames[i],
                    platformsRoot.transform,
                    platformPositions[i],
                    new Vector3(1.5f, 0.4f, 1.5f),
                    platformBaseMaterials[i],
                    true);

                BoxCollider platformCollider = platformGO.AddComponent<BoxCollider>();
                platformCollider.size = new Vector3(3f, 0.8f, 3f);

                Renderer platformRenderer = platformGO.GetComponent<Renderer>();
                if (platformRenderer != null)
                {
                    platformRenderer.material.SetColor("_BaseColor", platformColors[i]);
                }

                GameObject labelGO = new GameObject("AnswerLabel");
                labelGO.transform.SetParent(platformGO.transform, false);
                labelGO.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                labelGO.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                TextMeshPro label = labelGO.AddComponent<TextMeshPro>();
                label.text = "?";
                label.fontSize = 96f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = ColorFromHex("#172B42");
                ((RectTransform)labelGO.transform).sizeDelta = new Vector2(4f, 2f);

                MakePrimitive(
                    PrimitiveType.Cylinder,
                    "TopDisc",
                    platformGO.transform,
                    new Vector3(0f, 0.395f, 0f),
                    new Vector3(1.3f, 0.025f, 1.3f),
                    matOffWhite,
                    true);

                AnswerPlatform platform = platformGO.AddComponent<AnswerPlatform>();
                SetIdleColor(platform, platformColors[i]);
                BuiltPlatforms.Add(platform);
            }
        }

        internal static Material CreateMaterial(string name, Color color, float smoothness = 0.35f)
        {
            EnsureFolder("Assets/BrainJump/Materials");
            string path = "Assets/BrainJump/Materials/" + name + ".mat";

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("BrainJump: URP Lit shader was not found while creating material " + name + ".");
                return null;
            }

            Material material = new Material(shader);
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        internal static Color ColorFromHex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }

            Debug.LogWarning("BrainJump: invalid color hex '" + hex + "'. Falling back to white.");
            return Color.white;
        }

        internal static GameObject MakePrimitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 pos,
            Vector3 scale,
            Material mat,
            bool removeCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;

            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = pos;
            go.transform.localScale = scale;

            if (mat != null)
            {
                Renderer renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = mat;
                }
            }

            if (removeCollider)
            {
                Collider collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }
            }

            return go;
        }

        internal static void SetIdleColor(AnswerPlatform platform, Color color)
        {
            if (platform == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(platform);
            SerializedProperty idleColorProperty = serializedObject.FindProperty("idleColor");
            if (idleColorProperty != null)
            {
                idleColorProperty.colorValue = color;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
