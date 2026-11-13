#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class SetupLab1Scene
{
    private const string ProjectRoot = "Assets";
    private const string ScenesFolder = ProjectRoot + "/Scenes";
    private const string MaterialsFolder = ProjectRoot + "/Materials";
    private const string TexturesFolder = ProjectRoot + "/Textures";
    private const string PrefabsFolder = ProjectRoot + "/Prefabs";
    private const string TerrainDataFolder = ProjectRoot + "/TerrainData";

    // [Legacy Lab 1 Menu Items removed to clean up Editor Menu]
    public static void RunFullSetup()
    {
        Debug.Log(">>> [Lab 1] BẮT ĐẦU THIẾT LẬP DỰ ÁN 3D SURVIVAL GAME (MSSV: 2312690) <<<");

        EnsureFolders();
        SetupLayers();

        // Tạo Textures & Vật liệu
        Texture2D grassTex = CreateNoiseTexture("GrassTex", new Color(0.25f, 0.55f, 0.20f), new Color(0.18f, 0.42f, 0.14f), 512, 0.08f);
        Texture2D rockTex = CreateNoiseTexture("RockTex", new Color(0.45f, 0.45f, 0.45f), new Color(0.25f, 0.25f, 0.25f), 512, 0.12f);
        Texture2D dirtTex = CreateNoiseTexture("DirtTex", new Color(0.55f, 0.40f, 0.25f), new Color(0.38f, 0.28f, 0.18f), 512, 0.10f);
        Texture2D grassBladeTex = CreateGrassBladeTexture("GrassBlade");

        // Tạo Materials
        Material woodMat = CreateMaterial("Mat_Wood", new Color(0.40f, 0.25f, 0.12f));
        Material leafMat = CreateMaterial("Mat_Leaves", new Color(0.15f, 0.50f, 0.15f));
        Material rockMat = CreateMaterial("Mat_Rock", new Color(0.40f, 0.42f, 0.44f));
        Material mushroomStemMat = CreateMaterial("Mat_MushroomStem", new Color(0.92f, 0.90f, 0.85f));
        Material mushroomCapMat = CreateMaterial("Mat_MushroomCap", new Color(0.85f, 0.15f, 0.15f));
        Material waterMat = CreateTransparentMaterial("Mat_Water", new Color(0.15f, 0.55f, 0.75f, 0.75f));

        // Tạo Prefabs
        GameObject treePrefab = CreateTreePrefab(woodMat, leafMat);
        GameObject rockPrefab = CreateRockPrefab(rockMat);
        GameObject mushroomPrefab = CreateMushroomPrefab(mushroomStemMat, mushroomCapMat);

        // Tạo Terrain Layers
        TerrainLayer layerGrass = CreateTerrainLayer("Layer_Grass", grassTex, 15f);
        TerrainLayer layerRock = CreateTerrainLayer("Layer_Rock", rockTex, 25f);
        TerrainLayer layerDirt = CreateTerrainLayer("Layer_Dirt", dirtTex, 12f);

        // Tạo Scene & Terrain
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // Setup Lighting
        SetupLighting();

        // Setup Terrain Data & Sculpt
        Terrain terrain = BuildTerrain(layerGrass, layerRock, layerDirt, treePrefab, grassBladeTex);

        // Setup Environment Props (Đá, Nấm, Nước rải rác)
        ScatterProps(terrain, rockPrefab, mushroomPrefab, waterMat);

        // Setup FPP Player (CharacterController + PlayerMovement + MouseMovement)
        GameObject playerObj = SetupFPPPlayer(terrain);

        // Lưu Scene
        string scenePath = ScenesFolder + "/SampleScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log(">>> Đã lưu Scene tại: " + scenePath);

        // Cấu hình Build Settings
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };

        PlayerSettings.companyName = "ITFacDLU";
        PlayerSettings.productName = "2312690_3DSurvivalGame";

        Selection.activeGameObject = playerObj;
        Debug.Log("🎉 [Lab 1] HOÀN TẤT THIẾT LẬP THÀNH CÔNG! Địa hình Terrain, Cây, Cỏ, Nấm, Đá và Nhân vật FPP đã sẵn sàng!");
    }

    public static void BuildExecutable()
    {
        RunFullSetup();

        string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Build_Lab1"));
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

        string exePath = Path.Combine(buildDir, "2312690_3DSurvivalGame.exe");
        Debug.Log(">>> Bắt đầu Build Standalone Windows EXE tại: " + exePath);

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
            Debug.Log("🎉 BUILD THÀNH CÔNG! File thực thi tại: " + exePath);
        }
        else
        {
            Debug.LogError("❌ BUILD THẤT BẠI: " + report.summary.result);
        }
    }

    private static void EnsureFolders()
    {
        string[] dirs = { MaterialsFolder, TexturesFolder, PrefabsFolder, TerrainDataFolder, ScenesFolder };
        foreach (var d in dirs)
        {
            if (!AssetDatabase.IsValidFolder(d))
            {
                string parent = Path.GetDirectoryName(d).Replace('\\', '/');
                string folder = Path.GetFileName(d);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
        AssetDatabase.Refresh();
    }

    private static void SetupLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        bool hasGround = false;
        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == "Ground")
            {
                hasGround = true;
                break;
            }
        }
        if (!hasGround)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                var elem = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(elem.stringValue))
                {
                    elem.stringValue = "Ground";
                    tagManager.ApplyModifiedProperties();
                    Debug.Log(">>> Đã thiết lập Layer 'Ground' tại index: " + i);
                    break;
                }
            }
        }
    }

    private static Texture2D CreateNoiseTexture(string name, Color baseCol, Color darkCol, int size, float scale)
    {
        string path = TexturesFolder + "/" + name + ".png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * scale, y * scale);
                float n2 = Mathf.PerlinNoise(x * scale * 2.5f, y * scale * 2.5f) * 0.5f;
                float blend = Mathf.Clamp01(n * 0.7f + n2 * 0.3f);
                Color c = Color.Lerp(darkCol, baseCol, blend);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D CreateGrassBladeTexture(string name)
    {
        string path = TexturesFolder + "/" + name + ".png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        int w = 64, h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            float halfWidth = (1f - t * 0.85f) * (w * 0.4f);
            float centerX = w * 0.5f + Mathf.Sin(t * 3f) * 6f;
            Color grassColor = Color.Lerp(new Color(0.20f, 0.45f, 0.15f), new Color(0.40f, 0.75f, 0.25f), t);

            for (int x = 0; x < w; x++)
            {
                if (Mathf.Abs(x - centerX) <= halfWidth)
                {
                    tex.SetPixel(x, y, grassColor);
                }
                else
                {
                    tex.SetPixel(x, y, clear);
                }
            }
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material CreateTransparentMaterial(string name, Color color)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        // Thiết lập transparent cho URP
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // 1 = Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0);     // 0 = Alpha
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static TerrainLayer CreateTerrainLayer(string name, Texture2D texture, float tileSize)
    {
        string path = TerrainDataFolder + "/" + name + ".terrainlayer";
        TerrainLayer existing = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (existing != null) return existing;

        TerrainLayer layer = new TerrainLayer();
        layer.diffuseTexture = texture;
        layer.tileSize = new Vector2(tileSize, tileSize);
        AssetDatabase.CreateAsset(layer, path);
        return layer;
    }

    private static GameObject CreateTreePrefab(Material woodMat, Material leafMat)
    {
        string path = PrefabsFolder + "/Tree_Stylized.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("Tree_Stylized");

        // Trunk (Thân cây)
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0, 2f, 0);
        trunk.transform.localScale = new Vector3(0.6f, 2f, 0.6f);
        trunk.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
        GameObject.DestroyImmediate(trunk.GetComponent<Collider>());

        // Foliage 1 (Tán dưới)
        GameObject f1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        f1.name = "Leaves_Bottom";
        f1.transform.SetParent(root.transform);
        f1.transform.localPosition = new Vector3(0, 3.5f, 0);
        f1.transform.localScale = new Vector3(3.5f, 2.2f, 3.5f);
        f1.GetComponent<MeshRenderer>().sharedMaterial = leafMat;
        GameObject.DestroyImmediate(f1.GetComponent<Collider>());

        // Foliage 2 (Tán trên)
        GameObject f2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        f2.name = "Leaves_Top";
        f2.transform.SetParent(root.transform);
        f2.transform.localPosition = new Vector3(0, 5.0f, 0);
        f2.transform.localScale = new Vector3(2.6f, 2.0f, 2.6f);
        f2.GetComponent<MeshRenderer>().sharedMaterial = leafMat;
        GameObject.DestroyImmediate(f2.GetComponent<Collider>());

        // Capsule Collider cho cả cây
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 2.5f, 0);
        col.radius = 0.5f;
        col.height = 5.0f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        GameObject.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateRockPrefab(Material rockMat)
    {
        string path = PrefabsFolder + "/Rock_Stylized.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Rock_Stylized";
        root.transform.localScale = new Vector3(1.6f, 1.0f, 1.3f);
        root.GetComponent<MeshRenderer>().sharedMaterial = rockMat;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        GameObject.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateMushroomPrefab(Material stemMat, Material capMat)
    {
        string path = PrefabsFolder + "/Mushroom_Red.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("Mushroom_Red");

        // Stem
        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "Stem";
        stem.transform.SetParent(root.transform);
        stem.transform.localPosition = new Vector3(0, 0.4f, 0);
        stem.transform.localScale = new Vector3(0.25f, 0.4f, 0.25f);
        stem.GetComponent<MeshRenderer>().sharedMaterial = stemMat;
        GameObject.DestroyImmediate(stem.GetComponent<Collider>());

        // Cap
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.name = "Cap";
        cap.transform.SetParent(root.transform);
        cap.transform.localPosition = new Vector3(0, 0.75f, 0);
        cap.transform.localScale = new Vector3(0.85f, 0.45f, 0.85f);
        cap.GetComponent<MeshRenderer>().sharedMaterial = capMat;
        GameObject.DestroyImmediate(cap.GetComponent<Collider>());

        SphereCollider col = root.AddComponent<SphereCollider>();
        col.center = new Vector3(0, 0.5f, 0);
        col.radius = 0.45f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        GameObject.DestroyImmediate(root);
        return prefab;
    }

    private static void SetupLighting()
    {
        // Directional Light
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.0f, 0.96f, 0.88f);
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.sun = light;
        RenderSettings.ambientMode = AmbientMode.Skybox;
    }

    private static Terrain BuildTerrain(TerrainLayer grassLayer, TerrainLayer rockLayer, TerrainLayer dirtLayer, GameObject treePrefab, Texture2D grassBladeTex)
    {
        string dataPath = TerrainDataFolder + "/Terrain_Lab1.asset";
        TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
        if (terrainData == null)
        {
            terrainData = new TerrainData();
            AssetDatabase.CreateAsset(terrainData, dataPath);
        }

        int hRes = 257;
        terrainData.heightmapResolution = hRes;
        terrainData.size = new Vector3(300, 45, 300); // 300m x 45m x 300m

        // Sculpt Heightmap: thung lũng ở giữa, đồi nhấp nhô, viền núi cao bao quanh
        float[,] heights = new float[hRes, hRes];
        float centerX = hRes * 0.5f;
        float centerY = hRes * 0.5f;

        for (int y = 0; y < hRes; y++)
        {
            for (int x = 0; x < hRes; x++)
            {
                float dx = (x - centerX) / (float)hRes;
                float dy = (y - centerY) / (float)hRes;
                float distFromCenter = Mathf.Sqrt(dx * dx + dy * dy) * 2f; // 0 ở tâm, ~1.4 ở góc

                // Đồi thoai thoải
                float n1 = Mathf.PerlinNoise(x * 0.035f, y * 0.035f) * 0.12f;
                float n2 = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.04f;
                float baseHeight = 0.08f + n1 + n2;

                // Núi cao dựng bao bọc ngoài rìa
                float mountainRidge = Mathf.Pow(Mathf.Clamp01(distFromCenter - 0.45f) * 1.8f, 2f) * 0.65f;
                float ridgeNoise = Mathf.PerlinNoise(x * 0.05f + 10f, y * 0.05f + 10f) * 0.25f;

                heights[y, x] = Mathf.Clamp01(baseHeight + mountainRidge + mountainRidge * ridgeNoise);
            }
        }
        terrainData.SetHeights(0, 0, heights);

        // Layers: 0 = Grass, 1 = Rock, 2 = Dirt
        terrainData.terrainLayers = new TerrainLayer[] { grassLayer, rockLayer, dirtLayer };
        int aRes = terrainData.alphamapResolution;
        float[,,] alphaMaps = new float[aRes, aRes, 3];

        for (int y = 0; y < aRes; y++)
        {
            for (int x = 0; x < aRes; x++)
            {
                float normX = (float)x / aRes;
                float normY = (float)y / aRes;
                float steepness = terrainData.GetSteepness(normX, normY);

                if (steepness > 32f) // Vách núi dốc -> Đá
                {
                    alphaMaps[y, x, 0] = 0.15f;
                    alphaMaps[y, x, 1] = 0.85f;
                    alphaMaps[y, x, 2] = 0f;
                }
                else if (steepness > 18f)
                {
                    float factor = (steepness - 18f) / 14f;
                    alphaMaps[y, x, 0] = 1f - factor;
                    alphaMaps[y, x, 1] = factor;
                    alphaMaps[y, x, 2] = 0f;
                }
                else
                {
                    // Mặt đất phẳng -> Cỏ chủ đạo + vết đất mòn tự nhiên
                    float pathNoise = Mathf.PerlinNoise(normX * 12f, normY * 12f);
                    if (pathNoise > 0.68f)
                    {
                        alphaMaps[y, x, 0] = 0.3f;
                        alphaMaps[y, x, 1] = 0f;
                        alphaMaps[y, x, 2] = 0.7f;
                    }
                    else
                    {
                        alphaMaps[y, x, 0] = 1.0f;
                        alphaMaps[y, x, 1] = 0f;
                        alphaMaps[y, x, 2] = 0f;
                    }
                }
            }
        }
        terrainData.SetAlphamaps(0, 0, alphaMaps);

        // Trees (Cây cối)
        TreePrototype[] treePrototypes = new TreePrototype[1];
        treePrototypes[0] = new TreePrototype { prefab = treePrefab };
        terrainData.treePrototypes = treePrototypes;

        List<TreeInstance> treeInstances = new List<TreeInstance>();
        System.Random rnd = new System.Random(42);
        for (int i = 0; i < 220; i++)
        {
            float px = (float)rnd.NextDouble() * 0.7f + 0.15f;
            float pz = (float)rnd.NextDouble() * 0.7f + 0.15f;

            // Tránh spawn cây ngay tại vị trí spawn của player (tâm map)
            if (Mathf.Abs(px - 0.5f) < 0.08f && Mathf.Abs(pz - 0.5f) < 0.08f) continue;

            float steep = terrainData.GetSteepness(px, pz);
            if (steep < 25f)
            {
                TreeInstance ti = new TreeInstance
                {
                    position = new Vector3(px, 0, pz),
                    widthScale = (float)(rnd.NextDouble() * 0.4f + 0.85f),
                    heightScale = (float)(rnd.NextDouble() * 0.4f + 0.85f),
                    color = Color.white,
                    lightmapColor = Color.white,
                    prototypeIndex = 0
                };
                treeInstances.Add(ti);
            }
        }
        terrainData.SetTreeInstances(treeInstances.ToArray(), true);

        // Grass & Detail (Cỏ chi tiết)
        DetailPrototype[] detailPrototypes = new DetailPrototype[1];
        detailPrototypes[0] = new DetailPrototype
        {
            prototypeTexture = grassBladeTex,
            renderMode = DetailRenderMode.GrassBillboard,
            minWidth = 0.8f,
            maxWidth = 1.3f,
            minHeight = 0.7f,
            maxHeight = 1.2f,
            healthyColor = new Color(0.35f, 0.75f, 0.20f),
            dryColor = new Color(0.65f, 0.70f, 0.25f)
        };
        terrainData.detailPrototypes = detailPrototypes;

        int dRes = terrainData.detailResolution;
        int[,] detailLayer = new int[dRes, dRes];
        for (int y = 0; y < dRes; y++)
        {
            for (int x = 0; x < dRes; x++)
            {
                float nx = (float)x / dRes;
                float ny = (float)y / dRes;
                float st = terrainData.GetSteepness(nx, ny);
                if (st < 18f && Mathf.PerlinNoise(nx * 15f, ny * 15f) > 0.45f)
                {
                    detailLayer[y, x] = rnd.Next(2, 6);
                }
                else
                {
                    detailLayer[y, x] = 0;
                }
            }
        }
        terrainData.SetDetailLayer(0, 0, 0, detailLayer);

        AssetDatabase.SaveAssets();

        // Tạo GameObject Terrain trong Scene
        GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.name = "Terrain_Environment";
        terrainObj.transform.position = Vector3.zero;

        // Gán Layer Ground
        int groundLayerIdx = LayerMask.NameToLayer("Ground");
        if (groundLayerIdx != -1)
        {
            terrainObj.layer = groundLayerIdx;
        }

        Terrain terrainComp = terrainObj.GetComponent<Terrain>();
        terrainComp.treeDistance = 1000f;
        terrainComp.detailObjectDistance = 80f;

        return terrainComp;
    }

    private static void ScatterProps(Terrain terrain, GameObject rockPrefab, GameObject mushroomPrefab, Material waterMat)
    {
        GameObject envPropsRoot = new GameObject("Environment_Props");

        // Hồ nước (Water Pond) ở thung lũng trung tâm
        GameObject waterObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterObj.name = "Water_Lake";
        waterObj.transform.SetParent(envPropsRoot.transform);
        waterObj.transform.position = new Vector3(150f, 6.2f, 150f);
        waterObj.transform.localScale = new Vector3(4.5f, 1f, 4.5f); // ~45m x 45m
        waterObj.GetComponent<MeshRenderer>().sharedMaterial = waterMat;

        System.Random rnd = new System.Random(101);

        // Rải Đá (Rocks)
        GameObject rocksGroup = new GameObject("Rocks_Scattered");
        rocksGroup.transform.SetParent(envPropsRoot.transform);
        for (int i = 0; i < 60; i++)
        {
            float wx = (float)rnd.NextDouble() * 200f + 50f;
            float wz = (float)rnd.NextDouble() * 200f + 50f;
            float wy = terrain.SampleHeight(new Vector3(wx, 0, wz));

            if (wy > 7.0f) // Tránh ngập dưới nước
            {
                GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab);
                rock.transform.SetParent(rocksGroup.transform);
                rock.transform.position = new Vector3(wx, wy - 0.2f, wz);
                float scale = (float)rnd.NextDouble() * 1.5f + 0.8f;
                rock.transform.localScale = new Vector3(scale * 1.2f, scale * 0.9f, scale * 1.1f);
                rock.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 30f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 30f);
            }
        }

        // Rải Nấm (Mushrooms) mọc thành cụm
        GameObject mushroomsGroup = new GameObject("Mushrooms_Scattered");
        mushroomsGroup.transform.SetParent(envPropsRoot.transform);
        for (int cluster = 0; cluster < 18; cluster++)
        {
            float cx = (float)rnd.NextDouble() * 160f + 70f;
            float cz = (float)rnd.NextDouble() * 160f + 70f;
            int countInCluster = rnd.Next(3, 8);

            for (int m = 0; m < countInCluster; m++)
            {
                float mx = cx + (float)(rnd.NextDouble() * 4f - 2f);
                float mz = cz + (float)(rnd.NextDouble() * 4f - 2f);
                float my = terrain.SampleHeight(new Vector3(mx, 0, mz));

                if (my > 7.0f)
                {
                    GameObject shroom = (GameObject)PrefabUtility.InstantiatePrefab(mushroomPrefab);
                    shroom.transform.SetParent(mushroomsGroup.transform);
                    shroom.transform.position = new Vector3(mx, my, mz);
                    float s = (float)rnd.NextDouble() * 0.6f + 0.7f;
                    shroom.transform.localScale = new Vector3(s, s, s);
                    shroom.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
                }
            }
        }
    }

    private static GameObject SetupFPPPlayer(Terrain terrain)
    {
        // Vị trí spawn trên đồi cỏ cạnh hồ nước
        float spawnX = 142f;
        float spawnZ = 135f;
        float groundY = terrain.SampleHeight(new Vector3(spawnX, 0, spawnZ));
        Vector3 spawnPos = new Vector3(spawnX, groundY + 1.2f, spawnZ);

        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = spawnPos;

        // CharacterController
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2.0f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1.0f, 0);
        cc.stepOffset = 0.35f;
        cc.slopeLimit = 45f;

        // PlayerMovement script
        PlayerMovement pm = player.AddComponent<PlayerMovement>();
        pm.controller = cc;
        pm.speed = 12f;
        pm.gravity = -19.62f;
        pm.jumpHeight = 3.0f;
        pm.groundDistance = 0.4f;

        int groundLayerIdx = LayerMask.NameToLayer("Ground");
        if (groundLayerIdx != -1)
        {
            pm.groundMask = 1 << groundLayerIdx;
        }
        else
        {
            pm.groundMask = ~0; // Mặc định mọi layer
        }

        // GroundCheck child
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0, 0.05f, 0);
        pm.groundCheck = groundCheck.transform;

        // Main Camera child
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(player.transform);
        camObj.transform.localPosition = new Vector3(0, 1.65f, 0); // Ngang tầm mắt FPP
        camObj.transform.localRotation = Quaternion.identity;

        Camera cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        camObj.AddComponent<AudioListener>();

        // MouseMovement script
        MouseMovement mm = camObj.AddComponent<MouseMovement>();
        mm.mouseSensitivity = 120f;
        mm.playerBody = player.transform; // Xoay thân player theo trục Y

        // Lưu Player Prefab
        string prefabPath = PrefabsFolder + "/Player.prefab";
        PrefabUtility.SaveAsPrefabAsset(player, prefabPath);

        return player;
    }
}
#endif
