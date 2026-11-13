using System.IO;
using BrainJump;
using UnityEditor;
using UnityEngine;

namespace BrainJump.Editor
{
    public static partial class SceneBuilder
    {
        internal static void BuildPlayerCameraAndDecor()
        {
            Material matOffWhite = AssetDatabase.LoadAssetAtPath<Material>("Assets/BrainJump/Materials/Mat_OffWhite.mat");
            Material matDark = AssetDatabase.LoadAssetAtPath<Material>("Assets/BrainJump/Materials/Mat_Dark.mat");
            Material matGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/BrainJump/Materials/Mat_Gold.mat");
            Material matCoral = AssetDatabase.LoadAssetAtPath<Material>("Assets/BrainJump/Materials/Mat_Coral.mat");
            Material matTrunk = AssetDatabase.LoadAssetAtPath<Material>("Assets/BrainJump/Materials/Mat_Trunk.mat");
            Material matGrass = AssetDatabase.LoadAssetAtPath<Material>("Assets/BrainJump/Materials/Mat_Grass.mat");
            Material matCrystal = AssetDatabase.LoadAssetAtPath<Material>("Assets/BrainJump/Materials/Mat_Crystal.mat");
            Material matCloud = AssetDatabase.LoadAssetAtPath<Material>("Assets/BrainJump/Materials/Mat_Cloud.mat");
            Material matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/BrainJump/Materials/Mat_Rock.mat");

            Vector3 spawnPosition = new Vector3(0f, 0.1f, -2.5f);

            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = spawnPosition;
            player.transform.rotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;

            Rigidbody rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 0f;
            rigidbody.angularDamping = 0.05f;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            CapsuleCollider capsuleCollider = player.AddComponent<CapsuleCollider>();
            capsuleCollider.height = 1.8f;
            capsuleCollider.radius = 0.45f;
            capsuleCollider.center = new Vector3(0f, 0.9f, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            GameObject body = MakePrimitive(
                PrimitiveType.Capsule,
                "Body",
                visual.transform,
                new Vector3(0f, 0.95f, 0f),
                new Vector3(0.9f, 1.0f, 0.9f),
                matOffWhite,
                true);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localRotation = Quaternion.identity;

            GameObject head = MakePrimitive(
                PrimitiveType.Sphere,
                "Head",
                visual.transform,
                new Vector3(0f, 1.75f, 0f),
                new Vector3(1.1f, 1.0f, 1.0f),
                matOffWhite,
                true);
            head.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            head.transform.localRotation = Quaternion.identity;

            GameObject eyeL = MakePrimitive(
                PrimitiveType.Sphere,
                "EyeL",
                visual.transform,
                new Vector3(-0.2f, 1.8f, 0.48f),
                new Vector3(0.18f, 0.22f, 0.1f),
                matDark,
                true);
            eyeL.transform.localPosition = new Vector3(-0.2f, 1.8f, 0.48f);
            eyeL.transform.localRotation = Quaternion.identity;

            GameObject eyeR = MakePrimitive(
                PrimitiveType.Sphere,
                "EyeR",
                visual.transform,
                new Vector3(0.2f, 1.8f, 0.48f),
                new Vector3(0.18f, 0.22f, 0.1f),
                matDark,
                true);
            eyeR.transform.localPosition = new Vector3(0.2f, 1.8f, 0.48f);
            eyeR.transform.localRotation = Quaternion.identity;

            GameObject armL = MakePrimitive(
                PrimitiveType.Capsule,
                "ArmL",
                visual.transform,
                new Vector3(-0.55f, 1.0f, 0f),
                new Vector3(0.24f, 0.5f, 0.24f),
                matGold,
                true);
            armL.transform.localPosition = new Vector3(-0.55f, 1.0f, 0f);
            armL.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);

            GameObject armR = MakePrimitive(
                PrimitiveType.Capsule,
                "ArmR",
                visual.transform,
                new Vector3(0.55f, 1.0f, 0f),
                new Vector3(0.24f, 0.5f, 0.24f),
                matGold,
                true);
            armR.transform.localPosition = new Vector3(0.55f, 1.0f, 0f);
            armR.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);

            GameObject legL = MakePrimitive(
                PrimitiveType.Capsule,
                "LegL",
                visual.transform,
                new Vector3(-0.22f, 0.25f, 0f),
                new Vector3(0.28f, 0.4f, 0.28f),
                matCoral,
                true);
            legL.transform.localPosition = new Vector3(-0.22f, 0.25f, 0f);
            legL.transform.localRotation = Quaternion.identity;

            GameObject legR = MakePrimitive(
                PrimitiveType.Capsule,
                "LegR",
                visual.transform,
                new Vector3(0.22f, 0.25f, 0f),
                new Vector3(0.28f, 0.4f, 0.28f),
                matCoral,
                true);
            legR.transform.localPosition = new Vector3(0.22f, 0.25f, 0f);
            legR.transform.localRotation = Quaternion.identity;

            player.AddComponent<PlayerController>();
            player.AddComponent<CharacterAnimator>();

            BuiltPlayer = player;

            GameObject playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.position = spawnPosition;
            playerSpawn.transform.rotation = Quaternion.identity;
            BuiltSpawn = playerSpawn.transform;

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            Vector3 cameraPosition = new Vector3(0f, 8.5f, -8f);
            Vector3 cameraLookTarget = new Vector3(0f, 1f, 4f);

            camera.transform.position = cameraPosition;
            camera.transform.LookAt(cameraLookTarget, Vector3.up);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ColorFromHex("#A8D8EA");
            camera.farClipPlane = 100f;

            CameraFollow cameraFollow = camera.GetComponent<CameraFollow>();
            if (cameraFollow == null)
            {
                cameraFollow = camera.gameObject.AddComponent<CameraFollow>();
            }

            cameraFollow.Target = player.transform;
            cameraFollow.SnapToTarget();
            BuiltCameraFollow = cameraFollow;

            GameObject decorRoot = new GameObject("Decor");
            decorRoot.transform.position = Vector3.zero;
            decorRoot.transform.rotation = Quaternion.identity;

            Vector3[] treePositions =
            {
                new Vector3(-3.5f, 0f, -3.5f),
                new Vector3(3.8f, 0f, -2.5f),
                new Vector3(-4.0f, 0f, 1.5f)
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                GameObject tree = new GameObject("Tree_" + (i + 1));
                tree.transform.SetParent(decorRoot.transform, false);
                tree.transform.localPosition = treePositions[i];
                tree.transform.localRotation = Quaternion.identity;

                MakePrimitive(
                    PrimitiveType.Cylinder,
                    "Trunk",
                    tree.transform,
                    new Vector3(0f, 0.6f, 0f),
                    new Vector3(0.3f, 0.6f, 0.3f),
                    matTrunk,
                    true);

                MakePrimitive(
                    PrimitiveType.Sphere,
                    "Foliage_Lower",
                    tree.transform,
                    new Vector3(0.3f, 1.6f, 0f),
                    new Vector3(1.8f, 1.8f, 1.8f),
                    matGrass,
                    true);

                MakePrimitive(
                    PrimitiveType.Sphere,
                    "Foliage_Upper",
                    tree.transform,
                    new Vector3(0.3f, 2.3f, 0f),
                    new Vector3(1.3f, 1.3f, 1.3f),
                    matGrass,
                    true);
            }

            float[] flowerAngles = { 20f, 70f, 110f, 160f, 200f, 250f, 290f, 340f };
            Material[] flowerMaterials =
            {
                matCoral,
                matGold,
                matOffWhite,
                matCoral,
                matGold,
                matOffWhite,
                matCoral,
                matGold
            };

            for (int i = 0; i < flowerAngles.Length; i++)
            {
                float angleRadians = flowerAngles[i] * Mathf.Deg2Rad;
                Vector3 flowerPosition = new Vector3(
                    Mathf.Cos(angleRadians) * 4.2f,
                    0.12f,
                    Mathf.Sin(angleRadians) * 4.2f);

                MakePrimitive(
                    PrimitiveType.Sphere,
                    "Flower_" + (i + 1),
                    decorRoot.transform,
                    flowerPosition,
                    Vector3.one * 0.24f,
                    flowerMaterials[i],
                    true);
            }

            Vector3[] crystalPositions =
            {
                new Vector3(-4.2f, 0.5f, -1f),
                new Vector3(4.2f, 0.5f, 0.5f),
                new Vector3(0f, 0.5f, -4.3f)
            };

            Quaternion[] crystalRotations =
            {
                Quaternion.Euler(0f, 20f, 12f),
                Quaternion.Euler(8f, 55f, -10f),
                Quaternion.Euler(-6f, 85f, 14f)
            };

            for (int i = 0; i < crystalPositions.Length; i++)
            {
                GameObject crystal = MakePrimitive(
                    PrimitiveType.Sphere,
                    "Crystal_" + (i + 1),
                    decorRoot.transform,
                    crystalPositions[i],
                    new Vector3(0.3f, 0.9f, 0.3f),
                    matCrystal,
                    true);

                crystal.transform.localPosition = crystalPositions[i];
                crystal.transform.localRotation = crystalRotations[i];
            }

            float[] cloudAngles = { 15f, 75f, 135f, 195f, 255f, 315f };
            float[] cloudRadii = { 9f, 11f, 13f, 15f, 10f, 14f };
            float[] cloudHeights = { 2.2f, 3.1f, 4.2f, 2.8f, 4.7f, 3.6f };

            for (int i = 0; i < cloudAngles.Length; i++)
            {
                float angleRadians = cloudAngles[i] * Mathf.Deg2Rad;
                Vector3 cloudPosition = new Vector3(
                    Mathf.Cos(angleRadians) * cloudRadii[i],
                    cloudHeights[i],
                    Mathf.Sin(angleRadians) * cloudRadii[i]);

                GameObject cloud = MakePrimitive(
                    PrimitiveType.Sphere,
                    "Cloud_" + (i + 1),
                    decorRoot.transform,
                    cloudPosition,
                    new Vector3(2.5f, 0.8f, 1.5f),
                    matCloud,
                    true);

                cloud.transform.localPosition = cloudPosition;
                cloud.transform.localRotation = Quaternion.Euler(0f, cloudAngles[i], 0f);
            }

            Vector3[] distantIslandPositions =
            {
                new Vector3(-14f, -3f, 12f),
                new Vector3(12f, -5f, 18f)
            };

            for (int i = 0; i < distantIslandPositions.Length; i++)
            {
                GameObject distantIsland = new GameObject("DistantIsland_" + (i + 1));
                distantIsland.transform.SetParent(decorRoot.transform, false);
                distantIsland.transform.localPosition = distantIslandPositions[i];
                distantIsland.transform.localRotation = Quaternion.identity;

                MakePrimitive(
                    PrimitiveType.Cylinder,
                    "GrassDisc",
                    distantIsland.transform,
                    Vector3.zero,
                    new Vector3(5f, 0.15f, 5f),
                    matGrass,
                    true);

                MakePrimitive(
                    PrimitiveType.Sphere,
                    "Rock",
                    distantIsland.transform,
                    new Vector3(0f, -1f, 0f),
                    new Vector3(3.8f, 2.2f, 3.8f),
                    matRock,
                    true);
            }

            string playerPrefabFolder = "Assets/BrainJump/Prefabs/Player";
            string platformPrefabFolder = "Assets/BrainJump/Prefabs/Platforms";

            Directory.CreateDirectory(playerPrefabFolder);
            Directory.CreateDirectory(platformPrefabFolder);

            string playerPrefabPath = playerPrefabFolder + "/Player.prefab";
            string platformPrefabPath = platformPrefabFolder + "/AnswerPlatform.prefab";

            PrefabUtility.SaveAsPrefabAsset(player, playerPrefabPath);

            if (BuiltPlatforms != null && BuiltPlatforms.Count > 1 && BuiltPlatforms[1] != null)
            {
                PrefabUtility.SaveAsPrefabAsset(BuiltPlatforms[1].gameObject, platformPrefabPath);
            }
        }
    }
}
