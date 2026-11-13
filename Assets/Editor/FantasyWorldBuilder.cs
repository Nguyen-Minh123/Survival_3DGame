#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// FantasyWorldBuilder - Chuyên gia Thiết kế Bản đồ 3D (Level Design, Environment Art & Technical Art)
/// Tích hợp hoàn chỉnh 3 bộ Asset Store được cung cấp cùng hệ thống Survival Homestead & POLYGON Nature:
/// 1. Fantasy Skybox FREE (ID 18353 - Render Knight): Bầu trời 판타지 độ phân giải cao (FS013_Day / FS002_Day).
/// 2. Fantasy Landscape (ID 103573 - Pxltiger): Rừng cây Bạch Dương (Birch_tree1, Birch_tree2), Đá rêu (Rock1),
///    Nấm rừng (Mushroom1), Hoa dại (Flower1, Flower2), Cỏ 3D (Grass1) & Texture mặt đất (ground_grass, ground_road).
/// 3. Terrain Sample Asset Pack (ID 145808 - Unity Technologies): Hệ thống PBR Terrain Layers & Normal Maps
///    (Grass, Pebbles, Sand, Rock) và các khối đá vách núi phủ rêu chi tiết cao (Rock_C_01, Rock_Overgrown_B, Rock_Overgrown_D).
/// </summary>
[InitializeOnLoad]
public static class FantasyWorldBuilder
{
    private const string ProjectRoot = "Assets";
    private const string ModelsFolder = ProjectRoot + "/Models";
    private const string MaterialsFolder = ProjectRoot + "/Materials";
    private const string TexturesFolder = ProjectRoot + "/Textures";
    private const string PrefabsFolder = ProjectRoot + "/Prefabs";
    private const string ResourcesFolder = ProjectRoot + "/Resources";

    // 1. Thư mục Asset Store #1: Fantasy Skybox FREE (ID 18353)
    private const string FantasySkyboxRoot = ProjectRoot + "/Fantasy Skybox FREE";

    // 2. Thư mục Asset Store #2: Fantasy Landscape (ID 103573 - Pxltiger)
    private const string PxltigerRoot = ProjectRoot + "/FantasyEnvironments/Environments";
    private const string PxltigerBirchPrefabs = PxltigerRoot + "/Ambient-Occlusion-Trees/Prefabs";
    private const string PxltigerBirchMats = PxltigerRoot + "/Ambient-Occlusion-Trees/Materials";
    private const string PxltigerBirchTex = PxltigerRoot + "/Ambient-Occlusion-Trees/Textures";
    private const string PxltigerPrefabs = PxltigerRoot + "/Prefabs";
    private const string PxltigerMats = PxltigerRoot + "/Materials";
    private const string PxltigerTex = PxltigerRoot + "/Textures";

    // 3. Thư mục Asset Store #3: Terrain Sample Asset Pack (ID 145808 - Unity Technologies)
    private const string TerrainPackRoot = ProjectRoot + "/TerrainSampleAssets";
    private const string TerrainPackRocks = TerrainPackRoot + "/Prefabs/Rocks";
    private const string TerrainPackRockMats = TerrainPackRocks + "/Materials";
    private const string TerrainPackRockTex = TerrainPackRocks + "/Textures";
    private const string TerrainPackTex = TerrainPackRoot + "/Textures/Terrain";

    // 4. Thư mục Assets POLYGON Nature & Survival Engine 3D hiện có
    private const string PolyNaturePath = ProjectRoot + "/PolygonNature";
    private const string PolyTrees = PolyNaturePath + "/Prefabs/Trees";
    private const string PolyPlants = PolyNaturePath + "/Prefabs/Plants";
    private const string PolyRocks = PolyNaturePath + "/Prefabs/Rocks";
    private const string PolyProps = PolyNaturePath + "/Prefabs/Props";
    private const string PolyTerrainPrefabs = PolyNaturePath + "/Prefabs/Terrain";

    private const string SurvivalPrefabs = ProjectRoot + "/SurvivalEngine/Prefabs";
    private const string SurvivalModels = ProjectRoot + "/SurvivalEngine/Models";
    private const string SurvivalMaterials = ProjectRoot + "/SurvivalEngine/Materials";
    private const string WhiteRabbitPrefabs = ProjectRoot + "/WhiteRabbit/Prefabs";

    // Thông số địa hình chuẩn của Khu Căn Cứ & Sông Hồ
    private const float CampPlateauHeight = 8.0f;   // Độ cao mặt phẳng căn cứ sinh tồn (8.0m)
    private const float WaterSurfaceHeight = 6.65f; // Độ cao mặt nước sông & hồ (6.65m - thấp hơn bờ 1.35m tự nhiên)
    private const float RiverBedHeight = 4.4f;      // Độ sâu đáy sông & đáy hồ (4.4m - sâu hơn mặt nước 2.25m)

    private const string AutoUpgradeMarkerName = "Fantasy_AssetStore_V4_WhiteRabbit_Integrated";

    static FantasyWorldBuilder()
    {
        EditorApplication.delayCall += TryAutoApplyRedesignOnce;
    }

    private static void TryAutoApplyRedesignOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Terrain terrain = Object.FindAnyObjectByType<Terrain>();
        if (terrain == null) return;

        GameObject envRoot = GameObject.Find("Fantasy_World_Environment");
        if (envRoot != null && envRoot.transform.Find(AutoUpgradeMarkerName) != null)
        {
            return; // Đã tích hợp bản thiết kế V3 với 3 bộ Asset Store mới
        }

        DecorateWorld();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Hàm nội suy mượt chuẩn GLSL smoothstep(edge0, edge1, x) trả về giá trị trong [0, 1].
    /// </summary>
    private static float SmoothRange(float edge0, float edge1, float x)
    {
        if (Mathf.Approximately(edge0, edge1)) return (x >= edge1) ? 1f : 0f;
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Khoảng cách từ điểm P đến đoạn thẳng AB trên mặt phẳng XZ (dùng để vẽ mạng lưới đường mòn kết nối các khu vực).
    /// </summary>
    private static float DistanceToSegmentXZ(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
        Vector2 proj = a + t * ab;
        return Vector2.Distance(p, proj);
    }

    [MenuItem("3D Survival Game/🌲 Trang Trí Lại Bản Đồ (Refresh World Map)", false, 2)]
    public static void DecorateWorld()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        EnsureFolders();

        Terrain terrain = Object.FindAnyObjectByType<Terrain>();
        if (terrain == null)
        {
            Debug.LogWarning("Không tìm thấy Terrain trong Scene!");
            return;
        }

        // Dọn dẹp môi trường cũ
        string[] oldRoots = { "Environment_Props", "Fantasy_World_Environment" };
        foreach (string rName in oldRoots)
        {
            GameObject old = GameObject.Find(rName);
            if (old != null) Object.DestroyImmediate(old);
        }

        GameObject worldRoot = new GameObject("Fantasy_World_Environment");
        GameObject marker = new GameObject(AutoUpgradeMarkerName);
        marker.transform.SetParent(worldRoot.transform);

        // 1. Nâng cấp toàn bộ vật liệu từ 3 gói Asset Store sang URP & bật GPU Instancing để tối ưu Draw Calls
        FixAllEnvironmentMaterials();

        // 2. Điêu khắc địa hình (Terrain) & Sơn Splatmap 4 lớp (Cỏ thảo nguyên, Đường mòn đất đá, Cát bờ sông, Đá vách núi)
        UpgradeTerrainVisuals(terrain);

        // 3. Thiết lập Bầu trời Fantasy Skybox FREE (FS013_Day), Ánh sáng mặt trời ấm áp & Sương mù chiều sâu
        SetupAtmosphereAndLighting(worldRoot.transform);

        // 4. Dựng Sông, Hồ Nước Trong Vắt Liền Mạch, Thác Nước & Cầu Gỗ Bắc Ngang Sông
        BuildCarvedRiverLakeAndBridge(terrain, worldRoot.transform);

        // 5. Dựng Khu Căn Cứ Sinh Tồn (Nhà gỗ 3D, Lều gỗ, Lều trại, Bàn chế tạo, Lò rèn, Lửa trại, Hàng rào gỗ)
        BuildSurvivalHomesteadAndCamp(terrain, worldRoot.transform);

        // 6. Trồng Hệ Sinh Thái Rừng Đa Tầng (Tiền cảnh - Trung cảnh - Hậu cảnh) kết hợp Birch Trees, Overgrown Rocks & AI Animals
        BuildForestEcosystemAndWildlife(terrain, worldRoot.transform);

        // 7. Đặt vị trí Player ngay tại lối vào Căn Cứ Sinh Tồn, nhìn thẳng ra Lửa trại, Nhà gỗ & Cây cầu bắc qua sông
        PositionPlayerInScenicSpot(terrain);

        EditorUtility.SetDirty(terrain);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("🌲 Đã hoàn tất thiết kế lại Bản Đồ Sinh Tồn 3D từ 3 bộ Asset Store (Fantasy Skybox FREE, Fantasy Landscape & Terrain Sample Pack)!");
    }

    [MenuItem("3D Survival Game/🐇 Thêm & Làm Mới Thỏ Trắng AI (White Rabbit)", false, 4)]
    public static void RefreshWhiteRabbits()
    {
        Terrain terrain = Object.FindAnyObjectByType<Terrain>();
        if (terrain == null)
        {
            Debug.LogWarning("Không tìm thấy Terrain!");
            return;
        }

        GameObject wildlifeGO = GameObject.Find("Wildlife_Animals_3D");
        Transform wildlifeGroup;
        if (wildlifeGO != null)
        {
            wildlifeGroup = wildlifeGO.transform;
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in wildlifeGroup)
            {
                if (child.name.Contains("WhiteRabbit"))
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroy) Object.DestroyImmediate(go);
        }
        else
        {
            GameObject worldRoot = GameObject.Find("Fantasy_World_Environment");
            if (worldRoot == null) worldRoot = new GameObject("Fantasy_World_Environment");
            wildlifeGroup = new GameObject("Wildlife_Animals_3D").transform;
            wildlifeGroup.SetParent(worldRoot.transform);
        }

        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 182f, 194f, 75f, 1.15f, 1.4f, 4.6f, 9f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 187f, 206f, 130f, 1.1f, 1.4f, 4.6f, 9f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 214f, 185f, 45f, 1.15f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 218f, 212f, 220f, 1.1f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 210f, 164f, 310f, 1.1f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 245f, 168f, 180f, 1.15f, 1.4f, 4.6f, 11f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 244f, 198f, 90f, 1.1f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("🐇 Đã hoàn tất thêm 7 chú Thỏ Trắng (White Rabbit - Asset Store ID 138709) kèm AI chuyển động vào Bản Đồ!");
    }

    public static void PopulateRichEnvironment(Terrain terrain)
    {
        DecorateWorld();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(ModelsFolder)) AssetDatabase.CreateFolder(ProjectRoot, "Models");
        if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder(ProjectRoot, "Materials");
        if (!AssetDatabase.IsValidFolder(TexturesFolder)) AssetDatabase.CreateFolder(ProjectRoot, "Textures");
        if (!AssetDatabase.IsValidFolder(PrefabsFolder)) AssetDatabase.CreateFolder(ProjectRoot, "Prefabs");
        if (!AssetDatabase.IsValidFolder(ResourcesFolder)) AssetDatabase.CreateFolder(ProjectRoot, "Resources");
    }

    // ==================== 1. ĐẢM BẢO VẬT LIỆU URP CHUẨN XÁC & BẬT GPU INSTANCING ====================

    private static void FixAllEnvironmentMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");
        if (urpLit == null) return;

        // 1.1. POLYGON Nature Main Material
        Material mainPolyMat = AssetDatabase.LoadAssetAtPath<Material>(PolyNaturePath + "/Materials/PolygonNature_01.mat");
        Texture2D mainPolyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PolyNaturePath + "/Textures/PolygonNature_01.png");
        if (mainPolyMat != null)
        {
            mainPolyMat.shader = urpLit;
            if (mainPolyTex != null)
            {
                mainPolyMat.SetTexture("_BaseMap", mainPolyTex);
                mainPolyMat.SetTexture("_MainTex", mainPolyTex);
            }
            mainPolyMat.SetColor("_BaseColor", Color.white);
            mainPolyMat.SetColor("_Color", Color.white);
            mainPolyMat.SetFloat("_Smoothness", 0.1f);
            mainPolyMat.enableInstancing = true;
            EditorUtility.SetDirty(mainPolyMat);
        }

        // 1.2. Chỉnh lại vật liệu Cờ hiệu (Flag.mat) sang màu đỏ thẫm đẹp mắt
        Material flagMat = AssetDatabase.LoadAssetAtPath<Material>(SurvivalMaterials + "/Flag.mat");
        if (flagMat != null)
        {
            flagMat.shader = urpLit;
            flagMat.SetTexture("_BaseMap", null);
            flagMat.SetTexture("_MainTex", null);
            Color bannerRed = new Color(0.74f, 0.18f, 0.15f, 1.0f);
            flagMat.SetColor("_BaseColor", bannerRed);
            flagMat.SetColor("_Color", bannerRed);
            flagMat.SetFloat("_Smoothness", 0.15f);
            flagMat.enableInstancing = true;
            EditorUtility.SetDirty(flagMat);
        }

        // 1.3. Chuyển đổi vật liệu của gói Fantasy Landscape (ID 103573 - Pxltiger) sang URP Lit + Alpha Clipping + GPU Instancing
        ConfigureURPMaterial(
            PxltigerBirchMats + "/Birch_bark.mat",
            PxltigerBirchTex + "/Birch_bark.tga",
            null, false, 0.5f, 0.08f, Color.white);

        ConfigureURPMaterial(
            PxltigerBirchMats + "/Birch_leaves.mat",
            PxltigerBirchTex + "/Birch_leaves.tga",
            null, true, 0.35f, 0.05f, Color.white);

        ConfigureURPMaterial(
            PxltigerBirchMats + "/Birch_leaves 1.mat",
            PxltigerBirchTex + "/Birch_leaves.tga",
            null, true, 0.35f, 0.05f, new Color(0.94f, 1.0f, 0.86f, 1.0f));

        ConfigureURPMaterial(
            PxltigerMats + "/Plant.mat",
            PxltigerTex + "/Plant.tga",
            null, true, 0.35f, 0.06f, Color.white);

        ConfigureURPMaterial(
            PxltigerMats + "/Rock1.mat",
            PxltigerTex + "/Rock1.tga",
            null, false, 0.5f, 0.12f, Color.white);

        // 1.4. Chuyển đổi vật liệu đá HDRP của gói Terrain Sample Asset Pack (ID 145808) sang URP Lit + Normal Map + GPU Instancing
        ConfigureURPMaterial(
            TerrainPackRockMats + "/Rock_C.mat",
            TerrainPackRockTex + "/Rock_C_Albedo.tif",
            TerrainPackRockTex + "/Rock_C_Normal.tif",
            false, 0.5f, 0.14f, Color.white);

        ConfigureURPMaterial(
            TerrainPackRockMats + "/Rock_Overgrown_B.mat",
            TerrainPackRockTex + "/Rock_Overgrown_B/Rock_Overgrown_B_BaseColor.tif",
            TerrainPackRockTex + "/Rock_Overgrown_B/Rock_Overgrown_B_Normal.tif",
            false, 0.5f, 0.14f, Color.white);

        ConfigureURPMaterial(
            TerrainPackRockMats + "/Rock_Overgrown_D.mat",
            TerrainPackRockTex + "/Rock_Overgrown_I/Rock_Overgrown_I_BaseColor.tif",
            TerrainPackRockTex + "/Rock_Overgrown_I/Rock_Overgrown_I_Normal.tif",
            false, 0.5f, 0.14f, Color.white);
    }

    private static void ConfigureURPMaterial(string matPath, string baseTexPath, string normalTexPath,
        bool alphaClip, float cutoff, float smoothness, Color tint)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) return;

        mat.shader = urpLit;
        Texture2D baseTex = !string.IsNullOrEmpty(baseTexPath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexPath) : null;
        Texture2D normTex = !string.IsNullOrEmpty(normalTexPath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexPath) : null;

        if (baseTex != null)
        {
            mat.SetTexture("_BaseMap", baseTex);
            mat.SetTexture("_MainTex", baseTex);
        }
        if (normTex != null)
        {
            mat.SetTexture("_BumpMap", normTex);
            mat.EnableKeyword("_NORMALMAP");
        }

        mat.SetColor("_BaseColor", tint);
        mat.SetColor("_Color", tint);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0f);

        if (alphaClip)
        {
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", cutoff);
            mat.SetFloat("_Cull", 0f); // Render 2 mặt cho tán lá & hoa cỏ
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
        }
        else
        {
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_Cull", 2f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = -1;
        }

        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
    }

    private static Material GetOrCreateCrystalWaterMaterial()
    {
        string path = MaterialsFolder + "/Mat_CrystalSurvivalWater.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(urpLit);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = urpLit;
        Color waterCol = new Color(0.11f, 0.56f, 0.76f, 1.0f);
        mat.SetColor("_BaseColor", waterCol);
        mat.SetColor("_Color", waterCol);
        mat.SetFloat("_Smoothness", 0.90f);
        mat.SetFloat("_Metallic", 0.14f);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ==================== 2. ĐIÊU KHẮC ĐỊA HÌNH & SPLATMAP 4 LỚP TỪ ASSET STORE ====================

    private static float GetRiverCenterX(float wz)
    {
        return 228f + Mathf.Sin((wz - 200f) * 0.045f) * 3.0f;
    }

    private static float EvaluateSurvivalWorldElevation(float wx, float wz)
    {
        // 1. Độ cao nền thung lũng & đồi núi bao quanh
        float distFromValleyCenter = Vector2.Distance(new Vector2(wx, wz), new Vector2(208f, 195f));
        float hillNoise =
            Mathf.Sin(wx * 0.025f) * Mathf.Cos(wz * 0.025f) * 3.8f +
            Mathf.Sin((wx + wz) * 0.05f) * 1.6f +
            Mathf.PerlinNoise(wx * 0.018f + 1.3f, wz * 0.018f + 2.7f) * 5.5f;

        float valleyFactor = SmoothRange(66f, 150f, distFromValleyCenter);
        float baseElevation = Mathf.Lerp(CampPlateauHeight, 13.5f + hillNoise, valleyFactor);

        // Làm phẳng tuyệt đối khu vực Căn Cứ Sinh Tồn (tâm 196, 200, bán kính 25m) và Bờ Đông cầu (tâm 244, 200, bán kính 12m)
        float distCamp = Vector2.Distance(new Vector2(wx, wz), new Vector2(196f, 200f));
        float campFlatWeight = 1f - SmoothRange(18f, 28f, distCamp);
        baseElevation = Mathf.Lerp(baseElevation, CampPlateauHeight, campFlatWeight);

        float distEastBank = Vector2.Distance(new Vector2(wx, wz), new Vector2(244f, 200f));
        float eastFlatWeight = 1f - SmoothRange(8f, 16f, distEastBank);
        baseElevation = Mathf.Lerp(baseElevation, CampPlateauHeight, eastFlatWeight);

        // Độ nhiễu tự nhiên dọc bờ nước để bờ sông/bờ hồ uốn lượn mềm mại (giữ phẳng tại 2 đầu cầu gỗ wz ~ 200)
        float bridgeZoneMask = SmoothRange(4.5f, 11.0f, Mathf.Abs(wz - 200f));
        float organicShoreNoise = (Mathf.PerlinNoise(wx * 0.09f + 3.1f, wz * 0.09f + 7.4f) - 0.5f) * 2.8f * bridgeZoneMask;

        // 2. Đào Lòng Sông (River Gorge) chảy từ chân thác phía Bắc (wz = 258) qua Cầu Gỗ (wz = 200) mở rộng dần vào Hồ Nước (wz = 148)
        float riverDepthFactor = 0f;
        if (wz >= 145f && wz <= 260f)
        {
            float riverCenterX = GetRiverCenterX(wz);
            float dxRiver = Mathf.Abs(wx - riverCenterX);

            float lakeFlare = (1f - SmoothRange(150f, 182f, wz)) * 11.5f;
            float riverBedRadius = 3.8f + lakeFlare * 0.65f + organicShoreNoise * 0.45f;
            float riverBankRadius = 8.4f + lakeFlare + organicShoreNoise;

            if (dxRiver < riverBankRadius)
            {
                float t = 1f - SmoothRange(riverBedRadius, riverBankRadius, dxRiver);
                float northFade = 1f - SmoothRange(250f, 259f, wz);
                riverDepthFactor = Mathf.Max(riverDepthFactor, t * northFade);
            }
        }

        // 3. Đào Lòng Hồ Lớn (Crystal Lake Basin) ở phía Nam dòng sông (tâm wx = 229, wz = 148) nối liền mạch với cửa sông
        float lakeDx = (wx - 229f) / (23.0f + organicShoreNoise);
        float lakeDz = (wz - 148f) / (21.5f + organicShoreNoise);
        float lakeDistNorm = Mathf.Sqrt(lakeDx * lakeDx + lakeDz * lakeDz);
        float lakeDepthFactor = 0f;
        if (lakeDistNorm < 1.0f)
        {
            lakeDepthFactor = 1f - SmoothRange(0.48f, 1.0f, lakeDistNorm);
        }

        float waterCarve = Mathf.Max(riverDepthFactor, lakeDepthFactor);
        float carvedElevation = Mathf.Lerp(baseElevation, RiverBedHeight, waterCarve);

        // Nâng vách núi đá tự nhiên ngay phía sau thác nước (wz ~ 266, wx ~ 229)
        float cliffDist = Vector2.Distance(new Vector2(wx, wz), new Vector2(229f, 268f));
        if (cliffDist < 20f)
        {
            float cliffBoost = (1f - SmoothRange(6f, 20f, cliffDist)) * 10.5f;
            carvedElevation += cliffBoost;
        }

        return Mathf.Clamp(carvedElevation, 1.0f, 34.0f);
    }

    private static void UpgradeTerrainVisuals(Terrain terrain)
    {
        TerrainData tData = terrain.terrainData;
        if (tData == null) return;

        terrain.transform.position = Vector3.zero;
        tData.heightmapResolution = 513;
        tData.size = new Vector3(400f, 36f, 400f);

        // Sử dụng vật liệu TerrainLit chính thức của gói URP để hiển thị 4 lớp Splatmap trong 1 Pass duy nhất
        Material urpDefaultTerrainMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/TerrainLit.mat");
        if (urpDefaultTerrainMat != null)
        {
            terrain.materialTemplate = urpDefaultTerrainMat;
        }

        // Kết hợp Texture & Normal Map từ Fantasy Landscape (103573) và Terrain Sample Asset Pack (145808):
        // - Layer 0 (Cỏ thảo nguyên): ground_grass.tga (Pxltiger) + Grass_A_Normal.tif (Unity Terrain Pack)
        // - Layer 1 (Đường mòn đất/đá): ground_road.tga (Pxltiger) + Pebbles_B_Normal.tif (Unity Terrain Pack)
        // - Layer 2 (Cát bờ sông/hồ): Sand_BaseColor.tif + Sand_Normal.tif (Unity Terrain Pack)
        // - Layer 3 (Đá vách núi): Rock_BaseColor.tif + Rock_Normal.tif (Unity Terrain Pack)
        TerrainLayer layerGrass = GetOrCreateAssetTerrainLayer(
            "TL_SurvivalGrass_V2",
            PxltigerTex + "/ground_grass.tga",
            TerrainPackTex + "/Grass_A_Normal.tif",
            new Color(0.24f, 0.52f, 0.18f), 10f, 0.65f, 0.02f);

        TerrainLayer layerRoad = GetOrCreateAssetTerrainLayer(
            "TL_SurvivalRoad_V2",
            PxltigerTex + "/ground_road.tga",
            TerrainPackTex + "/Pebbles_B_Normal.tif",
            new Color(0.55f, 0.44f, 0.30f), 7f, 0.75f, 0.04f);

        TerrainLayer layerSand = GetOrCreateAssetTerrainLayer(
            "TL_SurvivalSand_V2",
            TerrainPackTex + "/Sand_BaseColor.tif",
            TerrainPackTex + "/Sand_Normal.tif",
            new Color(0.76f, 0.63f, 0.38f), 8f, 0.85f, 0.05f);

        TerrainLayer layerRock = GetOrCreateAssetTerrainLayer(
            "TL_SurvivalRock_V2",
            TerrainPackTex + "/Rock_BaseColor.tif",
            TerrainPackTex + "/Rock_Normal.tif",
            new Color(0.42f, 0.43f, 0.45f), 14f, 1.15f, 0.08f);

        tData.terrainLayers = new TerrainLayer[] { layerGrass, layerRoad, layerSand, layerRock };

        SculptTerrainHeightmap(tData);
        PaintTerrainSplatmap(tData);

        terrain.Flush();
        TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
        if (tc != null) tc.terrainData = tData;
    }

    private static TerrainLayer GetOrCreateAssetTerrainLayer(string name, string diffusePath, string normalPath,
        Color fallbackColor, float tileSize, float normalScale, float smoothness)
    {
        Texture2D diffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
        Texture2D normTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

        if (diffTex == null)
        {
            string fallbackTexPath = TexturesFolder + "/" + name + "_Tex.png";
            diffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(fallbackTexPath);
            if (diffTex == null)
            {
                diffTex = new Texture2D(128, 128, TextureFormat.RGBA32, true);
                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        Color col = fallbackColor;
                        col.a = 1.0f;
                        diffTex.SetPixel(x, y, col);
                    }
                }
                diffTex.Apply();
                File.WriteAllBytes(fallbackTexPath, diffTex.EncodeToPNG());
                AssetDatabase.ImportAsset(fallbackTexPath);
                diffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(fallbackTexPath);
            }
        }

        string layerPath = MaterialsFolder + "/" + name + ".terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, layerPath);
        }
        layer.diffuseTexture = diffTex;
        layer.normalMapTexture = normTex;
        layer.normalScale = normalScale;
        layer.tileSize = new Vector2(tileSize, tileSize);
        layer.metallic = 0f;
        layer.smoothness = smoothness;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static void SculptTerrainHeightmap(TerrainData tData)
    {
        int res = tData.heightmapResolution;
        float[,] heights = new float[res, res];
        float maxH = tData.size.y;

        for (int y = 0; y < res; y++)
        {
            float wz = ((float)y / (res - 1)) * tData.size.z;
            for (int x = 0; x < res; x++)
            {
                float wx = ((float)x / (res - 1)) * tData.size.x;
                float elev = EvaluateSurvivalWorldElevation(wx, wz);
                heights[y, x] = elev / maxH;
            }
        }

        tData.SetHeights(0, 0, heights);
    }

    private static void PaintTerrainSplatmap(TerrainData tData)
    {
        if (tData.terrainLayers == null || tData.terrainLayers.Length < 4) return;

        int alphaRes = tData.alphamapResolution;
        float[,,] splat = new float[alphaRes, alphaRes, 4];

        // Các tuyến đường mòn kết nối các phân khu trên bản đồ:
        // 1. Tuyến chính Tây - Đông: Từ Điểm Spawn (178, 200) -> Lửa trại (198, 200) -> Cầu Gỗ (228, 200) -> Tàn tích cổ (253, 202)
        Vector2 roadWestStart = new Vector2(176f, 200f);
        Vector2 roadCampCenter = new Vector2(198f, 200f);
        Vector2 roadEastRuins = new Vector2(253f, 202f);
        // 2. Tuyến nhánh Bắc: Từ Căn cứ (194, 208) -> Hang động Gấu/Sói phía Tây Bắc (167, 243)
        Vector2 roadNorthStart = new Vector2(194f, 208f);
        Vector2 roadNorthCave = new Vector2(167f, 243f);
        // 3. Tuyến nhánh Nam: Từ Căn cứ (202, 192) -> Bờ Hồ Lớn phía Nam (213, 158)
        Vector2 roadSouthStart = new Vector2(202f, 192f);
        Vector2 roadSouthLake = new Vector2(213f, 158f);

        for (int y = 0; y < alphaRes; y++)
        {
            float normY = (float)y / (alphaRes - 1);
            float wz = normY * tData.size.z;
            for (int x = 0; x < alphaRes; x++)
            {
                float normX = (float)x / (alphaRes - 1);
                float wx = normX * tData.size.x;
                Vector2 p = new Vector2(wx, wz);

                float steepness = tData.GetSteepness(normX, normY);
                float height = tData.GetInterpolatedHeight(normX, normY);

                // Layer 3: Đá vách núi cao dốc (Rock_BaseColor + Rock_Normal)
                float rockWeight = (height > 8.25f) ? SmoothRange(18f, 33f, steepness) : 0f;

                // Layer 2: Bờ cát vàng mịn quanh lòng sông & lòng hồ (Sand_BaseColor + Sand_Normal)
                float sandWeight = (1f - SmoothRange(6.85f, 7.92f, height)) * (1f - rockWeight);

                // Layer 1: Đường mòn đất đá (ground_road + Pebbles_B_Normal) quanh sân lửa trại & các lối đi kết nối
                float distCampFire = Vector2.Distance(p, roadCampCenter);
                float campCourtyard = (1f - SmoothRange(4.2f, 9.5f, distCampFire)) * 0.82f;

                float dMainRoad = Mathf.Min(
                    DistanceToSegmentXZ(p, roadWestStart, roadCampCenter),
                    DistanceToSegmentXZ(p, roadCampCenter, roadEastRuins));
                float mainRoadWeight = (1f - SmoothRange(1.4f, 3.2f, dMainRoad)) * 0.88f;

                float dNorthTrail = DistanceToSegmentXZ(p, roadNorthStart, roadNorthCave);
                float northTrailWeight = (1f - SmoothRange(1.1f, 2.6f, dNorthTrail)) * 0.76f;

                float dSouthTrail = DistanceToSegmentXZ(p, roadSouthStart, roadSouthLake);
                float southTrailWeight = (1f - SmoothRange(1.1f, 2.6f, dSouthTrail)) * 0.76f;

                float rawRoad = Mathf.Max(campCourtyard, Mathf.Max(mainRoadWeight, Mathf.Max(northTrailWeight, southTrailWeight)));
                // Không vẽ đường đè lên lòng sông dưới gầm cầu
                float aboveWaterMask = SmoothRange(7.55f, 7.95f, height);
                float roadWeight = Mathf.Clamp01(rawRoad * aboveWaterMask * (1f - rockWeight) * (1f - sandWeight * 0.8f));

                // Layer 0: Thảm cỏ thảo nguyên xanh mướt (ground_grass + Grass_A_Normal)
                float grassWeight = Mathf.Clamp01(1f - rockWeight - sandWeight - roadWeight);

                float sum = grassWeight + roadWeight + sandWeight + rockWeight;
                if (sum < 0.0001f) sum = 1f;
                splat[y, x, 0] = grassWeight / sum;
                splat[y, x, 1] = roadWeight / sum;
                splat[y, x, 2] = sandWeight / sum;
                splat[y, x, 3] = rockWeight / sum;
            }
        }

        tData.SetAlphamaps(0, 0, splat);
    }

    // ==================== 3. BẦU TRỜI (FANTASY SKYBOX FREE), ÁNH SÁNG & SƯƠNG MÙ CHIỀU SÂU ====================

    private static void SetupAtmosphereAndLighting(Transform worldRoot)
    {
        // Ưu tiên bầu trời từ gói Asset Store #1: Fantasy Skybox FREE (FS013_Day / FS002_Day_Cubemap)
        string[] skyboxCandidates = {
            FantasySkyboxRoot + "/Materials/FS013/FS013_Day.mat",
            FantasySkyboxRoot + "/Cubemaps/FS002/FS002_Day_Cubemap.mat",
            FantasySkyboxRoot + "/Cubemaps/Classic/FS000_Day_02.mat",
            PolyNaturePath + "/Materials/Misc/PolygonNature_MountainSkybox.mat"
        };

        foreach (string candidate in skyboxCandidates)
        {
            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(candidate);
            if (skyMat != null && skyMat.shader != null && !skyMat.shader.name.Contains("Error"))
            {
                RenderSettings.skybox = skyMat;
                break;
            }
        }

        // Sương mù chiều sâu không gian (Atmospheric Fog) giúp tách biệt Tiền cảnh - Trung cảnh - Hậu cảnh
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.70f, 0.84f, 0.93f, 1f);
        RenderSettings.fogDensity = 0.0020f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.74f, 0.84f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.64f, 0.70f, 0.64f);
        RenderSettings.ambientGroundColor = new Color(0.42f, 0.45f, 0.37f);

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.color = new Color(1.0f, 0.96f, 0.86f);
                l.intensity = 1.45f;
                l.shadows = LightShadows.Soft;
                l.shadowStrength = 0.82f;
                l.transform.rotation = Quaternion.Euler(44f, -45f, 0f);
                break;
            }
        }

        Transform skyGroup = new GameObject("Sky_Clouds").transform;
        skyGroup.SetParent(worldRoot);
        string[] cloudPaths = {
            PolyProps + "/SM_Prop_Cloud_01.prefab",
            PolyProps + "/SM_Prop_Cloud_02.prefab",
            PolyProps + "/SM_Prop_Cloud_03.prefab"
        };
        System.Random rnd = new System.Random(101);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * (Mathf.PI * 2f / 12f);
            float dist = 115f + (float)rnd.NextDouble() * 55f;
            float cx = 205f + Mathf.Cos(angle) * dist;
            float cz = 195f + Mathf.Sin(angle) * dist;
            float cy = 54f + (float)rnd.NextDouble() * 18f;
            SpawnSyntyPrefab(cloudPaths[i % cloudPaths.Length], skyGroup, new Vector3(cx, cy, cz),
                (float)rnd.NextDouble() * 360f, new Vector3(1.5f, 1.5f, 1.5f), false);
        }
    }

    // ==================== 4. SÔNG, HỒ NƯỚC TỰ NHIÊN LIỀN MẠCH, THÁC NƯỚC & CẦU GỖ VỮNG CHÃI ====================

    private static void BuildCarvedRiverLakeAndBridge(Terrain terrain, Transform worldRoot)
    {
        Transform waterGroup = new GameObject("River_Lake_And_Bridge").transform;
        waterGroup.SetParent(worldRoot);

        string riverPlanePath = PolyTerrainPrefabs + "/River_Plane_01.prefab";
        Material waterMat = GetOrCreateCrystalWaterMaterial();

        // 1. Lát mặt nước liền mạch phủ kín toàn bộ lòng sông & lòng hồ ở độ cao WaterSurfaceHeight (6.65m)
        float[] waterZStrip = { 246f, 224f, 202f, 180f, 158f, 138f };
        foreach (float wzCenter in waterZStrip)
        {
            GameObject wp = SpawnSyntyPrefab(riverPlanePath, waterGroup,
                new Vector3(229f, WaterSurfaceHeight, wzCenter), 0f, new Vector3(16.0f, 1f, 10.0f), false);
            if (wp != null && waterMat != null)
            {
                foreach (var r in wp.GetComponentsInChildren<Renderer>()) r.sharedMaterial = waterMat;
            }
        }

        // 2. Thác nước (River_Plane_WaterFall_01) đổ từ vách núi đá phía Bắc (wx = 229, wz = 258) xuống thẳng đầu sông
        GameObject wf = SpawnSyntyPrefab(PolyTerrainPrefabs + "/River_Plane_WaterFall_01.prefab", waterGroup,
            new Vector3(229f, WaterSurfaceHeight + 5.2f, 258f), 180f, new Vector3(1.5f, 1.25f, 1.5f), false);
        if (wf != null && waterMat != null)
        {
            foreach (var r in wf.GetComponentsInChildren<Renderer>()) r.sharedMaterial = waterMat;
        }
        SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/SM_Waterfall_Foam.prefab", waterGroup,
            new Vector3(229f, WaterSurfaceHeight + 0.15f, 253.5f), 180f, new Vector3(1.5f, 1.2f, 1.5f), false);
        SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/FX_Water_Ripple_01.prefab", waterGroup,
            new Vector3(228f, WaterSurfaceHeight + 0.04f, 200f), 0f, Vector3.one, false);
        SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/FX_Water_Ripple_01.prefab", waterGroup,
            new Vector3(229f, WaterSurfaceHeight + 0.04f, 148f), 0f, new Vector3(1.8f, 1f, 1.8f), false);

        // Vách đá hùng vĩ bao bọc lấy đầu nguồn thác nước phía Bắc (kết hợp đá vách núi rêu phong từ TerrainSampleAssets & PolygonNature)
        SpawnOnGround(TerrainPackRocks + "/Rock_Overgrown_B.prefab", waterGroup, terrain, 220f, 257f, 25f, new Vector3(1.35f, 1.45f, 1.35f), -0.6f, true);
        SpawnOnGround(TerrainPackRocks + "/Rock_Overgrown_D.prefab", waterGroup, terrain, 238f, 257f, -35f, new Vector3(1.35f, 1.45f, 1.35f), -0.6f, true);
        SpawnOnGround(TerrainPackRocks + "/Rock_C_01.prefab", waterGroup, terrain, 229f, 264f, 0f, new Vector3(1.45f, 1.55f, 1.45f), -0.7f, true);
        SpawnOnGround(PolyRocks + "/SM_Rock_Big_01.prefab", waterGroup, terrain, 221f, 259f, 25f, new Vector3(1.5f, 1.7f, 1.5f), -0.8f);
        SpawnOnGround(PolyRocks + "/SM_Rock_Big_02.prefab", waterGroup, terrain, 237f, 259f, -30f, new Vector3(1.5f, 1.7f, 1.5f), -0.8f);
        SpawnOnGround(PolyRocks + "/SM_Rock_Cluster_Large_02.prefab", waterGroup, terrain, 214f, 254f, 40f, new Vector3(1.3f, 1.3f, 1.3f), -0.6f);
        SpawnOnGround(PolyRocks + "/SM_Rock_Cluster_Large_02.prefab", waterGroup, terrain, 244f, 254f, -40f, new Vector3(1.3f, 1.3f, 1.3f), -0.6f);

        // 3. CẦU GỖ CONG (SM_Prop_Bridge_Curved_01) BẮC NGANG QUA SÔNG TẠI (wx = 228, wz = 200)
        BuildGroundedRiverBridge(waterGroup, terrain);

        // 4. Trang trí Hoa súng (LilyPad), Lau sậy (Reeds), Đá cuội bờ sông, Cây Liễu rủ (Willow) & Cây Bạch Dương (Birch_tree1/2)
        System.Random rnd = new System.Random(303);

        for (int i = 0; i < 28; i++)
        {
            float angle = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float r = 2.5f + (float)rnd.NextDouble() * 12.5f;
            float lx = 229f + Mathf.Cos(angle) * r;
            float lz = 148f + Mathf.Sin(angle) * r;
            if (terrain.SampleHeight(new Vector3(lx, 0, lz)) < WaterSurfaceHeight - 0.25f)
            {
                string lilyPath = (i % 2 == 0) ? (PolyPlants + "/SM_Plant_LilyPad_01.prefab") : (PolyPlants + "/SM_Plant_LilyPad_02.prefab");
                float s = 0.90f + (float)rnd.NextDouble() * 0.45f;
                SpawnSyntyPrefab(lilyPath, waterGroup, new Vector3(lx, WaterSurfaceHeight + 0.03f, lz),
                    (float)rnd.NextDouble() * 360f, new Vector3(s, s, s), false);
            }
        }

        string[] willowPrefabs = {
            PolyTrees + "/SM_Tree_Willow_01.prefab",
            PolyTrees + "/SM_Tree_Willow_02.prefab"
        };
        string[] reedPrefabs = {
            PolyPlants + "/SM_Plant_Reeds_01.prefab",
            PolyPlants + "/SM_Plant_Reeds_02.prefab",
            PolyPlants + "/SM_Plant_Reeds_03.prefab"
        };
        string[] shoreRocks = {
            PxltigerPrefabs + "/Rock1.prefab",
            PolyRocks + "/SM_Rock_River_01.prefab",
            PolyRocks + "/SM_Rock_Moss_01.prefab",
            PolyRocks + "/SM_Rock_Moss_02.prefab",
            PolyRocks + "/SM_Rock_Cluster_01.prefab"
        };

        // Đá cuội & Lau sậy dọc hai bờ sông
        for (float rz = 168f; rz <= 248f; rz += 5.0f)
        {
            if (Mathf.Abs(rz - 200f) < 7.0f) continue; // Chừa lối đi thông thoáng hai đầu Cầu Gỗ

            float riverCenterX = GetRiverCenterX(rz);
            float westWaterEdgeX = riverCenterX - 6.2f;
            float eastWaterEdgeX = riverCenterX + 6.2f;

            SpawnOnGround(reedPrefabs[(int)rz % reedPrefabs.Length], waterGroup, terrain,
                westWaterEdgeX, rz, (float)rnd.NextDouble() * 360f, new Vector3(1.15f, 1.15f, 1.15f), -0.05f, false);
            SpawnOnGround(reedPrefabs[((int)rz + 1) % reedPrefabs.Length], waterGroup, terrain,
                eastWaterEdgeX, rz + 1.8f, (float)rnd.NextDouble() * 360f, new Vector3(1.15f, 1.15f, 1.15f), -0.05f, false);

            float rockScale = 0.85f + (float)rnd.NextDouble() * 0.45f;
            SpawnOnGround(shoreRocks[(int)rz % shoreRocks.Length], waterGroup, terrain,
                westWaterEdgeX - 0.8f, rz + 2.2f, (float)rnd.NextDouble() * 360f, new Vector3(rockScale, rockScale, rockScale), -0.12f, true);
            SpawnOnGround(shoreRocks[((int)rz + 2) % shoreRocks.Length], waterGroup, terrain,
                eastWaterEdgeX + 0.8f, rz - 1.5f, (float)rnd.NextDouble() * 360f, new Vector3(rockScale, rockScale, rockScale), -0.12f, true);
        }

        // Lau sậy và đá tảng quanh bờ Hồ Lớn (tâm 229, 148)
        for (int i = 0; i < 22; i++)
        {
            float ang = i * (Mathf.PI * 2f / 22f);
            if (ang > 1.15f && ang < 1.98f) continue; // Chừa cửa sông phía Bắc hồ
            float lx = 229f + Mathf.Cos(ang) * 18.2f;
            float lz = 148f + Mathf.Sin(ang) * 17.0f;
            SpawnOnGround(reedPrefabs[i % reedPrefabs.Length], waterGroup, terrain,
                lx, lz, i * 45f, new Vector3(1.2f, 1.2f, 1.2f), -0.06f, false);
            if (i % 2 == 0)
            {
                float rs = 0.9f + (i % 3) * 0.2f;
                SpawnOnGround(shoreRocks[i % shoreRocks.Length], waterGroup, terrain,
                    lx + Mathf.Cos(ang) * 1.4f, lz + Mathf.Sin(ang) * 1.4f, i * 70f, new Vector3(rs, rs, rs), -0.15f, true);
            }
        }

        // Cây Liễu rủ (Willow) duyên dáng ven hồ và ven sông
        Vector2[] willowSpots = {
            new Vector2(211f, 172f), new Vector2(208f, 148f),
            new Vector2(251f, 156f), new Vector2(249f, 134f),
            new Vector2(222f, 125f), new Vector2(240f, 218f),
            new Vector2(217f, 224f), new Vector2(241f, 182f)
        };
        for (int i = 0; i < willowSpots.Length; i++)
        {
            float s = 1.10f + (i % 3) * 0.15f;
            SpawnOnGround(willowPrefabs[i % willowPrefabs.Length], waterGroup, terrain,
                willowSpots[i].x, willowSpots[i].y, i * 67f, new Vector3(s, s, s), -0.15f, true);
        }
    }

    private static void BuildGroundedRiverBridge(Transform parent, Terrain terrain)
    {
        string bridgePath = PolyProps + "/SM_Prop_Bridge_Curved_01.prefab";
        GameObject bridgeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(bridgePath);
        if (bridgeAsset == null) return;

        GameObject bridge = (GameObject)PrefabUtility.InstantiatePrefab(bridgeAsset);
        bridge.name = "Wooden_River_Bridge";
        bridge.transform.SetParent(parent);
        bridge.transform.position = Vector3.zero;
        bridge.transform.rotation = Quaternion.identity;
        bridge.transform.localScale = Vector3.one;

        Bounds rawBounds = ComputeWorldBounds(bridge);
        float rotY = (rawBounds.size.z > rawBounds.size.x) ? 90f : 0f;
        bridge.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        Bounds rotatedBounds = ComputeWorldBounds(bridge);
        float targetLengthX = 18.4f;
        float scaleX = (rotatedBounds.size.x > 0.1f) ? (targetLengthX / rotatedBounds.size.x) : 1.45f;
        bridge.transform.localScale = new Vector3(scaleX, 1.18f, scaleX);

        bridge.transform.position = new Vector3(228f, CampPlateauHeight, 200f);
        Bounds placedBounds = ComputeWorldBounds(bridge);
        float centerOffsetX = placedBounds.center.x - 228f;
        float centerOffsetZ = placedBounds.center.z - 200f;
        float bottomY = placedBounds.min.y;
        float targetBottomY = CampPlateauHeight - 0.45f; // Hai đầu cầu ăn sâu vững chãi vào hai bờ sông 8.0m

        bridge.transform.position += new Vector3(-centerOffsetX, targetBottomY - bottomY, -centerOffsetZ);
        EnsureSolidColliders(bridge);

        // Đặt 4 cụm đá mố cầu (kết hợp Rock1 của Pxltiger & SM_Rock_Cluster_02) và 4 trụ đuốc ở hai đầu cầu Đông & Tây
        SpawnOnGround(PxltigerPrefabs + "/Rock1.prefab", parent, terrain, 219.2f, 197.8f, 15f, new Vector3(0.95f, 0.85f, 0.95f), -0.15f, true);
        SpawnOnGround(PxltigerPrefabs + "/Rock1.prefab", parent, terrain, 219.2f, 202.2f, 195f, new Vector3(0.95f, 0.85f, 0.95f), -0.15f, true);
        SpawnOnGround(PolyRocks + "/SM_Rock_Cluster_02.prefab", parent, terrain, 236.8f, 198.0f, 85f, new Vector3(1.1f, 0.9f, 1.1f), -0.15f, true);
        SpawnOnGround(PolyRocks + "/SM_Rock_Cluster_02.prefab", parent, terrain, 236.8f, 202.0f, 265f, new Vector3(1.1f, 0.9f, 1.1f), -0.15f, true);

        SpawnTorchWithLight(parent, terrain, 218.8f, 197.8f);
        SpawnTorchWithLight(parent, terrain, 218.8f, 202.2f);
        SpawnTorchWithLight(parent, terrain, 237.2f, 197.8f);
        SpawnTorchWithLight(parent, terrain, 237.2f, 202.2f);
    }

    // ==================== 5. KHU CĂN CỨ SINH TỒN 3D (SURVIVAL HOMESTEAD & BASE CAMP) ====================

    private static void BuildSurvivalHomesteadAndCamp(Terrain terrain, Transform worldRoot)
    {
        Transform campGroup = new GameObject("Survival_Base_Camp").transform;
        campGroup.SetParent(worldRoot);

        // 1. HAI NGÔI NHÀ GỖ SINH TỒN LỚN
        BuildCompleteSurvivalLogCabin(campGroup, terrain, "Survival_LogHouse_Main", 195.5f, 211.5f, 90f, 1.42f);
        BuildCompleteSurvivalLogCabin(campGroup, terrain, "Survival_LogHouse_Workshop", 207.8f, 208.5f, 135f, 1.32f);

        // 2. LỀU GỖ TRÒN (Hut) & LỀU TRẠI DÃ CHIẾN (Primitive Shelter) ở phía Nam lửa trại
        SpawnAuthoredHut(campGroup, terrain, "Survival_Hut_South", 194.8f, 188.5f, 180f, 1.38f);
        SpawnAuthoredShelter(campGroup, terrain, "Survival_Shelter_SouthEast", 206.5f, 190.0f, 135f, 1.38f);

        // 3. BẾP LỬA TRẠI TRUNG TÂM tại (198, 200)
        SpawnAuthoredFirepit(campGroup, terrain, 198f, 200f, 1.35f);

        // Các khúc gỗ ngồi quanh bếp lửa trại & gốc cây kê đồ
        SpawnOnGround(PolyTrees + "/SM_Tree_Log_01.prefab", campGroup, terrain, 194.8f, 200.2f, 90f, new Vector3(0.85f, 0.85f, 0.85f), -0.06f, true);
        SpawnOnGround(PolyTrees + "/SM_Tree_Log_01.prefab", campGroup, terrain, 198.2f, 196.6f, 0f, new Vector3(0.85f, 0.85f, 0.85f), -0.06f, true);
        SpawnOnGround(PolyTrees + "/SM_Tree_Stump_01.prefab", campGroup, terrain, 201.2f, 201.5f, 45f, Vector3.one, -0.05f, true);

        // 4. KHU CHẾ TÁC VŨ KHÍ (Workshop), GIẢ KIM (Alchemy), LÒ NUNG ĐÁ (Furnace)
        SpawnAuthoredWorkshopStation(campGroup, terrain, 189.8f, 206.8f, 145f, 1.25f);
        SpawnAuthoredAlchemyStation(campGroup, terrain, 201.5f, 209.2f, 180f, 1.25f);
        SpawnAuthoredFurnace(campGroup, terrain, "Stone_Furnace_East", 212.5f, 205.0f, -90f, 1.35f);
        SpawnAuthoredFurnace(campGroup, terrain, "Stone_Furnace_West", 189.5f, 192.5f, 45f, 1.25f);

        // Rương gỗ, Rương đá, Thùng hàng & Tổ ong mật
        SpawnAuthoredChestWood(campGroup, terrain, 192.0f, 209.5f, 0f, 1.3f);
        SpawnAuthoredChestStone(campGroup, terrain, 204.2f, 207.5f, 45f, 1.3f);
        SpawnAuthoredCrate(campGroup, terrain, 210.5f, 203.5f, 25f, 1.2f);
        SpawnAuthoredCrate(campGroup, terrain, 211.5f, 204.2f, 70f, 1.0f);
        SpawnAuthoredBeeHive(campGroup, terrain, 188.0f, 188.5f, 30f, 1.2f);

        // Cờ hiệu Căn Cứ Sinh Tồn (Flag) ở cổng Tây và cổng Đông ra cầu gỗ
        SpawnAuthoredFlag(campGroup, terrain, 185.0f, 205.2f, 0f, 1.30f);
        SpawnAuthoredFlag(campGroup, terrain, 214.5f, 202.5f, -90f, 1.30f);

        // 5. TƯỜNG RÀO GỖ PHÒNG THỦ BAO QUANH CĂN CỨ (Wall_wood)
        Vector3[] woodWalls = {
            new Vector3(188.5f, 216.8f, 0f), new Vector3(192.2f, 217.4f, 0f),
            new Vector3(201.0f, 217.4f, 0f), new Vector3(204.8f, 216.8f, 15f),
            new Vector3(188.5f, 183.5f, 180f), new Vector3(192.2f, 182.8f, 180f),
            new Vector3(201.0f, 182.8f, 180f), new Vector3(204.8f, 183.5f, 165f)
        };
        foreach (var ww in woodWalls)
        {
            SpawnAuthoredWallWood(campGroup, terrain, ww.x, ww.y, ww.z, 1.2f);
        }

        // Hàng rào gỗ thấp (SM_Prop_Fence_01) dẫn dọc lối mòn từ căn cứ ra Cầu Gỗ
        SpawnOnGround(PolyProps + "/SM_Prop_Fence_01.prefab", campGroup, terrain, 213.5f, 197.2f, 0f, Vector3.one, -0.05f, true);
        SpawnOnGround(PolyProps + "/SM_Prop_Fence_02.prefab", campGroup, terrain, 216.5f, 197.2f, 0f, Vector3.one, -0.05f, true);
        SpawnOnGround(PolyProps + "/SM_Prop_Fence_01.prefab", campGroup, terrain, 213.5f, 202.8f, 0f, Vector3.one, -0.05f, true);
        SpawnOnGround(PolyProps + "/SM_Prop_Fence_02.prefab", campGroup, terrain, 216.5f, 202.8f, 0f, Vector3.one, -0.05f, true);

        // Đuốc thắp sáng quanh căn cứ
        SpawnTorchWithLight(campGroup, terrain, 192.5f, 207.8f);
        SpawnTorchWithLight(campGroup, terrain, 204.5f, 205.8f);
        SpawnTorchWithLight(campGroup, terrain, 192.5f, 191.5f);
        SpawnTorchWithLight(campGroup, terrain, 204.5f, 193.5f);
        SpawnTorchWithLight(campGroup, terrain, 185.2f, 196.2f);
        SpawnTorchWithLight(campGroup, terrain, 185.2f, 203.8f);

        // Bạn đồng hành tại căn cứ: Chó săn thông minh (Dog.prefab) & Ngựa hiền lành (Horse.prefab)
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Dog.prefab", campGroup, terrain,
            195.5f, 198.5f, 65f, 1.15f, 1.8f, 4.5f, 7.5f, false, true, 5.0f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Horse.prefab", campGroup, terrain,
            187.8f, 205.5f, 135f, 1.15f, 1.5f, 4.5f, 11.0f, true, false, 6.0f, -0.02f);
    }

    private static GameObject BuildCompleteSurvivalLogCabin(Transform parent, Terrain terrain,
        string cabinName, float wx, float wz, float rotY, float uniformScale = 1.4f)
    {
        float wy = terrain.SampleHeight(new Vector3(wx, 0f, wz));
        GameObject cabinRoot = new GameObject(cabinName);
        cabinRoot.transform.SetParent(parent);
        cabinRoot.transform.position = new Vector3(wx, wy - 0.04f, wz);
        cabinRoot.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        SpawnAuthoredFBXPart(cabinRoot.transform, "HouseDeck_Mesh", "Constructions/house1.fbx",
            new Vector3(0.0423f, 0.91f, -0.0455f),
            new Quaternion(0f, 0.7072302f, 0.7069834f, 0f),
            new Vector3(16.71606f, 16.71606f, 56.43418f),
            new string[] { "WallWood", "WallWood" });

        SpawnAuthoredFBXPart(cabinRoot.transform, "HouseTop_Mesh", "Constructions/house2.fbx",
            new Vector3(0.29f, 1.92f, -0.0455f),
            new Quaternion(0f, 0.7072302f, 0.7069834f, 0f),
            new Vector3(21.812965f, 21.812965f, 73.64156f),
            new string[] { "WallWood", "WallWoodEdge", "WallWood" });

        SpawnAuthoredFBXPart(cabinRoot.transform, "RoofGrid_Mesh", "Constructions/roof.fbx",
            new Vector3(0.29f, 2.68f, -0.0455f),
            Quaternion.identity,
            new Vector3(21.8f, 21.8f, 154f),
            new string[] { "WallWood", "WallWoodEdge" });

        GameObject stairsHolder = new GameObject("Cabin_Stairs");
        stairsHolder.transform.SetParent(cabinRoot.transform, false);
        stairsHolder.transform.localPosition = new Vector3(0.05f, -0.75f, -1.85f);
        stairsHolder.transform.localRotation = Quaternion.identity;
        SpawnAuthoredFBXPart(stairsHolder.transform, "Stairs_Mesh", "Constructions/stairs.fbx",
            new Vector3(-0.0256f, 1.4f, 0.03f),
            new Quaternion(-0.358368f, 0f, 0f, 0.93358046f),
            new Vector3(30.619707f, 30.619707f, 241.45111f),
            new string[] { "WallWood", "WallWoodEdge" });

        GameObject chimneyRock = AssetDatabase.LoadAssetAtPath<GameObject>(PolyRocks + "/SM_Rock_Stack_01.prefab");
        if (chimneyRock != null)
        {
            GameObject ch = (GameObject)PrefabUtility.InstantiatePrefab(chimneyRock);
            ch.transform.SetParent(cabinRoot.transform, false);
            ch.transform.localPosition = new Vector3(-1.35f, 1.75f, 0.65f);
            ch.transform.localScale = new Vector3(0.45f, 1.18f, 0.45f);
        }

        GameObject interiorLightObj = new GameObject("Cabin_WarmLight");
        interiorLightObj.transform.SetParent(cabinRoot.transform, false);
        interiorLightObj.transform.localPosition = new Vector3(0.29f, 1.85f, 0f);
        Light il = interiorLightObj.AddComponent<Light>();
        il.type = LightType.Point;
        il.color = new Color(1.0f, 0.72f, 0.32f);
        il.intensity = 2.2f;
        il.range = 8.0f;

        cabinRoot.transform.localScale = Vector3.one * uniformScale;

        SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/Smoke_Light_FX.prefab", cabinRoot.transform,
            cabinRoot.transform.TransformPoint(new Vector3(-1.35f, 3.8f, 0.65f)), 0f, Vector3.one, false);

        EnsureSolidColliders(cabinRoot);
        return cabinRoot;
    }

    private static GameObject SpawnAuthoredHut(Transform parent, Terrain terrain, string name, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, name, wx, wz, rotY, scale, -0.03f);
        SpawnAuthoredFBXPart(root.transform, "Hut_Mesh", "Constructions/Hut.fbx",
            new Vector3(-0.0604f, 1.69f, 0.33f),
            new Quaternion(-0.1106159f, 0.69840115f, 0.69840115f, 0.1106159f),
            new Vector3(78.58607f, 78.58603f, 78.58603f),
            new string[] { "PalmTrunk", "Hut" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredShelter(Transform parent, Terrain terrain, string name, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, name, wx, wz, rotY, scale, -0.03f);
        SpawnAuthoredFBXPart(root.transform, "Shelter_Mesh", "Constructions/Primitive_Shelter.fbx",
            new Vector3(0f, 1.02f, 0f),
            new Quaternion(0f, 0.7071068f, 0f, 0.7071068f),
            new Vector3(0.47573f, 0.47573f, 0.47573f),
            new string[] { "PalmTrunk", "Palm" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredFirepit(Transform parent, Terrain terrain, float wx, float wz, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, "Central_Camp_Firepit", wx, wz, 0f, scale, -0.02f);
        SpawnAuthoredFBXPart(root.transform, "Firepit_Base", "Constructions/Firepit_base.fbx",
            new Vector3(0f, 0.1258f, 0f),
            Quaternion.identity,
            new Vector3(0.18775f, 0.11518f, 0.12442f),
            new string[] { "Stone", "StoneDarker" });
        SpawnAuthoredFBXPart(root.transform, "Firepit_Wood", "Constructions/Firepit_wood.fbx",
            new Vector3(-0.0605f, 0.593f, 0.0335f),
            new Quaternion(0.02826f, -0.00611f, -0.3015f, 0.95303f),
            new Vector3(0.62602f, 0.62602f, 0.62602f),
            new string[] { "Wood" });

        float wy = terrain.SampleHeight(new Vector3(wx, 0f, wz));
        SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/Fire_FX.prefab", root.transform,
            new Vector3(wx, wy + 0.15f, wz), 0f, new Vector3(1.2f, 1.2f, 1.2f), false);
        SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/Smoke_Light_FX.prefab", root.transform,
            new Vector3(wx, wy + 0.55f, wz), 0f, Vector3.one, false);

        GameObject lObj = new GameObject("Campfire_WarmLight");
        lObj.transform.SetParent(root.transform, false);
        lObj.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        Light l = lObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1.0f, 0.65f, 0.25f);
        l.intensity = 2.4f;
        l.range = 12f;

        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredWorkshopStation(Transform parent, Terrain terrain, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, "Station_Workshop", wx, wz, rotY, scale, -0.02f);
        SpawnAuthoredFBXPart(root.transform, "Workshop_Table", "Constructions/Table.fbx",
            new Vector3(0f, 0.896f, 0f),
            new Quaternion(-0.7071068f, 0f, 0f, 0.7071067f),
            new Vector3(7.24465f, 11.11164f, 46.99548f),
            new string[] { "WallWood" });
        SpawnAuthoredFBXPart(root.transform, "Workshop_Anvil", "Constructions/anvil.fbx",
            new Vector3(-0.794f, 1.0f, -0.086f),
            new Quaternion(-0.7071068f, 0f, 0f, 0.7071067f),
            new Vector3(71.4218f, 71.4218f, 71.4218f),
            new string[] { "ToolMetal", "ToolMetalEdge" });
        SpawnAuthoredFBXPart(root.transform, "Workshop_Saw", "Constructions/saw.fbx",
            new Vector3(0.057f, 0.968f, 0.496f),
            Quaternion.identity,
            new Vector3(41.378f, 41.378f, 41.378f),
            new string[] { "ToolMetalEdge" });
        SpawnAuthoredFBXPart(root.transform, "Workshop_Hammer", "Tools/Hammer.fbx",
            new Vector3(0.584f, 1.0f, -0.418f),
            new Quaternion(0f, 0.21007f, 0f, 0.97769f),
            new Vector3(14.0436f, 4.6979f, 4.6979f),
            new string[] { "ToolMetalEdge", "ToolWood" });
        SpawnAuthoredFBXPart(root.transform, "Workshop_WaterTrough", "Constructions/water-trough.fbx",
            new Vector3(-0.003f, 0.163f, -0.42f),
            new Quaternion(-0.7071068f, 0f, 0f, 0.7071067f),
            new Vector3(61.1954f, 17.2399f, 17.2399f),
            new string[] { "WallWood", "Water" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredAlchemyStation(Transform parent, Terrain terrain, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, "Station_Alchemy", wx, wz, rotY, scale, -0.02f);
        SpawnAuthoredFBXPart(root.transform, "Alchemy_Table", "Constructions/Table-green.fbx",
            new Vector3(0f, 0.109f, 0.035f),
            Quaternion.identity,
            new Vector3(0.29486f, 0.29486f, 0.29486f),
            new string[] { "WallWood", "FabricGreen" });
        SpawnAuthoredFBXPart(root.transform, "Alchemy_Cauldron", "Constructions/cauldron.fbx",
            new Vector3(-0.405f, 0.37f, -0.679f),
            Quaternion.identity,
            new Vector3(37.156f, 37.156f, 37.156f),
            new string[] { "StoneDark", "FabricGreen", "StoneDark" });
        SpawnAuthoredFBXPart(root.transform, "Alchemy_Book", "Constructions/book.fbx",
            new Vector3(0.28f, 0.94f, -0.03f),
            new Quaternion(-0.7071068f, 0f, 0f, 0.7071067f),
            new Vector3(13.97f, 13.97f, 13.97f),
            new string[] { "Paper", "ChestDark", "Stone" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredFurnace(Transform parent, Terrain terrain, string name, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, name, wx, wz, rotY, scale, -0.03f);
        SpawnAuthoredFBXPart(root.transform, "Furnace_Mesh", "Constructions/furnace.fbx",
            new Vector3(-0.045f, 0f, 0f),
            new Quaternion(0f, 0.7071068f, 0f, 0.7071068f),
            new Vector3(0.78597f, 0.78597f, 0.78597f),
            new string[] { "StoneDarker", "StoneDark", "StoneDarker", "StoneDarker" });
        float wy = terrain.SampleHeight(new Vector3(wx, 0f, wz));
        SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/Fire_Small_FX.prefab", root.transform,
            new Vector3(wx, wy + 0.45f, wz), 0f, Vector3.one, false);
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredChestWood(Transform parent, Terrain terrain, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, "Chest_Wood", wx, wz, rotY, scale, -0.02f);
        SpawnAuthoredFBXPart(root.transform, "ChestWood_Mesh", "Constructions/Chest.fbx",
            Vector3.zero, new Quaternion(0f, 1f, 0f, 0f), Vector3.one,
            new string[] { "ChestLight", "ChestDark", "ChestLight", "Black", "ChestDark" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredChestStone(Transform parent, Terrain terrain, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, "Chest_Stone", wx, wz, rotY, scale, -0.02f);
        SpawnAuthoredFBXPart(root.transform, "ChestStone_Mesh", "Constructions/Chest stone.fbx",
            Vector3.zero, new Quaternion(0f, 1f, 0f, 0f), Vector3.one,
            new string[] { "ChestStone", "ChestStone2", "ChestStone2", "Black", "ChestStone" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredCrate(Transform parent, Terrain terrain, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, "Wooden_Crate", wx, wz, rotY, scale, -0.02f);
        SpawnAuthoredFBXPart(root.transform, "Crate_Mesh", "Other/WoodenCrate.fbx",
            new Vector3(0f, 0.017f, 0f), new Quaternion(-0.7071068f, 0f, 0f, 0.7071068f),
            new Vector3(51.3516f, 51.3516f, 51.3516f),
            new string[] { "Wood", "WoodDark" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredBeeHive(Transform parent, Terrain terrain, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, "BeeHive", wx, wz, rotY, scale, -0.02f);
        SpawnAuthoredFBXPart(root.transform, "BeeHive_Mesh", "Environment/Beehive.fbx",
            new Vector3(0f, 0.331f, 0.08f), new Quaternion(-0.22519f, -0.41632f, 0.10722f, 0.87434f),
            new Vector3(36.809f, 36.809f, 36.809f),
            new string[] { "Hive", "Hive", "Hive" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredFlag(Transform parent, Terrain terrain, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, "Survival_Flag", wx, wz, rotY, scale, -0.03f);
        SpawnAuthoredFBXPart(root.transform, "Flag_Mesh", "Constructions/flag.fbx",
            new Vector3(0f, -0.013f, 0f), new Quaternion(0f, 1f, 0f, 0f),
            new Vector3(0.24365f, 0.24365f, 0.24365f),
            new string[] { "ToolWood", "ToolWood", "Flag" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredWallWood(Transform parent, Terrain terrain, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, "Palisade_WallWood", wx, wz, rotY, scale, -0.04f);
        SpawnAuthoredFBXPart(root.transform, "WallWood_Mesh", "Constructions/Wall_wood.fbx",
            new Vector3(0f, -0.006f, 0f), new Quaternion(-0.7071068f, 0f, 0f, 0.7071067f),
            new Vector3(28.94746f, 28.94745f, 14.47373f),
            new string[] { "WallWood", "WallWoodEdge" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject SpawnAuthoredStatue(Transform parent, Terrain terrain, float wx, float wz, float rotY, float scale)
    {
        GameObject root = CreateGroundedRoot(parent, terrain, "Ancient_Survival_Statue", wx, wz, rotY, scale, -0.04f);
        SpawnAuthoredFBXPart(root.transform, "Statue_Mesh", "Constructions/Statue_SurvivalEngine.fbx",
            new Vector3(0f, 1.87f, 0f), Quaternion.identity, Vector3.one,
            new string[] { "StoneDark" });
        EnsureSolidColliders(root);
        return root;
    }

    private static GameObject CreateGroundedRoot(Transform parent, Terrain terrain, string name, float wx, float wz, float rotY, float scale, float yOffset)
    {
        float wy = (terrain != null) ? (terrain.SampleHeight(new Vector3(wx, 0f, wz)) + yOffset) : yOffset;
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(wx, wy, wz);
        root.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        root.transform.localScale = Vector3.one * scale;
        return root;
    }

    private static GameObject SpawnAuthoredFBXPart(Transform parent, string partName, string fbxRelPath,
        Vector3 localPos, Quaternion localRot, Vector3 localScale, string[] matNames)
    {
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SurvivalModels + "/" + fbxRelPath);
        if (fbxAsset == null) return null;

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        inst.name = partName;
        inst.transform.SetParent(parent, false);
        inst.transform.localPosition = localPos;
        inst.transform.localRotation = localRot;
        inst.transform.localScale = localScale;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Renderer[] renderers = inst.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (matNames != null && matNames.Length > 0)
            {
                Material[] mats = new Material[matNames.Length];
                for (int i = 0; i < matNames.Length; i++)
                {
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(SurvivalMaterials + "/" + matNames[i] + ".mat");
                    if (m != null && urpLit != null)
                    {
                        if (m.shader == null || m.shader.name.Contains("Error"))
                        {
                            m.shader = urpLit;
                        }
                        m.enableInstancing = true;
                        EditorUtility.SetDirty(m);
                    }
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }
        return inst;
    }

    private static void SpawnTorchWithLight(Transform parent, Terrain terrain, float wx, float wz)
    {
        GameObject torch = SpawnOnGround(PolyProps + "/SM_Prop_TorchStick_01.prefab", parent, terrain,
            wx, wz, 0f, new Vector3(1.2f, 1.2f, 1.2f), -0.05f, false);
        if (torch != null)
        {
            float ty = terrain.SampleHeight(new Vector3(wx, 0f, wz));
            SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/Fire_Small_FX.prefab", torch.transform,
                new Vector3(wx, ty + 1.35f, wz), 0f, Vector3.one, false);

            GameObject lObj = new GameObject("Torch_PointLight");
            lObj.transform.SetParent(torch.transform);
            lObj.transform.position = new Vector3(wx, ty + 1.55f, wz);
            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.68f, 0.28f);
            l.intensity = 1.4f;
            l.range = 8f;
        }
    }

    // ==================== 6. RỪNG ĐA TẦNG (TIỀN CẢNH - TRUNG CẢNH - HẬU CẢNH), ĐÁ VÁCH NÚI & ĐỘNG VẬT AI ====================

    private static void BuildForestEcosystemAndWildlife(Terrain terrain, Transform worldRoot)
    {
        Transform landmarksGroup = new GameObject("Landmarks_Ruins_And_Cave").transform;
        landmarksGroup.SetParent(worldRoot);

        // 1. HANG ĐỘNG ĐÁ LỚN (Rocky Cave Entrance) ở vách núi phía Tây Bắc (wx = 165, wz = 246)
        // Gia cố thêm các khối đá phủ rêu lớn từ gói TerrainSampleAssets (Rock_Overgrown_B, Rock_Overgrown_D, Rock_C_01)
        SpawnOnGround(PolyRocks + "/SM_Rock_CaveEntrance_02.prefab", landmarksGroup, terrain,
            165f, 246f, 145f, new Vector3(1.15f, 1.15f, 1.15f), -0.45f, true);
        SpawnOnGround(TerrainPackRocks + "/Rock_Overgrown_B.prefab", landmarksGroup, terrain,
            158f, 248f, 120f, new Vector3(1.25f, 1.25f, 1.25f), -0.35f, true);
        SpawnOnGround(TerrainPackRocks + "/Rock_Overgrown_D.prefab", landmarksGroup, terrain,
            172f, 249f, 165f, new Vector3(1.25f, 1.25f, 1.25f), -0.35f, true);
        SpawnTorchWithLight(landmarksGroup, terrain, 161f, 241f);
        SpawnTorchWithLight(landmarksGroup, terrain, 169f, 241f);

        // 2. KHU TÀN TÍCH ĐÁ CỔ (Ancient Stone Ruins) bên kia Cầu Gỗ phía Đông (wx = 252, wz = 204)
        SpawnOnGround(PolyRocks + "/SM_Rock_Ruins_Wall_01.prefab", landmarksGroup, terrain, 254f, 206f, -30f, Vector3.one, -0.15f, true);
        SpawnOnGround(PolyRocks + "/SM_Rock_Ruins_Wall_02.prefab", landmarksGroup, terrain, 258f, 201f, 60f, Vector3.one, -0.15f, true);
        SpawnOnGround(PolyRocks + "/SM_Rock_Ruins_Block_01.prefab", landmarksGroup, terrain, 251f, 196f, 15f, Vector3.one, -0.1f, true);
        SpawnOnGround(PolyRocks + "/SM_Rock_Stack_01.prefab", landmarksGroup, terrain, 248f, 208f, 0f, new Vector3(1.1f, 1.1f, 1.1f), -0.1f, true);
        SpawnOnGround(TerrainPackRocks + "/Rock_Overgrown_B.prefab", landmarksGroup, terrain, 257f, 208f, 45f, new Vector3(1.1f, 1.1f, 1.1f), -0.25f, true);
        SpawnAuthoredStatue(landmarksGroup, terrain, 253f, 202f, 250f, 1.25f);

        // 3. HẬU CẢNH (BACKGROUND): VÒNG ĐÁ VÁCH NÚI BAO QUANH THUNG LŨNG (Kết hợp TerrainSampleAssets Rocks & PolygonNature Big Rocks)
        string[] bigRocks = {
            TerrainPackRocks + "/Rock_C_01.prefab",
            TerrainPackRocks + "/Rock_Overgrown_B.prefab",
            TerrainPackRocks + "/Rock_Overgrown_D.prefab",
            PolyRocks + "/SM_Rock_Big_01.prefab",
            PolyRocks + "/SM_Rock_Big_02.prefab",
            PolyRocks + "/SM_Rock_Big_03.prefab",
            PolyRocks + "/SM_Rock_Cluster_Large_01.prefab",
            PolyRocks + "/SM_Rock_Cluster_Large_02.prefab"
        };
        System.Random rnd = new System.Random(2312690);
        for (int i = 0; i < 28; i++)
        {
            float angle = i * (Mathf.PI * 2f / 28f);
            float dist = 76f + (float)rnd.NextDouble() * 34f;
            float rx = 205f + Mathf.Cos(angle) * dist;
            float rz = 195f + Mathf.Sin(angle) * dist;
            float s = 1.05f + (float)rnd.NextDouble() * 0.50f;
            SpawnOnGround(bigRocks[i % bigRocks.Length], landmarksGroup, terrain,
                rx, rz, (float)rnd.NextDouble() * 360f, new Vector3(s, s, s), -0.45f, true);
        }

        // 4. ĐỘNG VẬT HOANG DÃ 3D (Dùng Prefab trong Assets/SurvivalEngine/Prefabs/Animals/ + AnimalAI)
        Transform wildlifeGroup = new GameObject("Wildlife_Animals_3D").transform;
        wildlifeGroup.SetParent(worldRoot);

        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Deer.prefab", wildlifeGroup, terrain, 243f, 205f, 215f, 1.15f, 1.6f, 5.2f, 16f, true, false, 8.5f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Deer.prefab", wildlifeGroup, terrain, 246f, 194f, 285f, 1.05f, 1.6f, 5.2f, 16f, true, false, 8.5f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Deer.prefab", wildlifeGroup, terrain, 174f, 182f, 45f, 1.15f, 1.6f, 5.2f, 16f, true, false, 8.5f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Deer.prefab", wildlifeGroup, terrain, 212f, 176f, 320f, 1.10f, 1.6f, 5.2f, 16f, true, false, 8.5f, -0.01f);

        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Rabbit.prefab", wildlifeGroup, terrain, 189f, 194f, 110f, 1.1f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Rabbit.prefab", wildlifeGroup, terrain, 204f, 186f, 240f, 1.0f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Rabbit.prefab", wildlifeGroup, terrain, 240f, 196f, 160f, 1.1f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Squirrel.prefab", wildlifeGroup, terrain, 184f, 208f, 80f, 1.1f, 1.3f, 4.2f, 8f, true, false, 5.5f, -0.01f);

        // 4b. THỎ TRẮNG HOANG DÃ (White Rabbit - Asset Store ID 138709) kèm AI gặm cỏ, chạy trốn người chơi
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 182f, 194f, 75f, 1.15f, 1.4f, 4.6f, 9f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 187f, 206f, 130f, 1.1f, 1.4f, 4.6f, 9f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 214f, 185f, 45f, 1.15f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 218f, 212f, 220f, 1.1f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 210f, 164f, 310f, 1.1f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 245f, 168f, 180f, 1.15f, 1.4f, 4.6f, 11f, true, false, 6.5f, -0.01f);
        SpawnAnimalWithAI(WhiteRabbitPrefabs + "/WhiteRabbit.prefab", wildlifeGroup, terrain, 244f, 198f, 90f, 1.1f, 1.4f, 4.6f, 10f, true, false, 6.5f, -0.01f);

        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Boar.prefab", wildlifeGroup, terrain, 172f, 218f, 130f, 1.1f, 1.5f, 4.4f, 14f, true, false, 7.0f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Boar.prefab", wildlifeGroup, terrain, 175f, 221f, 155f, 1.0f, 1.5f, 4.4f, 14f, true, false, 7.0f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Bison.prefab", wildlifeGroup, terrain, 258f, 178f, 260f, 1.1f, 1.2f, 3.8f, 15f, false, false, 8.0f, -0.01f);

        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Wolf.prefab", wildlifeGroup, terrain, 172f, 236f, 160f, 1.1f, 1.8f, 5.0f, 16f, false, false, 9.0f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Wolf.prefab", wildlifeGroup, terrain, 176f, 238f, 185f, 1.05f, 1.8f, 5.0f, 16f, false, false, 9.0f, -0.01f);
        SpawnAnimalWithAI(SurvivalPrefabs + "/Animals/Bear.prefab", wildlifeGroup, terrain, 164f, 239f, 145f, 1.15f, 1.4f, 4.2f, 12f, false, false, 9.5f, -0.01f);

        // 5. HỆ SINH THÁI RỪNG CÂY, ĐÁ TẢNG & THẢM THỰC VẬT 3D (TÍCH HỢP FANTASY LANDSCAPE 103573 & TERRAIN PACK 145808)
        Transform treesGroup = new GameObject("Polygon_Forest_Trees").transform;
        treesGroup.SetParent(worldRoot);
        Transform birchGroup = new GameObject("FantasyLandscape_Birch_And_Props").transform;
        birchGroup.SetParent(worldRoot);
        Transform plantsGroup = new GameObject("Polygon_Lush_Vegetation").transform;
        plantsGroup.SetParent(worldRoot);

        // 5.1. Cụm cây Bạch Dương (Birch_tree1 & Birch_tree2 từ Fantasy Landscape 103573) tạo điểm nhấn Tiền cảnh & Trung cảnh
        string[] birchTrees = {
            PxltigerBirchPrefabs + "/Birch_tree1.prefab",
            PxltigerBirchPrefabs + "/Birch_tree2.prefab"
        };
        Vector2[] scenicBirchSpots = {
            // Tiền cảnh hai bên lối vào Căn Cứ Sinh Tồn (quanh Player Spawn 183, 200)
            new Vector2(177f, 193f), new Vector2(176f, 207f), new Vector2(182f, 211f), new Vector2(183f, 189f),
            // Trung cảnh dọc bờ sông phía Tây & phía Đông Cầu Gỗ
            new Vector2(215f, 211f), new Vector2(216f, 188f), new Vector2(241f, 209f), new Vector2(242f, 191f),
            // Ven Hồ Lớn phía Nam & thảo nguyên phía Đông
            new Vector2(212f, 160f), new Vector2(204f, 154f), new Vector2(246f, 166f), new Vector2(254f, 188f),
            // Dọc lối mòn lên Hang động phía Bắc
            new Vector2(186f, 224f), new Vector2(178f, 231f), new Vector2(208f, 225f), new Vector2(220f, 232f)
        };
        for (int i = 0; i < scenicBirchSpots.Length; i++)
        {
            float bs = 0.85f + (i % 3) * 0.12f;
            SpawnOnGround(birchTrees[i % birchTrees.Length], birchGroup, terrain,
                scenicBirchSpots[i].x, scenicBirchSpots[i].y, i * 47f, new Vector3(bs, bs, bs), -0.12f, true);
        }

        // 5.2. Hậu cảnh & Trung cảnh: Cây Thông (Pine) và cây Rừng lá tán lớn (Tree_Base) + Bạch Dương xen kẽ
        string[] forestTrees = {
            PolyTrees + "/SM_Tree_Pine_01.prefab",
            PolyTrees + "/SM_Tree_Pine_02.prefab",
            PolyTrees + "/SM_Tree_Pine_03.prefab",
            PolyTrees + "/SM_Tree_Pine_04.prefab",
            PolyTrees + "/SM_Tree_Pine_05.prefab",
            PolyTrees + "/SM_Tree_Base_01.prefab",
            PolyTrees + "/SM_Tree_Base_02.prefab",
            PolyTrees + "/SM_Tree_Base_03.prefab",
            PxltigerBirchPrefabs + "/Birch_tree1.prefab",
            PxltigerBirchPrefabs + "/Birch_tree2.prefab"
        };

        string[] bushesAndFerns = {
            PolyPlants + "/SM_Plant_Bush_01.prefab",
            PolyPlants + "/SM_Plant_Bush_02.prefab",
            PolyPlants + "/SM_Plant_Bush_03.prefab",
            PolyPlants + "/SM_Plant_Fern_01.prefab",
            PolyPlants + "/SM_Plant_Fern_02.prefab",
            PolyPlants + "/SM_Plant_Fern_03.prefab"
        };

        // Tích hợp Flower1, Flower2, Grass1, Mushroom1 của gói Fantasy Landscape (103573) cùng thảm hoa cỏ 3D
        string[] flowersAndGrass3D = {
            PxltigerPrefabs + "/Flower1.prefab",
            PxltigerPrefabs + "/Flower2.prefab",
            PxltigerPrefabs + "/Grass1.prefab",
            PxltigerPrefabs + "/Mushroom1.prefab",
            PolyPlants + "/SM_Plant_Flowers_01.prefab",
            PolyPlants + "/SM_Plant_Flowers_02.prefab",
            PolyPlants + "/SM_Plant_PurpleFlower_01.prefab",
            PolyPlants + "/SM_Plant_PurpleFlower_02.prefab",
            PolyPlants + "/SM_Plant_Grass_01.prefab",
            PolyPlants + "/SM_Plant_Grass_02.prefab"
        };

        // Tích hợp Rock1 (Pxltiger 103573) và Rock_Overgrown_B/D (TerrainSampleAssets 145808)
        string[] mediumRocks = {
            PxltigerPrefabs + "/Rock1.prefab",
            TerrainPackRocks + "/Rock_Overgrown_B.prefab",
            TerrainPackRocks + "/Rock_Overgrown_D.prefab",
            PolyRocks + "/SM_Rock_Med_01.prefab",
            PolyRocks + "/SM_Rock_Med_02.prefab",
            PolyRocks + "/SM_Rock_Cluster_01.prefab",
            PolyRocks + "/SM_Rock_Moss_01.prefab"
        };

        bool IsClearForTree(float wx, float wz)
        {
            float h = terrain.SampleHeight(new Vector3(wx, 0f, wz));
            if (h < WaterSurfaceHeight + 1.0f) return false;
            if (Vector2.Distance(new Vector2(wx, wz), new Vector2(198f, 200f)) < 18.5f) return false;
            if (wx >= 174f && wx <= 248f && Mathf.Abs(wz - 200f) < 7.0f) return false;
            if (wx >= 214f && wx <= 232f && wz >= 164f && wz <= 196f) return false;
            return true;
        }

        bool IsClearForPlant(float wx, float wz)
        {
            float h = terrain.SampleHeight(new Vector3(wx, 0f, wz));
            if (h < WaterSurfaceHeight + 0.45f) return false;
            if (Vector2.Distance(new Vector2(wx, wz), new Vector2(198f, 200f)) < 7.5f) return false;
            if (wx >= 216f && wx <= 240f && Mathf.Abs(wz - 200f) < 4.5f) return false;
            return true;
        }

        // Đặt hàng cây thông & cổ thụ cao lớn tạo khung cảnh rừng sâu bao bọc phía sau Căn Cứ và bên kia Sông
        Vector2[] scenicFramingTrees = {
            new Vector2(182f, 219f), new Vector2(190f, 222f), new Vector2(202f, 223f), new Vector2(214f, 220f),
            new Vector2(182f, 181f), new Vector2(191f, 178f), new Vector2(203f, 177f), new Vector2(211f, 180f),
            new Vector2(244f, 214f), new Vector2(252f, 216f), new Vector2(260f, 208f), new Vector2(262f, 192f),
            new Vector2(246f, 184f), new Vector2(256f, 172f), new Vector2(264f, 160f), new Vector2(206f, 164f),
            new Vector2(248f, 226f), new Vector2(238f, 232f), new Vector2(218f, 234f), new Vector2(174f, 212f)
        };
        for (int i = 0; i < scenicFramingTrees.Length; i++)
        {
            float s = 1.20f + (i % 3) * 0.16f;
            SpawnOnGround(forestTrees[i % 8], treesGroup, terrain,
                scenicFramingTrees[i].x, scenicFramingTrees[i].y, i * 53f, new Vector3(s, s, s), -0.15f, true);
        }

        // Trồng 480 cây rừng đa tầng bao quanh thung lũng (vùng cao ưu tiên Thông, vùng thung lũng xen kẽ Bạch Dương)
        for (int i = 0; i < 480; i++)
        {
            float tx = 65f + (float)rnd.NextDouble() * 270f;
            float tz = 65f + (float)rnd.NextDouble() * 270f;
            if (!IsClearForTree(tx, tz)) continue;

            float elev = terrain.SampleHeight(new Vector3(tx, 0f, tz));
            string treePrefab = (elev < 10.2f && i % 4 == 0)
                ? birchTrees[i % birchTrees.Length]
                : forestTrees[rnd.Next(8)];
            float s = (treePrefab.Contains("Birch"))
                ? (0.80f + (float)rnd.NextDouble() * 0.35f)
                : (1.05f + (float)rnd.NextDouble() * 0.60f);

            SpawnOnGround(treePrefab, treesGroup, terrain,
                tx, tz, (float)rnd.NextDouble() * 360f, new Vector3(s, s, s), -0.15f, true);
        }

        // Trồng 190 bụi cây & dương xỉ rừng (tắt Collider để tối ưu Physics)
        for (int i = 0; i < 190; i++)
        {
            float bx = 115f + (float)rnd.NextDouble() * 180f;
            float bz = 115f + (float)rnd.NextDouble() * 170f;
            if (!IsClearForPlant(bx, bz)) continue;

            float s = 0.90f + (float)rnd.NextDouble() * 0.55f;
            SpawnOnGround(bushesAndFerns[rnd.Next(bushesAndFerns.Length)], plantsGroup, terrain,
                bx, bz, (float)rnd.NextDouble() * 360f, new Vector3(s, s, s), -0.05f, false);
        }

        // Trồng 340 cụm hoa dại (Flower1, Flower2), nấm (Mushroom1) & cỏ 3D (Grass1) từ gói Fantasy Landscape + PolygonNature
        for (int i = 0; i < 340; i++)
        {
            float fx = 135f + (float)rnd.NextDouble() * 145f;
            float fz = 130f + (float)rnd.NextDouble() * 140f;
            if (!IsClearForPlant(fx, fz)) continue;

            float s = 0.95f + (float)rnd.NextDouble() * 0.45f;
            SpawnOnGround(flowersAndGrass3D[rnd.Next(flowersAndGrass3D.Length)], plantsGroup, terrain,
                fx, fz, (float)rnd.NextDouble() * 360f, new Vector3(s, s, s), -0.03f, false);
        }

        // Rải 90 cụm đá rêu phong (Rock1, Rock_Overgrown_B/D & SM_Rock_Moss) quanh bìa rừng
        for (int i = 0; i < 90; i++)
        {
            float rx = 95f + (float)rnd.NextDouble() * 210f;
            float rz = 95f + (float)rnd.NextDouble() * 210f;
            if (!IsClearForTree(rx, rz)) continue;

            float s = 0.85f + (float)rnd.NextDouble() * 0.45f;
            SpawnOnGround(mediumRocks[rnd.Next(mediumRocks.Length)], landmarksGroup, terrain,
                rx, rz, (float)rnd.NextDouble() * 360f, new Vector3(s, s, s), -0.12f, true);
        }

        // Hiệu ứng hạt thiên nhiên bay lượn (Bươm bướm, Đom đóm)
        Transform fxGroup = new GameObject("Nature_Particle_FX").transform;
        fxGroup.SetParent(worldRoot);
        float baseH = CampPlateauHeight;
        SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/Butterlies_Particle_01_MultiColour.prefab", fxGroup,
            new Vector3(196f, baseH + 1.5f, 198f), 0f, Vector3.one, false);
        SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/Butterlies_Particle_01_Orange.prefab", fxGroup,
            new Vector3(222f, baseH + 1.2f, 185f), 0f, Vector3.one, false);
        SpawnSyntyPrefab(PolyNaturePath + "/FX/FX_Prefabs/Fireflies_01.prefab", fxGroup,
            new Vector3(202f, baseH + 1.4f, 202f), 0f, Vector3.one, false);
    }

    // ==================== 7. ĐẶT VỊ TRÍ KHỞI ĐẦU CỦA NGƯỜI CHƠI ====================

    private static void PositionPlayerInScenicSpot(Terrain terrain)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) return;

        float px = 183f;
        float pz = 200f;
        float py = terrain.SampleHeight(new Vector3(px, 0, pz));

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = new Vector3(px, py + 1.15f, pz);
        player.transform.rotation = Quaternion.Euler(0f, 86f, 0f);
        if (cc != null) cc.enabled = true;

        Camera cam = player.GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cam.transform.localRotation = Quaternion.Euler(2.5f, 0f, 0f);
            cam.farClipPlane = 550f;
            cam.clearFlags = CameraClearFlags.Skybox;
        }
    }

    // ==================== CÁC HÀM TIỆN ÍCH SPAWN PREFAB & MODEL 3D CHUẨN XÁC ====================

    private static GameObject SpawnSyntyPrefab(string assetPath, Transform parent, Vector3 pos, float rotY, Vector3 scale, bool ensureCollider)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) return null;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent);
        instance.transform.position = pos;
        instance.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        instance.transform.localScale = scale;

        if (ensureCollider)
        {
            EnsureSolidColliders(instance);
        }
        else
        {
            // Tối ưu hóa Physics & Shadow cho cây cỏ nhỏ trang trí
            foreach (var c in instance.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(c);
            }
        }
        return instance;
    }

    private static GameObject SpawnOnGround(string assetPath, Transform parent, Terrain terrain,
        float wx, float wz, float rotY, Vector3 scale, float yOffset = -0.05f, bool ensureCollider = true)
    {
        float wy = terrain.SampleHeight(new Vector3(wx, 0f, wz)) + yOffset;
        return SpawnSyntyPrefab(assetPath, parent, new Vector3(wx, wy, wz), rotY, scale, ensureCollider);
    }

    /// <summary>
    /// Khởi tạo Động vật thông minh có AI (AnimalAI), gán Animator Controller chính hãng từ SurvivalEngine,
    /// tự động đi lại (Wander), nghỉ ngơi (Idle), chạy trốn khi người chơi đến gần (Prey) hoặc cảnh giác tuần tra (Predator).
    /// </summary>
    public static GameObject SpawnAnimalWithAI(string prefabPath, Transform parent, Terrain terrain,
        float wx, float wz, float rotY, float scaleMultiplier, float walkSpeed, float runSpeed,
        float wanderRadius, bool isPrey, bool isCompanion, float detectionRadius = 8f, float yOffset = -0.01f)
    {
        GameObject instance = SpawnSurvivalPrefab(prefabPath, parent, terrain, wx, wz, rotY, scaleMultiplier, yOffset);
        if (instance == null) return null;

        AnimalAI ai = instance.GetComponent<AnimalAI>();
        if (ai == null) ai = instance.AddComponent<AnimalAI>();

        ai.walkSpeed = walkSpeed;
        ai.runSpeed = runSpeed;
        ai.wanderRadius = wanderRadius;
        ai.isPrey = isPrey;
        ai.isCompanion = isCompanion;
        ai.detectionRadius = detectionRadius;
        ai.yOffset = yOffset;

        Animator anim = instance.GetComponentInChildren<Animator>();
        if (anim == null) anim = instance.AddComponent<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            if (anim.runtimeAnimatorController == null)
            {
                string animalName = Path.GetFileNameWithoutExtension(prefabPath);
                if (animalName.Contains("WhiteRabbit")) animalName = "Rabbit";
                string controllerPath = $"Assets/SurvivalEngine/Models/Animals/{animalName}.controller";
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (controller != null)
                {
                    anim.runtimeAnimatorController = controller;
                }
            }
        }

        return instance;
    }

    public static GameObject SpawnSurvivalPrefab(string prefabPath, Transform parent, Terrain terrain,
        float wx, float wz, float rotY, float scaleMultiplier = 1.0f, float yOffset = -0.02f)
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null) return null;

        float wy = (terrain != null) ? (terrain.SampleHeight(new Vector3(wx, 0f, wz)) + yOffset) : yOffset;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
        instance.transform.SetParent(parent);
        instance.transform.position = new Vector3(wx, wy, wz);
        instance.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        instance.transform.localScale = Vector3.one * scaleMultiplier;

        CleanSurvivalPrefabInstance(instance);
        EnsureSolidColliders(instance);
        return instance;
    }

    private static void CleanSurvivalPrefabInstance(GameObject instance)
    {
        foreach (var rb in instance.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(rb);
        }
        AssignURPMaterialsToInstance(instance);
    }

    public static void AssignURPMaterialsToInstance(GameObject instance)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;

                if (m.shader == null || m.shader.name.Contains("Error") || m.shader.name == "Standard")
                {
                    string cleanName = m.name.Replace(" (Instance)", "").Trim();
                    Material extMat = AssetDatabase.LoadAssetAtPath<Material>(SurvivalMaterials + "/" + cleanName + ".mat");
                    if (extMat != null && extMat.shader != null && !extMat.shader.name.Contains("Error"))
                    {
                        mats[i] = extMat;
                        changed = true;
                    }
                    else
                    {
                        m.shader = urpLit;
                        m.enableInstancing = true;
                        EditorUtility.SetDirty(m);
                    }
                }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    public static Bounds ComputeWorldBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0) renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    private static void EnsureSolidColliders(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>() != null) return;

        MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in mfs)
        {
            if (mf.sharedMesh != null && mf.gameObject.GetComponent<Collider>() == null)
            {
                mf.gameObject.AddComponent<MeshCollider>();
            }
        }
    }

    // ==================== HỖ TRỢ PICKUP CHO LAB 2 / LAB 3 ====================

    public static void CreatePickupMesh(GameObject item, string type, Material mat)
    {
        MeshFilter mf = item.GetComponent<MeshFilter>();
        if (mf != null) Object.DestroyImmediate(mf);
        MeshRenderer mr = item.GetComponent<MeshRenderer>();
        if (mr != null) Object.DestroyImmediate(mr);

        string prefabPath = PolyTrees + "/SM_Tree_Branch_01.prefab";
        Vector3 scale = new Vector3(0.65f, 0.65f, 0.65f);

        if (type == "Stone")
        {
            prefabPath = PolyRocks + "/SM_Rock_Small_01.prefab";
            scale = new Vector3(0.55f, 0.55f, 0.55f);
        }
        else if (type == "Mushroom")
        {
            prefabPath = PxltigerPrefabs + "/Mushroom1.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                prefabPath = PolyPlants + "/SM_Plant_Mushroom_Red_01.prefab";
            scale = new Vector3(1.5f, 1.5f, 1.5f);
        }
        else if (type == "Berries")
        {
            prefabPath = PolyPlants + "/SM_Plant_Bush_01.prefab";
            scale = new Vector3(0.35f, 0.35f, 0.35f);
        }

        GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (pref != null)
        {
            GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(pref);
            child.transform.SetParent(item.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = scale;
            foreach (var c in child.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
        }

        BoxCollider bc = item.GetComponent<BoxCollider>();
        if (bc == null) bc = item.AddComponent<BoxCollider>();
        bc.size = new Vector3(0.8f, 0.5f, 0.8f);
        bc.center = new Vector3(0f, 0.25f, 0f);
    }
}
#endif
