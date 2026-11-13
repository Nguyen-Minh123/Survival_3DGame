#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupLab2Scene
{
    private const string ProjectRoot = "Assets";
    private const string PrefabsFolder = ProjectRoot + "/Prefabs";
    private const string MaterialsFolder = ProjectRoot + "/Materials";
    private const string ScenesFolder = ProjectRoot + "/Scenes";

    // Menu item đã được tinh gọn vào SetupLabs4To8 -> "Nộp Bài Từng Lab"
    public static void RunFullSetupLab2()
    {
        Debug.Log(">>> [Lab 2] BẮT ĐẦU THIẾT LẬP LỰA CHỌN & NHẶT ĐỒ VẬT (MSSV: 2312690) <<<");

        string scenePath = ScenesFolder + "/SampleScene.unity";
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        // Lấy hoặc tạo Materials
        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Mat_Wood.mat");
        Material rockMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Mat_Rock.mat");
        Material mushroomStemMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Mat_MushroomStem.mat");
        Material mushroomCapMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Mat_MushroomCap.mat");

        Material berryMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Mat_Berry.mat");
        if (berryMat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            berryMat = new Material(shader) { color = new Color(0.85f, 0.10f, 0.35f) };
            AssetDatabase.CreateAsset(berryMat, MaterialsFolder + "/Mat_Berry.mat");
        }

        // Tạo các Prefab vật phẩm có thể nhặt (Pickups)
        GameObject stickPrefab = CreateStickPickupPrefab(woodMat);
        GameObject rockPrefab = CreateRockPickupPrefab(rockMat);
        GameObject mushroomPrefab = CreateMushroomPickupPrefab(mushroomStemMat, mushroomCapMat);
        GameObject berryPrefab = CreateBerryPickupPrefab(berryMat);

        // Thiết lập SelectionManager & SimpleInventory trên Player
        SetupPlayerComponents();

        // Rải các vật phẩm nhặt được trên Terrain
        Terrain terrain = Object.FindAnyObjectByType<Terrain>();
        if (terrain != null)
        {
            ScatterPickupItems(terrain, stickPrefab, rockPrefab, mushroomPrefab, berryPrefab);
        }

        // Lưu lại Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("🎉 [Lab 2] HOÀN TẤT THIẾT LẬP THÀNH CÔNG! Đã tích hợp SelectionManager, SimpleInventory và các vật phẩm nhặt được!");
    }

    // Menu item đã được tinh gọn vào SetupLabs4To8 -> "Nộp Bài Từng Lab"
    public static void BuildExecutableLab2()
    {
        RunFullSetupLab2();

        string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Build_Lab2"));
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

        string exePath = Path.Combine(buildDir, "2312690_3DSurvivalGame.exe");
        Debug.Log(">>> [Lab 2] Bắt đầu Build Standalone Windows EXE tại: " + exePath);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { ScenesFolder + "/SampleScene.unity" },
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("🎉 [Lab 2] BUILD THÀNH CÔNG! File thực thi tại: " + exePath);
        }
        else
        {
            Debug.LogError("❌ [Lab 2] BUILD THẤT BẠI: " + report.summary.result);
        }
    }

    private static void SetupPlayerComponents()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");

        if (player != null)
        {
            // Gắn SimpleInventory
            if (player.GetComponent<SimpleInventory>() == null)
            {
                player.AddComponent<SimpleInventory>();
            }

            // Gắn SelectionManager vào Camera
            Camera mainCam = player.GetComponentInChildren<Camera>();
            if (mainCam != null)
            {
                if (mainCam.GetComponent<SelectionManager>() == null)
                {
                    SelectionManager sm = mainCam.gameObject.AddComponent<SelectionManager>();
                    sm.maxDistance = 4.0f;
                    sm.interactionKey = KeyCode.E;
                }
            }
            else
            {
                if (player.GetComponent<SelectionManager>() == null)
                {
                    SelectionManager sm = player.AddComponent<SelectionManager>();
                    sm.maxDistance = 4.0f;
                    sm.interactionKey = KeyCode.E;
                }
            }
        }
    }

    private static GameObject CreateStickPickupPrefab(Material woodMat)
    {
        string path = PrefabsFolder + "/Pickup_Stick.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("Pickup_Stick");
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.transform.SetParent(root.transform);
        cyl.transform.localPosition = new Vector3(0, 0.05f, 0);
        cyl.transform.localRotation = Quaternion.Euler(0, 0, 85f); // nằm ngang dưới đất
        cyl.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
        cyl.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
        Object.DestroyImmediate(cyl.GetComponent<Collider>());

        BoxCollider col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.06f, 0);
        col.size = new Vector3(0.9f, 0.2f, 0.25f);

        InteractableObject io = root.AddComponent<InteractableObject>();
        io.ItemName = "Cành cây (Stick)";
        io.interactionDistance = 3.5f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateRockPickupPrefab(Material rockMat)
    {
        string path = PrefabsFolder + "/Pickup_Rock.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Pickup_Rock";
        root.transform.localScale = new Vector3(0.35f, 0.22f, 0.32f);
        root.GetComponent<MeshRenderer>().sharedMaterial = rockMat;

        SphereCollider col = root.GetComponent<SphereCollider>();
        col.radius = 0.5f;

        InteractableObject io = root.AddComponent<InteractableObject>();
        io.ItemName = "Hòn đá (Stone)";
        io.interactionDistance = 3.5f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateMushroomPickupPrefab(Material stemMat, Material capMat)
    {
        string path = PrefabsFolder + "/Pickup_Mushroom.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("Pickup_Mushroom");

        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "Stem";
        stem.transform.SetParent(root.transform);
        stem.transform.localPosition = new Vector3(0, 0.15f, 0);
        stem.transform.localScale = new Vector3(0.12f, 0.15f, 0.12f);
        stem.GetComponent<MeshRenderer>().sharedMaterial = stemMat;
        Object.DestroyImmediate(stem.GetComponent<Collider>());

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.name = "Cap";
        cap.transform.SetParent(root.transform);
        cap.transform.localPosition = new Vector3(0, 0.3f, 0);
        cap.transform.localScale = new Vector3(0.42f, 0.22f, 0.42f);
        cap.GetComponent<MeshRenderer>().sharedMaterial = capMat;
        Object.DestroyImmediate(cap.GetComponent<Collider>());

        SphereCollider col = root.AddComponent<SphereCollider>();
        col.center = new Vector3(0, 0.22f, 0);
        col.radius = 0.35f;

        InteractableObject io = root.AddComponent<InteractableObject>();
        io.ItemName = "Nấm rừng (Mushroom)";
        io.interactionDistance = 3.5f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateBerryPickupPrefab(Material berryMat)
    {
        string path = PrefabsFolder + "/Pickup_Berry.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Pickup_Berry";
        root.transform.localScale = new Vector3(0.20f, 0.20f, 0.20f);
        root.GetComponent<MeshRenderer>().sharedMaterial = berryMat;

        SphereCollider col = root.GetComponent<SphereCollider>();
        col.radius = 0.5f;

        InteractableObject io = root.AddComponent<InteractableObject>();
        io.ItemName = "Quả mọng (Berries)";
        io.interactionDistance = 3.5f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void ScatterPickupItems(Terrain terrain, GameObject stickPrefab, GameObject rockPrefab, GameObject mushroomPrefab, GameObject berryPrefab)
    {
        GameObject existingGroup = GameObject.Find("Pickups_In_World");
        if (existingGroup != null) Object.DestroyImmediate(existingGroup);

        GameObject pickupsGroup = new GameObject("Pickups_In_World");

        // 1. Đặt 6 vật phẩm ngay trước mặt Player spawn để người chơi vừa vào game là test được ngay!
        Vector3 spawnPlayerPos = new Vector3(142f, 0, 135f);
        Vector3[] immediateSpawnOffsets = new Vector3[]
        {
            new Vector3(1.5f, 0, 2.5f),   // Cành cây
            new Vector3(-1.2f, 0, 2.8f),  // Hòn đá
            new Vector3(0.2f, 0, 3.2f),   // Nấm rừng
            new Vector3(2.2f, 0, 1.8f),   // Quả mọng
            new Vector3(-2.0f, 0, 2.0f),  // Hòn đá 2
            new Vector3(1.0f, 0, 4.0f)    // Cành cây 2
        };

        GameObject[] immediatePrefabs = new GameObject[]
        {
            stickPrefab, rockPrefab, mushroomPrefab, berryPrefab, rockPrefab, stickPrefab
        };

        for (int i = 0; i < immediateSpawnOffsets.Length; i++)
        {
            Vector3 worldPos = spawnPlayerPos + immediateSpawnOffsets[i];
            worldPos.y = terrain.SampleHeight(worldPos) + 0.05f;

            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(immediatePrefabs[i]);
            item.transform.SetParent(pickupsGroup.transform);
            item.transform.position = worldPos;
        }

        // 2. Rải rác thêm 70 vật phẩm khắp khu vực thung lũng
        System.Random rnd = new System.Random(2026);
        GameObject[] allPrefabs = new GameObject[] { stickPrefab, rockPrefab, mushroomPrefab, berryPrefab };

        for (int i = 0; i < 70; i++)
        {
            float rx = (float)rnd.NextDouble() * 160f + 70f;
            float rz = (float)rnd.NextDouble() * 160f + 70f;
            float ry = terrain.SampleHeight(new Vector3(rx, 0, rz));

            if (ry > 6.8f && terrain.terrainData.GetSteepness(rx / 300f, rz / 300f) < 22f)
            {
                GameObject chosenPrefab = allPrefabs[rnd.Next(allPrefabs.Length)];
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab);
                item.transform.SetParent(pickupsGroup.transform);
                item.transform.position = new Vector3(rx, ry + 0.05f, rz);
                item.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
            }
        }
    }
}
#endif
