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

        /// <summary>
        /// Builds the floating island environment: materials, lighting, fog, island body,
        /// vines, waterfalls, sun, answer platforms, clouds and distant islands.
        /// </summary>
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
            Material matCloud = CreateMaterial("Mat_Cloud", Color.white);
            Material matTrunk = CreateMaterial("Mat_Trunk", ColorFromHex("#7A5230"));

            Material matCrystal = CreateMaterial("Mat_Crystal", ColorFromHex("#75B9D4"));
            if (matCrystal != null)
            {
                matCrystal.EnableKeyword("_EMISSION");
                matCrystal.SetColor("_EmissionColor", ColorFromHex("#75B9D4") * 0.6f);
            }

            Material matWater = CreateMaterial("Mat_Water", ColorFromHex("#5FB8D9"));
            if (matWater != null)
            {
                matWater.SetFloat("_Surface", 1f);
            }

            Material matSun = CreateMaterial("Mat_Sun", ColorFromHex("#FFF3B0"));
            if (matSun != null)
            {
                matSun.EnableKeyword("_EMISSION");
                matSun.SetColor("_EmissionColor", ColorFromHex("#FFF3B0") * 0.9f);
            }

            CreateMaterial("Mat_Particle", Color.white);

            Material matGlowGold = CreateMaterial("Mat_GlowGold", ColorFromHex("#F2B85B"));
            if (matGlowGold != null)
            {
                matGlowGold.EnableKeyword("_EMISSION");
                matGlowGold.SetColor("_EmissionColor", ColorFromHex("#F2B85B") * 0.5f);
            }

            Material matGlowCoral = CreateMaterial("Mat_GlowCoral", ColorFromHex("#E98270"));
            if (matGlowCoral != null)
            {
                matGlowCoral.EnableKeyword("_EMISSION");
                matGlowCoral.SetColor("_EmissionColor", ColorFromHex("#E98270") * 0.5f);
            }

            Material matGlowSky = CreateMaterial("Mat_GlowSky", ColorFromHex("#75B9D4"));
            if (matGlowSky != null)
            {
                matGlowSky.EnableKeyword("_EMISSION");
                matGlowSky.SetColor("_EmissionColor", ColorFromHex("#75B9D4") * 0.5f);
            }

            Material matVine = CreateMaterial("Mat_Vine", ColorFromHex("#2E6B4A"));

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

            // Layered rock underside.
            MakePrimitive(
                PrimitiveType.Sphere,
                "RockBody",
                island.transform,
                new Vector3(0f, -2.2f, 0f),
                new Vector3(4.8f, 2.4f, 4.8f),
                matRock,
                false);

            MakePrimitive(
                PrimitiveType.Cylinder,
                "RockMid",
                island.transform,
                new Vector3(0f, -3.1f, 0f),
                new Vector3(3.4f, 1.0f, 3.4f),
                matRock,
                false);

            MakePrimitive(
                PrimitiveType.Cylinder,
                "RockLow",
                island.transform,
                new Vector3(0f, -4.0f, 0f),
                new Vector3(2.0f, 0.9f, 2.0f),
                matRock,
                false);

            MakePrimitive(
                PrimitiveType.Sphere,
                "RockTip",
                island.transform,
                new Vector3(0f, -4.6f, 0f),
                new Vector3(1.4f, 1.4f, 1.4f),
                matRock,
                false);

            // Hanging vines around the island rim.
            float[] vineAngles = { 30f, 90f, 150f, 220f, 300f };
            const float vineRadius = 4.4f;
            for (int v = 0; v < vineAngles.Length; v++)
            {
                float rad = vineAngles[v] * Mathf.Deg2Rad;
                float vx = Mathf.Cos(rad) * vineRadius;
                float vz = Mathf.Sin(rad) * vineRadius;

                MakePrimitive(
                    PrimitiveType.Cylinder,
                    "Vine_" + v,
                    island.transform,
                    new Vector3(vx, -0.75f, vz),
                    new Vector3(0.08f, 1.1f, 0.08f),
                    matVine,
                    true);

                MakePrimitive(
                    PrimitiveType.Sphere,
                    "VineLeaf_" + v,
                    island.transform,
                    new Vector3(vx, -1.35f, vz),
                    new Vector3(0.3f, 0.3f, 0.3f),
                    matGrass,
                    true);
            }

            // Waterfalls with splash puffs.
            Vector3[] waterfallPositions =
            {
                new Vector3(3.6f, -1.4f, 2.6f),
                new Vector3(-3.2f, -1.4f, -3.0f)
            };
            Vector3[] splashPositions =
            {
                new Vector3(3.6f, -2.7f, 2.6f),
                new Vector3(-3.2f, -2.7f, -3.0f)
            };
            for (int w = 0; w < waterfallPositions.Length; w++)
            {
                MakePrimitive(
                    PrimitiveType.Cube,
                    "Waterfall_" + w,
                    island.transform,
                    waterfallPositions[w],
                    new Vector3(0.5f, 2.6f, 0.12f),
                    matWater,
                    true);

                if (w < splashPositions.Length)
                {
                    MakePrimitive(
                        PrimitiveType.Sphere,
                        "WaterfallSplash_" + w,
                        island.transform,
                        splashPositions[w],
                        new Vector3(0.7f, 0.25f, 0.7f),
                        matCloud,
                        true);
                }
            }

            // Sun at scene root.
            MakePrimitive(
                PrimitiveType.Sphere,
                "Sun",
                null,
                new Vector3(-18f, 14f, 25f),
                new Vector3(3f, 3f, 3f),
                matSun,
                true);

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
            Material[] glowMaterials = { matGlowGold, matGlowCoral, matGlowSky };

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

                MakePrimitive(
                    PrimitiveType.Cylinder,
                    "Rim",
                    platformGO.transform,
                    new Vector3(0f, 0.28f, 0f),
                    new Vector3(1.7f, 0.12f, 1.7f),
                    matRock,
                    true);

                Material glowMaterial = (i >= 0 && i < glowMaterials.Length) ? glowMaterials[i] : null;
                MakePrimitive(
                    PrimitiveType.Cylinder,
                    "GlowRing",
                    platformGO.transform,
                    new Vector3(0f, 0.42f, 0f),
                    new Vector3(1.55f, 0.04f, 1.55f),
                    glowMaterial,
                    true);

                GameObject pulseRingGO = MakePrimitive(
                    PrimitiveType.Cylinder,
                    "PulseRing",
                    platformGO.transform,
                    new Vector3(0f, 0.44f, 0f),
                    new Vector3(2.0f, 0.05f, 2.0f),
                    matGold,
                    true);
                Transform pulseRingTransform = pulseRingGO != null ? pulseRingGO.transform : null;

                AnswerPlatform platform = platformGO.AddComponent<AnswerPlatform>();
                SetIdleColor(platform, platformColors[i]);
                if (platform != null && pulseRingTransform != null)
                {
                    platform.WirePulseRing(pulseRingTransform);
                }
                BuiltPlatforms.Add(platform);
            }

            // Puffy clouds (main body + two side puffs each).
            GameObject cloudsRoot = new GameObject("Clouds");
            float[] cloudAngles = { 20f, 110f, 200f, 290f };
            float[] cloudRadii = { 14f, 16f, 13f, 18f };
            float[] cloudHeights = { 6f, 8f, 5f, 9f };
            int cloudCount = Mathf.Min(cloudAngles.Length, Mathf.Min(cloudRadii.Length, cloudHeights.Length));
            for (int i = 0; i < cloudCount; i++)
            {
                float rad = cloudAngles[i] * Mathf.Deg2Rad;
                Vector3 cloudPosition = new Vector3(
                    Mathf.Cos(rad) * cloudRadii[i],
                    cloudHeights[i],
                    Mathf.Sin(rad) * cloudRadii[i]);

                GameObject cloud = new GameObject("Cloud_" + i);
                cloud.transform.SetParent(cloudsRoot.transform, false);
                cloud.transform.localPosition = cloudPosition;
                cloud.transform.localRotation = Quaternion.Euler(0f, cloudAngles[i], 0f);

                MakePrimitive(
                    PrimitiveType.Sphere,
                    "CloudMain",
                    cloud.transform,
                    new Vector3(0f, 0f, 0f),
                    new Vector3(2.5f, 0.8f, 1.5f),
                    matCloud,
                    true);

                MakePrimitive(
                    PrimitiveType.Sphere,
                    "CloudPuffLeft",
                    cloud.transform,
                    new Vector3(-1.1f, 0.15f, 0.2f),
                    new Vector3(1.4f, 0.6f, 1.0f),
                    matCloud,
                    true);

                MakePrimitive(
                    PrimitiveType.Sphere,
                    "CloudPuffRight",
                    cloud.transform,
                    new Vector3(1.1f, 0.1f, -0.1f),
                    new Vector3(1.5f, 0.65f, 1.1f),
                    matCloud,
                    true);
            }

            // Distant islands.
            GameObject distantRoot = new GameObject("DistantIslands");
            Vector3[] distantIslandPositions =
            {
                new Vector3(-14f, -3f, 18f),
                new Vector3(15f, -5f, 16f),
                new Vector3(-20f, -6f, 22f),
                new Vector3(18f, -4f, 26f)
            };
            for (int i = 0; i < distantIslandPositions.Length; i++)
            {
                GameObject distant = new GameObject("DistantIsland_" + i);
                distant.transform.SetParent(distantRoot.transform, false);
                distant.transform.localPosition = distantIslandPositions[i];

                MakePrimitive(
                    PrimitiveType.Cylinder,
                    "DistantGrass",
                    distant.transform,
                    new Vector3(0f, 0f, 0f),
                    new Vector3(2f, 0.08f, 2f),
                    matGrass,
                    true);

                MakePrimitive(
                    PrimitiveType.Sphere,
                    "DistantRock",
                    distant.transform,
                    new Vector3(0f, -0.9f, 0f),
                    new Vector3(1.9f, 1.6f, 1.9f),
                    matRock,
                    true);

                if (i == 0)
                {
                    MakePrimitive(
                        PrimitiveType.Cylinder,
                        "TreeTrunk",
                        distant.transform,
                        new Vector3(0f, 0.5f, 0f),
                        new Vector3(0.25f, 0.5f, 0.25f),
                        matTrunk,
                        true);

                    MakePrimitive(
                        PrimitiveType.Sphere,
                        "TreeFoliage",
                        distant.transform,
                        new Vector3(0f, 1.2f, 0f),
                        new Vector3(1.4f, 1.4f, 1.4f),
                        matGrass,
                        true);
                }
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
