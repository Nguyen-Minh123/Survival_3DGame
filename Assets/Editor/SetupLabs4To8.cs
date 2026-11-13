#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// SetupLabs4To8 - Tự động thiết lập toàn bộ giao diện, Prefabs, Resources, Bản đồ 3D phong phú
/// và xuất bản file EXE cho Lab 4, Lab 5, Lab 6, và Lab 7_8 (MSSV: 2312690).
/// </summary>
public static class SetupLabs4To8
{
    private const string ProjectRoot = "Assets";
    private const string PrefabsFolder = ProjectRoot + "/Prefabs";
    private const string ResourcesFolder = ProjectRoot + "/Resources";
    private const string TexturesFolder = ProjectRoot + "/Textures";
    private const string MaterialsFolder = ProjectRoot + "/Materials";
    private const string ScenesFolder = ProjectRoot + "/Scenes";

    // ==================== MENU CHÍNH TINH GỌN (TOP LEVEL) ====================

    [MenuItem("3D Survival Game/🎮 Thiết Lập Game Hoàn Chỉnh (SampleScene)", false, 1)]
    public static void RunFullSetupSampleScene()
    {
        SetupCompleteSurvivalScene(8);
    }

    [MenuItem("3D Survival Game/📦 Xuất Bản Game (Build Standalone EXE)", false, 3)]
    public static void BuildFinalStandaloneGame()
    {
        BuildExecutableLab7_8();
    }

    // ==================== SUBMENU: NỘP BÀI & BUILD TỪNG LAB ====================

    [MenuItem("3D Survival Game/📁 Nộp Bài Từng Lab/🌟 BUILD TOÀN BỘ LAB 1 -> LAB 8 (FULL PIPELINE)", false, 50)]
    public static void BuildAllLabsSequential()
    {
        BuildLab1WithRichMap();
        BuildLab2WithRichMap();
        BuildLab3WithRichMap();
        BuildExecutableLab4();
        BuildExecutableLab5();
        BuildExecutableLab6();
        BuildExecutableLab7_8();
    }

    [MenuItem("3D Survival Game/📁 Nộp Bài Từng Lab/🌟 BUILD LAB 4 -> LAB 8 (FINAL LABS)", false, 51)]
    public static void BuildFinalLabs()
    {
        BuildExecutableLab4();
        BuildExecutableLab5();
        BuildExecutableLab6();
        BuildExecutableLab7_8();
    }

    [MenuItem("3D Survival Game/📁 Nộp Bài Từng Lab/Lab 1 - Bản Đồ & Góc Nhìn FPP", false, 61)]
    public static void BuildLab1WithRichMap()
    {
        SetupLab1Scene.RunFullSetup();
        FantasyWorldBuilder.DecorateWorld();
        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene, ScenesFolder + "/SampleScene.unity");
        AssetDatabase.SaveAssets();
        BuildStandaloneExe("Build_Lab1");
    }

    [MenuItem("3D Survival Game/📁 Nộp Bài Từng Lab/Lab 2 - Lựa Chọn & Nhặt Vật Phẩm", false, 62)]
    public static void BuildLab2WithRichMap()
    {
        SetupLab1Scene.RunFullSetup();
        FantasyWorldBuilder.DecorateWorld();
        SetupLab2Scene.RunFullSetupLab2();
        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene, ScenesFolder + "/SampleScene.unity");
        AssetDatabase.SaveAssets();
        BuildStandaloneExe("Build_Lab2");
    }

    [MenuItem("3D Survival Game/📁 Nộp Bài Từng Lab/Lab 3 - Túi Đồ & Kéo Thả Vật Phẩm", false, 63)]
    public static void BuildLab3WithRichMap()
    {
        SetupCompleteSurvivalScene(3);
        BuildStandaloneExe("Build_Lab3");
    }

    [MenuItem("3D Survival Game/📁 Nộp Bài Từng Lab/Lab 4 - Chế Tạo Vật Phẩm (Crafting)", false, 64)]
    public static void BuildExecutableLab4()
    {
        SetupCompleteSurvivalScene(4);
        BuildStandaloneExe("Build_Lab4");
    }

    [MenuItem("3D Survival Game/📁 Nộp Bài Từng Lab/Lab 5 - Cursors & Thanh Máu Năng Lượng", false, 65)]
    public static void BuildExecutableLab5()
    {
        SetupCompleteSurvivalScene(5);
        BuildStandaloneExe("Build_Lab5");
    }

    [MenuItem("3D Survival Game/📁 Nộp Bài Từng Lab/Lab 6 - Xem Chi Tiết, Dùng & Vứt Đồ", false, 66)]
    public static void BuildExecutableLab6()
    {
        SetupCompleteSurvivalScene(6);
        BuildStandaloneExe("Build_Lab6");
    }

    [MenuItem("3D Survival Game/📁 Nộp Bài Từng Lab/Lab 7 & 8 - Quick Slots & Cầm Rìu Chặt Cây", false, 67)]
    public static void BuildExecutableLab7_8()
    {
        SetupCompleteSurvivalScene(8);
        BuildStandaloneExe("Build_Lab7_Lab8");
    }

    // ==================== HÀM THIẾT LẬP TỔNG THỂ ====================

    public static void SetupCompleteSurvivalScene(int targetLabLevel = 8)
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        Debug.Log($">>> BẮT ĐẦU THIẾT LẬP SCENE CHO LAB CẤP ĐỘ {targetLabLevel} (MSSV: 2312690) <<<");

        EnsureDirectories();
        EnsureRequiredTags();

        string scenePath = ScenesFolder + "/SampleScene.unity";
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            if (File.Exists(scenePath))
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            else
            {
                SetupLab1Scene.RunFullSetup();
            }
        }
        else
        {
            Terrain t = Object.FindAnyObjectByType<Terrain>();
            if (t == null) SetupLab1Scene.RunFullSetup();
        }

        // 1. Trang trí bản đồ 3D phong phú (Rừng cây, Ngôi làng, Nhà gỗ, Hồ nước, Cầu cảng, Lửa trại, Hoa cỏ 3D)
        FantasyWorldBuilder.DecorateWorld();

        // 3. Tạo bộ Icon Sprites sắc nét cho các vật phẩm & UI
        Sprite stickIcon = CreateStylizedIcon("Icon_Stick", "Stick");
        Sprite stoneIcon = CreateStylizedIcon("Icon_Rock", "Stone");
        Sprite mushroomIcon = CreateStylizedIcon("Icon_Mushroom", "Mushroom");
        Sprite berryIcon = CreateStylizedIcon("Icon_Berry", "Berries");
        Sprite axeIcon = CreateStylizedIcon("Icon_Axe", "Axe");
        Sprite handSprite = CreateStylizedIcon("Icon_HandCursor", "Hand");
        Sprite dotSprite = CreateStylizedIcon("Icon_CenterDot", "Dot");
        Sprite trashClosedSprite = CreateStylizedIcon("Icon_TrashClosed", "TrashClosed");
        Sprite trashOpenSprite = CreateStylizedIcon("Icon_TrashOpen", "TrashOpen");

        // 4. Tạo các Prefabs vật phẩm nhặt ngoài bản đồ (Stick, Stone, Mushroom, Berries, Axe) và rải quanh map
        SetupWorldPickups(stickIcon, stoneIcon, mushroomIcon, berryIcon, axeIcon);

        // 5. Tạo các Prefab UI_Item trong Resources ("Stick", "Stone", "Mushroom", "Berries", "Axe")
        GameObject baseUIItemPrefab = CreateUIItemPrefabsInResources(stickIcon, stoneIcon, mushroomIcon, berryIcon, axeIcon);

        // 6. Thiết lập Canvas chính & EventSystem
        Canvas canvas = EnsureMainCanvas();
        EnsureEventSystem();

        // Xóa các UI cũ trên Canvas để dựng lại sạch sẽ
        List<GameObject> childrenToDelete = new List<GameObject>();
        foreach (Transform child in canvas.transform)
        {
            childrenToDelete.Add(child.gameObject);
        }
        foreach (var c in childrenToDelete) Object.DestroyImmediate(c);

        // 7. Xây dựng Crosshair (CenterDot & HandIcon) & Interaction Info UI
        GameObject interactionUI = BuildCrosshairAndInteractionUI(canvas.transform, dotSprite, handSprite,
            out Image centerDotImg, out Image handIconImg);

        // 8. Xây dựng Status Bars HUD ở góc trên bên trái (Health, Calories, Hydration - Lab 5+)
        GameObject statusBarsHUD = BuildStatusBarsHUD(canvas.transform, out HealthBar hb, out CaloriesBar cb, out HydrationBar hyb);
        statusBarsHUD.SetActive(targetLabLevel >= 5);

        // 9. Xây dựng Thanh QuickSlots (Phím 1-7) ở cạnh dưới màn hình (Lab 7-8)
        GameObject quickSlotsRoot = BuildQuickSlotsUI(canvas.transform, out GameObject quickSlotsPanel, out GameObject numbersHolder);
        quickSlotsRoot.SetActive(targetLabLevel >= 7);

        // 10. Xây dựng Giao diện Kho Đồ (Inventory Screen UI + ItemInfoUI + TrashSlot + TrashAlertUI)
        GameObject inventoryUI = BuildInventoryUI(
            canvas.transform, targetLabLevel,
            trashClosedSprite, trashOpenSprite,
            out Transform slotsGrid, out GameObject itemInfoUI, out GameObject trashAlertUI);

        // 11. Xây dựng Giao diện Chế Tạo (Crafting Screen UI & Tools Screen UI - Lab 4+)
        BuildCraftingUI(canvas.transform, axeIcon, out GameObject craftingScreenUI, out GameObject toolsScreenUI);

        // 12. Xây dựng Hộp thoại Thông báo Nhặt Đồ (Pickup Alert Popup - Lab 5+)
        GameObject pickupAlertUI = BuildPickupAlertUI(canvas.transform, out Text pickupNameText, out Image pickupIconImg);

        // 13. Xây dựng Thanh hướng dẫn phím tắt nhỏ gọn ở góc trên bên phải
        BuildControlsHintBanner(canvas.transform, targetLabLevel);

        // 14. Gắn và liên kết toàn bộ Scripts trên Player & Camera (InventorySystem, CraftingSystem, PlayerState, EquipSystem, SelectionManager, ToolHolder)
        WirePlayerSystems(
            targetLabLevel,
            stickIcon, stoneIcon, mushroomIcon, berryIcon, axeIcon,
            baseUIItemPrefab,
            interactionUI, centerDotImg, handIconImg,
            inventoryUI, slotsGrid, itemInfoUI,
            pickupAlertUI, pickupNameText, pickupIconImg,
            craftingScreenUI, toolsScreenUI,
            quickSlotsPanel, numbersHolder,
            hb, cb, hyb);

        // 15. Đặt sẵn các vật phẩm mẫu trong Kho Đồ (3 Stone + 3 Stick + 2 Mushroom + 2 Berries) và QuickSlot_1 (Axe)
        PopulateStarterInventoryItems(slotsGrid, quickSlotsPanel.transform, targetLabLevel);

        // Lưu Scene
        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene, ScenesFolder + "/SampleScene.unity");
        AssetDatabase.SaveAssets();

        Debug.Log($"🎉 HOÀN TẤT THIẾT LẬP SCENE LAB {targetLabLevel} THÀNH CÔNG!");
    }

    public static void BuildStandaloneExe(string buildFolderName)
    {
        string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../" + buildFolderName));
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

        string exePath = Path.Combine(buildDir, "2312690_3DSurvivalGame.exe");
        Debug.Log(">>> Bắt đầu Build Standalone Windows EXE tại: " + exePath);

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenesFolder + "/SampleScene.unity" },
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("🎉 BUILD THÀNH CÔNG: " + exePath);
        }
        else
        {
            Debug.LogError("❌ BUILD THẤT BẠI: " + report.summary.result);
        }
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(PrefabsFolder)) AssetDatabase.CreateFolder(ProjectRoot, "Prefabs");
        if (!AssetDatabase.IsValidFolder(ResourcesFolder)) AssetDatabase.CreateFolder(ProjectRoot, "Resources");
        if (!AssetDatabase.IsValidFolder(TexturesFolder)) AssetDatabase.CreateFolder(ProjectRoot, "Textures");
        if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder(ProjectRoot, "Materials");
        if (!AssetDatabase.IsValidFolder(ScenesFolder)) AssetDatabase.CreateFolder(ProjectRoot, "Scenes");
    }

    private static void EnsureRequiredTags()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        string[] neededTags = { "QuickSlot", "Slot", "Pickable" };
        foreach (string tag in neededTags)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            }
        }
        tagManager.ApplyModifiedProperties();
    }

    // ==================== VẼ & NẠP ICONS SẮC NÉT ====================

    private static Sprite CreateStylizedIcon(string fileName, string kind)
    {
        // Ưu tiên nạp bộ Icon vẽ tay chính hãng từ SurvivalEngine/Sprites/Icons
        string seIconPath = null;
        if (kind == "Stick") seIconPath = "Assets/SurvivalEngine/Sprites/Icons/Wood.png";
        else if (kind == "Stone") seIconPath = "Assets/SurvivalEngine/Sprites/Icons/Rock.png";
        else if (kind == "Mushroom") seIconPath = "Assets/SurvivalEngine/Sprites/Icons/MushroomRed.png";
        else if (kind == "Berries") seIconPath = "Assets/SurvivalEngine/Sprites/Icons/Blueberry.png";
        else if (kind == "Axe") seIconPath = "Assets/SurvivalEngine/Sprites/Icons/Axe.png";

        if (!string.IsNullOrEmpty(seIconPath) && File.Exists(seIconPath))
        {
            TextureImporter seImp = AssetImporter.GetAtPath(seIconPath) as TextureImporter;
            if (seImp != null && seImp.textureType != TextureImporterType.Sprite)
            {
                seImp.textureType = TextureImporterType.Sprite;
                seImp.spriteImportMode = SpriteImportMode.Single;
                seImp.alphaIsTransparency = true;
                seImp.SaveAndReimport();
            }
            Sprite seSprite = AssetDatabase.LoadAssetAtPath<Sprite>(seIconPath);
            if (seSprite != null) return seSprite;
        }

        string path = TexturesFolder + "/" + fileName + ".png";
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, Color.clear);
            }
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - 64f) / 64f;
                float ny = (y - 64f) / 64f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);

                if (kind == "Dot")
                {
                    if (r < 0.35f) tex.SetPixel(x, y, Color.white);
                    else if (r < 0.48f) tex.SetPixel(x, y, new Color(0, 0, 0, 0.7f));
                    continue;
                }

                if (kind == "Hand")
                {
                    bool palm = (Mathf.Abs(nx) < 0.32f && ny > -0.42f && ny < 0.12f);
                    bool fingers = (Mathf.Abs(nx) < 0.30f && ny >= 0.12f && ny < 0.52f && (Mathf.Abs((nx + 0.3f) * 12f % 1f - 0.5f) < 0.38f));
                    bool thumb = (nx > -0.52f && nx <= -0.28f && ny > -0.25f && ny < 0.08f);
                    if (palm || fingers || thumb)
                        tex.SetPixel(x, y, new Color(1f, 0.96f, 0.85f, 0.95f));
                    continue;
                }

                if (kind == "TrashClosed" || kind == "TrashOpen")
                {
                    bool binBody = (Mathf.Abs(nx) < 0.32f - (0.3f - ny) * 0.08f && ny > -0.55f && ny < 0.18f);
                    float lidY = (kind == "TrashOpen") ? (0.32f + nx * 0.35f) : 0.25f;
                    bool lid = (Mathf.Abs(nx) < 0.40f && Mathf.Abs(ny - lidY) < 0.07f);
                    bool handle = (Mathf.Abs(nx) < 0.14f && Mathf.Abs(ny - (lidY + 0.09f)) < 0.05f);
                    if (binBody || lid || handle)
                    {
                        bool stripe = binBody && (Mathf.Abs(nx) < 0.04f || Mathf.Abs(Mathf.Abs(nx) - 0.16f) < 0.03f) && ny > -0.42f && ny < 0.08f;
                        tex.SetPixel(x, y, stripe ? new Color(0.15f, 0.15f, 0.15f, 0.9f) : new Color(0.92f, 0.92f, 0.92f, 0.95f));
                    }
                    continue;
                }

                if (kind == "Stick")
                {
                    float dLine = Mathf.Abs(ny - nx * 0.85f);
                    bool mainBranch = (dLine < 0.11f && Mathf.Abs(nx) < 0.58f);
                    bool twig = (nx > -0.1f && nx < 0.25f && ny > 0.05f && ny < 0.45f && Mathf.Abs((ny - 0.1f) - (nx + 0.05f) * 1.8f) < 0.07f);
                    if (mainBranch || twig)
                        tex.SetPixel(x, y, Color.Lerp(new Color(0.45f, 0.26f, 0.10f), new Color(0.72f, 0.48f, 0.22f), (ny + 1f) * 0.5f));
                }
                else if (kind == "Stone")
                {
                    float polyR = 0.52f + 0.06f * Mathf.Sin(Mathf.Atan2(ny, nx) * 5f);
                    if (r < polyR)
                    {
                        Color rockC = (ny > nx * 0.3f) ? new Color(0.68f, 0.70f, 0.72f) : new Color(0.44f, 0.46f, 0.49f);
                        if (r > polyR - 0.06f) rockC = new Color(0.22f, 0.24f, 0.26f);
                        tex.SetPixel(x, y, rockC);
                    }
                }
                else if (kind == "Mushroom")
                {
                    bool stem = (Mathf.Abs(nx) < 0.16f && ny >= -0.55f && ny <= 0.05f);
                    float capDist = Mathf.Sqrt(nx * nx * 1.1f + (ny - 0.12f) * (ny - 0.12f) * 2.2f);
                    bool cap = (ny > -0.02f && capDist < 0.58f);
                    if (cap)
                    {
                        bool spot = (Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.18f, 0.25f)) < 0.09f) ||
                                    (Vector2.Distance(new Vector2(nx, ny), new Vector2(0.20f, 0.22f)) < 0.08f) ||
                                    (Vector2.Distance(new Vector2(nx, ny), new Vector2(0.0f, 0.38f)) < 0.08f);
                        tex.SetPixel(x, y, spot ? Color.white : new Color(0.90f, 0.18f, 0.16f));
                    }
                    else if (stem)
                    {
                        tex.SetPixel(x, y, new Color(0.95f, 0.90f, 0.80f));
                    }
                }
                else if (kind == "Berries")
                {
                    float b1 = Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.18f, -0.10f));
                    float b2 = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.18f, -0.10f));
                    float b3 = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.0f, 0.14f));
                    bool leaf = (ny > 0.20f && ny < 0.52f && Mathf.Abs(nx) < (0.55f - ny) * 0.8f);
                    if (b1 < 0.24f || b2 < 0.24f || b3 < 0.24f)
                        tex.SetPixel(x, y, new Color(0.88f, 0.12f, 0.38f));
                    else if (leaf)
                        tex.SetPixel(x, y, new Color(0.22f, 0.72f, 0.24f));
                }
                else if (kind == "Axe")
                {
                    float dHandle = Mathf.Abs(ny - nx * 0.95f);
                    bool handle = (dHandle < 0.09f && nx > -0.55f && nx < 0.42f);
                    bool blade = (nx > -0.05f && nx < 0.55f && ny > 0.02f && ny < 0.55f && Mathf.Abs((nx - 0.22f) + (ny - 0.28f) * 0.6f) < 0.26f);
                    if (blade)
                        tex.SetPixel(x, y, new Color(0.66f, 0.69f, 0.72f));
                    else if (handle)
                        tex.SetPixel(x, y, new Color(0.62f, 0.38f, 0.16f));
                }
            }
        }

        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ==================== WORLD PICKUPS & RESOURCES ====================

    private static void SetupWorldPickups(Sprite stickIcon, Sprite stoneIcon, Sprite mushroomIcon, Sprite berryIcon, Sprite axeIcon)
    {
        CreateOrUpdateSyntyPickup(PrefabsFolder + "/Pickup_Stick.prefab", "Assets/PolygonNature/Prefabs/Trees/SM_Tree_Branch_01.prefab", "Stick", stickIcon, new Vector3(0.75f, 0.75f, 0.75f));
        CreateOrUpdateSyntyPickup(PrefabsFolder + "/Pickup_Rock.prefab", "Assets/PolygonNature/Prefabs/Rocks/SM_Rock_Small_01.prefab", "Stone", stoneIcon, new Vector3(0.55f, 0.55f, 0.55f));
        CreateOrUpdateSyntyPickup(PrefabsFolder + "/Pickup_Mushroom.prefab", "Assets/PolygonNature/Prefabs/Plants/SM_Plant_Mushrooms_01.prefab", "Mushroom", mushroomIcon, new Vector3(0.85f, 0.85f, 0.85f));
        CreateOrUpdateSyntyPickup(PrefabsFolder + "/Pickup_Berry.prefab", "Assets/PolygonNature/Prefabs/Plants/SM_Plant_Bush_01.prefab", "Berries", berryIcon, new Vector3(0.35f, 0.35f, 0.35f));

        // Cập nhật 3D Stone Axe Model (Resources/Axe_Model.prefab) dùng mô hình Primitive Axe.fbx từ SurvivalEngine
        UpgradeAxe3DModelPrefab();

        // Dọn dẹp các pickup cũ trong Scene và rải lại bằng các Model 3D đẹp mắt
        GameObject oldPickups = GameObject.Find("World_Pickups");
        if (oldPickups != null) Object.DestroyImmediate(oldPickups);

        GameObject pickupsGroup = new GameObject("World_Pickups");
        Terrain terrain = Object.FindAnyObjectByType<Terrain>();
        if (terrain == null) return;

        GameObject stickPref = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsFolder + "/Pickup_Stick.prefab");
        GameObject rockPref = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsFolder + "/Pickup_Rock.prefab");
        GameObject shroomPref = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsFolder + "/Pickup_Mushroom.prefab");
        GameObject berryPref = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsFolder + "/Pickup_Berry.prefab");

        GameObject[] allPrefabs = new GameObject[] { stickPref, rockPref, shroomPref, berryPref };

        // 1. Rải các vật phẩm ngay trước mặt người chơi (spawn tại 183, 200 nhìn hướng Đông) và quanh lửa trại (198, 200)
        Vector2[] closePositions = new Vector2[]
        {
            new Vector2(186.2f, 199.2f), // Stick ngay trước mặt
            new Vector2(186.8f, 200.8f), // Stone ngay trước mặt
            new Vector2(188.2f, 198.4f), // Mushroom
            new Vector2(188.6f, 201.6f), // Berries
            new Vector2(190.5f, 199.5f), // Stick
            new Vector2(191.2f, 201.0f), // Stone
            new Vector2(193.0f, 197.8f), // Mushroom
            new Vector2(193.5f, 202.2f), // Berries
            new Vector2(196.0f, 195.5f), // Stick
            new Vector2(201.5f, 196.2f), // Stone
            new Vector2(202.8f, 203.0f), // Berries
            new Vector2(205.0f, 199.2f), // Mushroom
            new Vector2(211.0f, 199.5f), // Stick trên lối ra cầu gỗ
            new Vector2(214.5f, 200.6f)  // Stone trên lối ra cầu gỗ
        };

        for (int i = 0; i < closePositions.Length; i++)
        {
            GameObject chosen = allPrefabs[i % allPrefabs.Length];
            if (chosen == null) continue;
            float px = closePositions[i].x;
            float pz = closePositions[i].y;
            float py = terrain.SampleHeight(new Vector3(px, 0, pz));

            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(chosen);
            item.transform.SetParent(pickupsGroup.transform);
            item.transform.position = new Vector3(px, py + 0.04f, pz);
            item.transform.rotation = Quaternion.Euler(0, i * 43f, 0);
        }

        // 2. Rải thêm 85 vật phẩm rải rác khắp thung lũng và bên kia cầu (tránh lòng sông & lòng hồ nơi y < 6.8m)
        System.Random rnd = new System.Random(777);
        for (int i = 0; i < 85; i++)
        {
            float rx = (float)rnd.NextDouble() * 140f + 130f;
            float rz = (float)rnd.NextDouble() * 130f + 130f;
            float ry = terrain.SampleHeight(new Vector3(rx, 0, rz));
            if (ry < 6.8f) continue; // Tránh lòng sông & lòng hồ

            GameObject chosen = allPrefabs[rnd.Next(allPrefabs.Length)];
            if (chosen == null) continue;

            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(chosen);
            item.transform.SetParent(pickupsGroup.transform);
            item.transform.position = new Vector3(rx, ry + 0.04f, rz);
            item.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
        }
    }

    private static void UpgradeAxe3DModelPrefab()
    {
        string axeModelPath = ResourcesFolder + "/Axe_Model.prefab";
        GameObject existingAxe = AssetDatabase.LoadAssetAtPath<GameObject>(axeModelPath);
        GameObject seAxeFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SurvivalEngine/Models/Tools/Primitive Axe.fbx");
        if (seAxeFbx == null) return;

        RuntimeAnimatorController animCtrl = null;
        if (existingAxe != null)
        {
            Animator oldAnim = existingAxe.GetComponent<Animator>();
            if (oldAnim != null) animCtrl = oldAnim.runtimeAnimatorController;
        }

        GameObject root = new GameObject("Axe_Model");
        root.transform.localPosition = new Vector3(0.42f, -0.36f, 0.68f);
        root.transform.localRotation = Quaternion.Euler(10f, -95f, -10f);
        root.transform.localScale = Vector3.one;

        GameObject meshChild = (GameObject)PrefabUtility.InstantiatePrefab(seAxeFbx);
        meshChild.name = "PrimitiveAxe_Mesh";
        meshChild.transform.SetParent(root.transform, false);
        FantasyWorldBuilder.AssignURPMaterialsToInstance(meshChild);

        Bounds b = FantasyWorldBuilder.ComputeWorldBounds(meshChild);
        if (b.size.magnitude > 0.001f)
        {
            float s = 0.68f / Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            meshChild.transform.localScale = Vector3.one * s;
        }
        meshChild.transform.localPosition = Vector3.zero;
        meshChild.transform.localRotation = Quaternion.identity;

        foreach (var col in root.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);

        Animator anim = root.AddComponent<Animator>();
        if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
        root.AddComponent<EquippableItem>();

        PrefabUtility.SaveAsPrefabAsset(root, axeModelPath);
        Object.DestroyImmediate(root);
    }

    private static void CreateOrUpdateSyntyPickup(string targetPrefabPath, string sourceSyntyPath, string itemName, Sprite icon, Vector3 scale)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourceSyntyPath);
        GameObject root = new GameObject("Pickup_" + itemName);

        if (source != null)
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.transform.SetParent(root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale = scale;
            FantasyWorldBuilder.AssignURPMaterialsToInstance(model);
            Collider[] childCols = model.GetComponentsInChildren<Collider>();
            foreach (var c in childCols) Object.DestroyImmediate(c);
        }
        else
        {
            GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prim.transform.SetParent(root.transform);
            prim.transform.localScale = Vector3.one * 0.3f;
            Object.DestroyImmediate(prim.GetComponent<Collider>());
        }

        BoxCollider col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(0.9f, 0.6f, 0.9f);
        col.center = new Vector3(0, 0.3f, 0);

        InteractableObject io = root.AddComponent<InteractableObject>();
        io.ItemName = itemName;
        io.itemIcon = icon;
        io.interactionDistance = 4.5f;

        PrefabUtility.SaveAsPrefabAsset(root, targetPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static GameObject CreateUIItemPrefabsInResources(Sprite stickIcon, Sprite stoneIcon, Sprite mushroomIcon, Sprite berryIcon, Sprite axeIcon)
    {
        GameObject basePrefab = CreateSingleUIItemPrefab(PrefabsFolder + "/UI_Item.prefab", "Item", stickIcon);

        CreateSingleUIItemPrefab(ResourcesFolder + "/Stick.prefab", "Stick", stickIcon);
        CreateSingleUIItemPrefab(ResourcesFolder + "/Stone.prefab", "Stone", stoneIcon);
        CreateSingleUIItemPrefab(ResourcesFolder + "/Mushroom.prefab", "Mushroom", mushroomIcon);
        CreateSingleUIItemPrefab(ResourcesFolder + "/Berries.prefab", "Berries", berryIcon);
        CreateSingleUIItemPrefab(ResourcesFolder + "/Axe.prefab", "Axe", axeIcon);

        return basePrefab;
    }

    private static GameObject CreateSingleUIItemPrefab(string path, string itemName, Sprite icon)
    {
        GameObject root = new GameObject(itemName);
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64f, 64f);
        rt.anchoredPosition = Vector2.zero;

        Image img = root.AddComponent<Image>();
        img.sprite = icon;
        img.color = Color.white;

        CanvasGroup cg = root.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;

        DragDrop dd = root.AddComponent<DragDrop>();
        dd.itemName = itemName;

        InventoryItem invItem = root.AddComponent<InventoryItem>();
        invItem.thisName = itemName;
        invItem.EnsureDefaultMetadata();

        // Nhãn tên nhỏ phía dưới icon
        GameObject textObj = new GameObject("Text_Name");
        textObj.transform.SetParent(root.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 0.32f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = itemName;
        text.fontSize = 12;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.LowerCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;

        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(1, -1);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ==================== XÂY DỰNG CANVAS & UI PANELS ====================

    private static Canvas EnsureMainCanvas()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null) return canvas;

        GameObject canvasObj = new GameObject("Canvas_GameUI");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        EventSystem es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    private static GameObject BuildCrosshairAndInteractionUI(Transform canvasTransform, Sprite dotSprite, Sprite handSprite, out Image centerDotImg, out Image handIconImg)
    {
        // Center Dot
        GameObject dotObj = new GameObject("CenterDot");
        dotObj.transform.SetParent(canvasTransform, false);
        RectTransform dotRt = dotObj.AddComponent<RectTransform>();
        dotRt.sizeDelta = new Vector2(14f, 14f);
        dotRt.anchoredPosition = Vector2.zero;
        centerDotImg = dotObj.AddComponent<Image>();
        centerDotImg.sprite = dotSprite;
        centerDotImg.raycastTarget = false;

        // Hand Icon
        GameObject handObj = new GameObject("HandIcon");
        handObj.transform.SetParent(canvasTransform, false);
        RectTransform handRt = handObj.AddComponent<RectTransform>();
        handRt.sizeDelta = new Vector2(42f, 42f);
        handRt.anchoredPosition = Vector2.zero;
        handIconImg = handObj.AddComponent<Image>();
        handIconImg.sprite = handSprite;
        handIconImg.raycastTarget = false;
        handObj.SetActive(false);

        // Interaction Info Prompt
        GameObject infoObj = new GameObject("interaction_Info_UI");
        infoObj.transform.SetParent(canvasTransform, false);
        RectTransform infoRt = infoObj.AddComponent<RectTransform>();
        infoRt.sizeDelta = new Vector2(260f, 42f);
        infoRt.anchoredPosition = new Vector2(0, 48f);

        Image bg = infoObj.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.80f);
        bg.raycastTarget = false;

        GameObject txtObj = new GameObject("interaction_text");
        txtObj.transform.SetParent(infoObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;

        infoObj.SetActive(false);
        return infoObj;
    }

    private static GameObject BuildStatusBarsHUD(Transform canvasTransform, out HealthBar hb, out CaloriesBar cb, out HydrationBar hyb)
    {
        GameObject hud = new GameObject("StatusBars_HUD");
        hud.transform.SetParent(canvasTransform, false);
        RectTransform hudRt = hud.AddComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0, 1);
        hudRt.anchorMax = new Vector2(0, 1);
        hudRt.pivot = new Vector2(0, 1);
        hudRt.anchoredPosition = new Vector2(28f, -24f);
        hudRt.sizeDelta = new Vector2(320f, 115f);

        GameObject hSliderObj = CreateStatusBarSlider(hud.transform, "HealthBar", new Vector2(0, -16f), new Color(0.86f, 0.16f, 0.16f), "100/100", out Text hText);
        hb = hSliderObj.AddComponent<HealthBar>();
        hb.healthCounter = hText;

        GameObject cSliderObj = CreateStatusBarSlider(hud.transform, "CaloriesBar", new Vector2(0, -52f), new Color(0.95f, 0.72f, 0.12f), "1800/2000", out Text cText);
        cb = cSliderObj.AddComponent<CaloriesBar>();
        cb.caloriesCounter = cText;

        GameObject hySliderObj = CreateStatusBarSlider(hud.transform, "HydrationBar", new Vector2(0, -88f), new Color(0.18f, 0.62f, 0.96f), "98%", out Text hyText);
        hyb = hySliderObj.AddComponent<HydrationBar>();
        hyb.hydrationCounter = hyText;

        return hud;
    }

    private static GameObject CreateStatusBarSlider(Transform parent, string name, Vector2 pos, Color fillCol, string defaultText, out Text counterText)
    {
        GameObject barObj = new GameObject(name);
        barObj.transform.SetParent(parent, false);
        RectTransform rt = barObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(290f, 26f);

        Image bg = barObj.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(barObj.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(3, 3);
        faRt.offsetMax = new Vector2(-3, -3);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRt = fill.AddComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = Vector2.zero;
        fRt.offsetMax = Vector2.zero;

        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = fillCol;

        Slider slider = barObj.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.fillRect = fRt;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        // Text Counter
        GameObject txtObj = new GameObject("CounterText");
        txtObj.transform.SetParent(barObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        counterText = txtObj.AddComponent<Text>();
        counterText.text = defaultText;
        counterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        counterText.fontSize = 14;
        counterText.fontStyle = FontStyle.Bold;
        counterText.alignment = TextAnchor.MiddleCenter;
        counterText.color = Color.white;

        Shadow sh = txtObj.AddComponent<Shadow>();
        sh.effectColor = Color.black;
        sh.effectDistance = new Vector2(1, -1);

        return barObj;
    }

    private static GameObject BuildQuickSlotsUI(Transform canvasTransform, out GameObject quickSlotsPanel, out GameObject numbersHolder)
    {
        GameObject root = new GameObject("QuickSlots_UI_Container");
        root.transform.SetParent(canvasTransform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0f);
        rootRt.anchorMax = new Vector2(0.5f, 0f);
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.anchoredPosition = new Vector2(0, 18f);
        rootRt.sizeDelta = new Vector2(610f, 92f);

        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.09f, 0.08f, 0.88f);

        quickSlotsPanel = new GameObject("QuickSlotsPanel");
        quickSlotsPanel.transform.SetParent(root.transform, false);
        RectTransform qRt = quickSlotsPanel.AddComponent<RectTransform>();
        qRt.anchorMin = Vector2.zero;
        qRt.anchorMax = Vector2.one;
        qRt.offsetMin = new Vector2(12, 8);
        qRt.offsetMax = new Vector2(-12, -8);

        GridLayoutGroup grid = quickSlotsPanel.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(72f, 72f);
        grid.spacing = new Vector2(10f, 0f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7;
        grid.childAlignment = TextAnchor.MiddleCenter;

        numbersHolder = new GameObject("NumbersHolder");
        numbersHolder.transform.SetParent(root.transform, false);
        RectTransform nRt = numbersHolder.AddComponent<RectTransform>();
        nRt.anchorMin = Vector2.zero;
        nRt.anchorMax = Vector2.one;
        nRt.offsetMin = new Vector2(12, 8);
        nRt.offsetMax = new Vector2(-12, -8);

        GridLayoutGroup numGrid = numbersHolder.AddComponent<GridLayoutGroup>();
        numGrid.cellSize = new Vector2(72f, 72f);
        numGrid.spacing = new Vector2(10f, 0f);
        numGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        numGrid.constraintCount = 7;
        numGrid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 1; i <= 7; i++)
        {
            GameObject qSlot = new GameObject("QuickSlot_" + i);
            qSlot.tag = "QuickSlot";
            qSlot.transform.SetParent(quickSlotsPanel.transform, false);
            Image sBg = qSlot.AddComponent<Image>();
            sBg.color = new Color(0.22f, 0.20f, 0.18f, 0.95f);
            qSlot.AddComponent<ItemSlot>();

            GameObject numObj = new GameObject("number" + i);
            numObj.transform.SetParent(numbersHolder.transform, false);
            Text nTxt = numObj.AddComponent<Text>();
            nTxt.text = i.ToString();
            nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nTxt.fontSize = 15;
            nTxt.fontStyle = FontStyle.Bold;
            nTxt.alignment = TextAnchor.UpperLeft;
            nTxt.color = Color.gray;
            nTxt.raycastTarget = false;
        }

        return root;
    }

    private static GameObject BuildInventoryUI(
        Transform canvasTransform, int targetLabLevel,
        Sprite trashClosed, Sprite trashOpen,
        out Transform slotsGrid, out GameObject itemInfoUI, out GameObject trashAlertUI)
    {
        GameObject panelObj = new GameObject("Inventory_Screen_UI");
        panelObj.transform.SetParent(canvasTransform, false);
        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        panelRt.sizeDelta = new Vector2(640f, 410f);
        panelRt.anchoredPosition = new Vector2(-280f, 50f);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.11f, 0.10f, 0.94f);

        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.45f, 0.36f, 0.24f, 0.9f);
        outline.effectDistance = new Vector2(3, -3);

        // Header
        GameObject headerObj = new GameObject("Header_Title");
        headerObj.transform.SetParent(panelObj.transform, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1);
        headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.anchoredPosition = new Vector2(0, -10f);
        headerRt.sizeDelta = new Vector2(0, 36f);

        Text headerText = headerObj.AddComponent<Text>();
        headerText.text = "INVENTORY (KHO CHỨA ĐỒ)";
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 19;
        headerText.fontStyle = FontStyle.Bold;
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.color = new Color(0.95f, 0.85f, 0.55f);

        // Grid 21 ô (7x3)
        GameObject gridObj = new GameObject("Slots_Grid");
        gridObj.transform.SetParent(panelObj.transform, false);
        RectTransform gridRt = gridObj.AddComponent<RectTransform>();
        gridRt.anchoredPosition = new Vector2(0, 15f);
        gridRt.sizeDelta = new Vector2(580f, 250f);

        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(72f, 72f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7;
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 1; i <= 21; i++)
        {
            GameObject slotObj = new GameObject("Slot_" + i);
            slotObj.tag = "Slot";
            slotObj.transform.SetParent(gridObj.transform, false);
            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.20f, 0.19f, 0.18f, 0.95f);
            slotObj.AddComponent<ItemSlot>();
        }
        slotsGrid = gridObj.transform;

        // ItemInfoUI (Hộp thông tin giấy da bên dưới Inventory - Lab 6+)
        itemInfoUI = new GameObject("ItemInfoUI");
        itemInfoUI.transform.SetParent(panelObj.transform, false);
        RectTransform infoRt = itemInfoUI.AddComponent<RectTransform>();
        infoRt.anchoredPosition = new Vector2(-45f, -295f);
        infoRt.sizeDelta = new Vector2(530f, 145f);

        Image infoBg = itemInfoUI.AddComponent<Image>();
        infoBg.color = new Color(0.78f, 0.71f, 0.58f, 0.96f); // Màu giấy da giống hình PDF Lab 7/8

        Outline infoBorder = itemInfoUI.AddComponent<Outline>();
        infoBorder.effectColor = new Color(0.25f, 0.18f, 0.10f, 0.95f);
        infoBorder.effectDistance = new Vector2(3, -3);

        CreateInfoTextChild(itemInfoUI.transform, "itemName", new Vector2(18f, -14f), new Vector2(490f, 32f), 22, FontStyle.Bold, new Color(0.12f, 0.08f, 0.04f));
        CreateInfoTextChild(itemInfoUI.transform, "itemDescription", new Vector2(18f, -50f), new Vector2(490f, 44f), 15, FontStyle.Normal, new Color(0.18f, 0.14f, 0.10f));
        CreateInfoTextChild(itemInfoUI.transform, "itemFunctionality", new Vector2(18f, -102f), new Vector2(490f, 30f), 14, FontStyle.Italic, new Color(0.32f, 0.25f, 0.18f));
        itemInfoUI.SetActive(false);

        // TrashAlertUI (Hộp thoại xác nhận xóa đồ Yes/No - Lab 6+)
        trashAlertUI = new GameObject("TrashAlertUI");
        trashAlertUI.transform.SetParent(panelObj.transform, false);
        RectTransform alertRt = trashAlertUI.AddComponent<RectTransform>();
        alertRt.anchoredPosition = new Vector2(0, 0);
        alertRt.sizeDelta = new Vector2(380f, 150f);

        Image alertBg = trashAlertUI.AddComponent<Image>();
        alertBg.color = new Color(0.10f, 0.10f, 0.12f, 0.97f);

        GameObject alertTxtObj = new GameObject("Text");
        alertTxtObj.transform.SetParent(trashAlertUI.transform, false);
        RectTransform aTxtRt = alertTxtObj.AddComponent<RectTransform>();
        aTxtRt.anchoredPosition = new Vector2(0, 28f);
        aTxtRt.sizeDelta = new Vector2(340f, 50f);
        Text aTxt = alertTxtObj.AddComponent<Text>();
        aTxt.text = "Throw away this item?";
        aTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        aTxt.fontSize = 18;
        aTxt.fontStyle = FontStyle.Bold;
        aTxt.alignment = TextAnchor.MiddleCenter;
        aTxt.color = Color.white;

        CreateUIButton(trashAlertUI.transform, "yes", "YES", new Vector2(-75f, -35f), new Vector2(110f, 40f), new Color(0.75f, 0.20f, 0.20f));
        CreateUIButton(trashAlertUI.transform, "no", "NO", new Vector2(75f, -35f), new Vector2(110f, 40f), new Color(0.28f, 0.32f, 0.36f));
        trashAlertUI.SetActive(false);

        // TrashSlot ở góc dưới bên phải của Inventory (Lab 6+)
        GameObject trashObj = new GameObject("TrashSlot");
        trashObj.transform.SetParent(panelObj.transform, false);
        RectTransform trashRt = trashObj.AddComponent<RectTransform>();
        trashRt.anchorMin = new Vector2(1, 0);
        trashRt.anchorMax = new Vector2(1, 0);
        trashRt.pivot = new Vector2(1, 0);
        trashRt.anchoredPosition = new Vector2(-24f, 16f);
        trashRt.sizeDelta = new Vector2(58f, 58f);

        Image trashSlotBg = trashObj.AddComponent<Image>();
        trashSlotBg.color = new Color(0.22f, 0.18f, 0.18f, 0.95f);

        GameObject trashBgChild = new GameObject("background");
        trashBgChild.transform.SetParent(trashObj.transform, false);
        RectTransform tbgRt = trashBgChild.AddComponent<RectTransform>();
        tbgRt.anchorMin = Vector2.zero;
        tbgRt.anchorMax = Vector2.one;
        tbgRt.offsetMin = new Vector2(6, 6);
        tbgRt.offsetMax = new Vector2(-6, -6);
        Image tIcon = trashBgChild.AddComponent<Image>();
        tIcon.sprite = trashClosed;

        TrashSlot ts = trashObj.AddComponent<TrashSlot>();
        ts.trashAlertUI = trashAlertUI;
        ts.trash_closed = trashClosed;
        ts.trash_opened = trashOpen;
        trashObj.SetActive(targetLabLevel >= 6);

        return panelObj;
    }

    private static GameObject CreateInfoTextChild(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color col)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Text txt = obj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.UpperLeft;
        txt.color = col;
        txt.raycastTarget = false;
        return obj;
    }

    private static Button CreateUIButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgCol)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgCol;

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        Text txt = txtObj.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 16;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return btn;
    }

    private static void BuildCraftingUI(Transform canvasTransform, Sprite axeIcon, out GameObject craftingScreenUI, out GameObject toolsScreenUI)
    {
        // 1. Main Crafting Category Screen
        craftingScreenUI = new GameObject("Crafting_Screen_UI");
        craftingScreenUI.transform.SetParent(canvasTransform, false);
        RectTransform cRt = craftingScreenUI.AddComponent<RectTransform>();
        cRt.sizeDelta = new Vector2(460f, 380f);
        cRt.anchoredPosition = new Vector2(340f, 50f);

        Image cBg = craftingScreenUI.AddComponent<Image>();
        cBg.color = new Color(0.12f, 0.11f, 0.10f, 0.94f);

        Outline cOut = craftingScreenUI.AddComponent<Outline>();
        cOut.effectColor = new Color(0.45f, 0.36f, 0.24f, 0.9f);
        cOut.effectDistance = new Vector2(3, -3);

        GameObject cTitle = new GameObject("Title");
        cTitle.transform.SetParent(craftingScreenUI.transform, false);
        RectTransform ctRt = cTitle.AddComponent<RectTransform>();
        ctRt.anchoredPosition = new Vector2(0, 145f);
        ctRt.sizeDelta = new Vector2(400f, 40f);
        Text ctTxt = cTitle.AddComponent<Text>();
        ctTxt.text = "🔨 CRAFTING SYSTEM [Phím C]";
        ctTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ctTxt.fontSize = 20;
        ctTxt.fontStyle = FontStyle.Bold;
        ctTxt.alignment = TextAnchor.MiddleCenter;
        ctTxt.color = new Color(0.95f, 0.85f, 0.55f);

        CreateUIButton(craftingScreenUI.transform, "ToolsButton", "⚒️ TOOLS (CÔNG CỤ SINH TỒN)", new Vector2(0, 20f), new Vector2(320f, 65f), new Color(0.28f, 0.45f, 0.26f));

        // 2. Tools Sub-Screen (Chế tạo Stone Axe)
        toolsScreenUI = new GameObject("Tools_Screen_UI");
        toolsScreenUI.transform.SetParent(canvasTransform, false);
        RectTransform tRt = toolsScreenUI.AddComponent<RectTransform>();
        tRt.sizeDelta = new Vector2(460f, 380f);
        tRt.anchoredPosition = new Vector2(340f, 50f);

        Image tBg = toolsScreenUI.AddComponent<Image>();
        tBg.color = new Color(0.12f, 0.11f, 0.10f, 0.94f);

        GameObject tTitle = new GameObject("Title");
        tTitle.transform.SetParent(toolsScreenUI.transform, false);
        RectTransform ttRt = tTitle.AddComponent<RectTransform>();
        ttRt.anchoredPosition = new Vector2(0, 145f);
        ttRt.sizeDelta = new Vector2(400f, 40f);
        Text ttTxt = tTitle.AddComponent<Text>();
        ttTxt.text = "⚒️ TOOLS - CHẾ TẠO CÔNG CỤ";
        ttTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ttTxt.fontSize = 20;
        ttTxt.fontStyle = FontStyle.Bold;
        ttTxt.alignment = TextAnchor.MiddleCenter;
        ttTxt.color = new Color(0.95f, 0.85f, 0.55f);

        CreateUIButton(toolsScreenUI.transform, "BackButton", "← Quay lại", new Vector2(-150f, 145f), new Vector2(110f, 34f), new Color(0.30f, 0.30f, 0.32f));

        // Card Stone Axe
        GameObject axeCard = new GameObject("Axe");
        axeCard.transform.SetParent(toolsScreenUI.transform, false);
        RectTransform axeRt = axeCard.AddComponent<RectTransform>();
        axeRt.anchoredPosition = new Vector2(0, -10f);
        axeRt.sizeDelta = new Vector2(410f, 220f);

        Image cardBg = axeCard.AddComponent<Image>();
        cardBg.color = new Color(0.20f, 0.19f, 0.18f, 0.95f);

        GameObject iconObj = new GameObject("AxeIcon");
        iconObj.transform.SetParent(axeCard.transform, false);
        RectTransform iRt = iconObj.AddComponent<RectTransform>();
        iRt.anchoredPosition = new Vector2(-135f, 25f);
        iRt.sizeDelta = new Vector2(92f, 92f);
        Image aImg = iconObj.AddComponent<Image>();
        aImg.sprite = axeIcon;

        CreateInfoTextChild(axeCard.transform, "AxeTitle", new Vector2(130f, -18f), new Vector2(260f, 30f), 20, FontStyle.Bold, Color.white).GetComponent<Text>().text = "Stone Axe (Rìu Đá)";
        CreateInfoTextChild(axeCard.transform, "req1", new Vector2(130f, -56f), new Vector2(250f, 28f), 17, FontStyle.Bold, Color.white).GetComponent<Text>().text = "3 Stone [0]";
        CreateInfoTextChild(axeCard.transform, "req2", new Vector2(130f, -86f), new Vector2(250f, 28f), 17, FontStyle.Bold, Color.white).GetComponent<Text>().text = "3 Stick [0]";

        CreateUIButton(axeCard.transform, "Button", "CRAFT (CHẾ TẠO)", new Vector2(0, -72f), new Vector2(220f, 44f), new Color(0.22f, 0.65f, 0.28f));

        craftingScreenUI.SetActive(false);
        toolsScreenUI.SetActive(false);
    }

    private static GameObject BuildPickupAlertUI(Transform canvasTransform, out Text pickupNameText, out Image pickupIconImg)
    {
        GameObject alert = new GameObject("PickupAlert_UI");
        alert.transform.SetParent(canvasTransform, false);
        RectTransform rt = alert.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-28f, 125f);
        rt.sizeDelta = new Vector2(290f, 76f);

        Image bg = alert.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.12f, 0.10f, 0.92f);

        Outline ol = alert.AddComponent<Outline>();
        ol.effectColor = new Color(0.35f, 0.78f, 0.35f, 0.9f);
        ol.effectDistance = new Vector2(2, -2);

        GameObject iconObj = new GameObject("pickupImage");
        iconObj.transform.SetParent(alert.transform, false);
        RectTransform iRt = iconObj.AddComponent<RectTransform>();
        iRt.anchoredPosition = new Vector2(-102f, 0);
        iRt.sizeDelta = new Vector2(56f, 56f);
        pickupIconImg = iconObj.AddComponent<Image>();

        GameObject txtObj = new GameObject("pickupName");
        txtObj.transform.SetParent(alert.transform, false);
        RectTransform tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(28f, 0);
        tRt.sizeDelta = new Vector2(185f, 56f);
        pickupNameText = txtObj.AddComponent<Text>();
        pickupNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pickupNameText.fontSize = 18;
        pickupNameText.fontStyle = FontStyle.Bold;
        pickupNameText.alignment = TextAnchor.MiddleLeft;
        pickupNameText.color = new Color(0.55f, 1.0f, 0.55f);

        alert.SetActive(false);
        return alert;
    }

    private static void BuildControlsHintBanner(Transform canvasTransform, int targetLabLevel)
    {
        GameObject banner = new GameObject("Controls_Hint_Banner");
        banner.transform.SetParent(canvasTransform, false);
        RectTransform rt = banner.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(450f, 70f);

        Image bg = banner.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.10f, 0.12f, 0.78f);
        bg.raycastTarget = false;

        GameObject txtObj = new GameObject("HintText");
        txtObj.transform.SetParent(banner.transform, false);
        RectTransform tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(12, 6);
        tRt.offsetMax = new Vector2(-12, -6);

        Text txt = txtObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 13;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.color = new Color(0.95f, 0.92f, 0.75f);
        txt.raycastTarget = false;

        if (targetLabLevel <= 3)
            txt.text = "🎮 [WASD] Di chuyển | [E] Nhặt đồ | [TAB / I] Kho đồ UI";
        else if (targetLabLevel == 4)
            txt.text = "🎮 [E] Nhặt đồ | [TAB / I] Kho đồ | [C] Chế tạo (Crafting)";
        else if (targetLabLevel <= 6)
            txt.text = "🎮 [E] Nhặt đồ | [TAB] Kho đồ (Chuột phải: Ăn / Kéo vào Thùng rác)\n🔨 [C] Chế tạo (Crafting) | [N] Test giảm máu";
        else
            txt.text = "🎮 [E] Nhặt | [TAB] Kho đồ | [C] Chế tạo (Crafting)\n⚔️ Phím [1..7] Cầm Rìu Đá (QuickSlots) | [Chuột Trái] Chặt";
    }

    // ==================== GẮN SCRIPTS LÊN PLAYER & CAMERA ====================

    private static void WirePlayerSystems(
        int targetLabLevel,
        Sprite stickIcon, Sprite stoneIcon, Sprite mushroomIcon, Sprite berryIcon, Sprite axeIcon,
        GameObject baseUIItemPrefab,
        GameObject interactionUI, Image centerDotImg, Image handIconImg,
        GameObject inventoryUI, Transform slotsGrid, GameObject itemInfoUI,
        GameObject pickupAlertUI, Text pickupNameText, Image pickupIconImg,
        GameObject craftingScreenUI, GameObject toolsScreenUI,
        GameObject quickSlotsPanel, GameObject numbersHolder,
        HealthBar hb, CaloriesBar cb, HydrationBar hyb)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) return;

        // 1. InventorySystem
        InventorySystem inv = player.GetComponent<InventorySystem>();
        if (inv == null) inv = player.AddComponent<InventorySystem>();
        inv.inventoryScreenUI = inventoryUI;
        inv.slotsParent = slotsGrid;
        inv.uiItemPrefab = baseUIItemPrefab;
        inv.ItemInfoUI = (targetLabLevel >= 6) ? itemInfoUI : null;
        inv.pickupAlert = (targetLabLevel >= 5) ? pickupAlertUI : null;
        inv.pickupName = pickupNameText;
        inv.pickupImage = pickupIconImg;
        inv.stickIcon = stickIcon;
        inv.stoneIcon = stoneIcon;
        inv.mushroomIcon = mushroomIcon;
        inv.berryIcon = berryIcon;
        inv.axeIcon = axeIcon;
        inv.PopulateSlotList();

        // 2. CraftingSystem (Lab 4+)
        CraftingSystem craft = player.GetComponent<CraftingSystem>();
        if (targetLabLevel >= 4)
        {
            if (craft == null) craft = player.AddComponent<CraftingSystem>();
            craft.craftingScreenUI = craftingScreenUI;
            craft.toolsScreenUI = toolsScreenUI;
        }
        else if (craft != null)
        {
            Object.DestroyImmediate(craft);
        }

        // 3. PlayerState (Lab 5+)
        PlayerState ps = player.GetComponent<PlayerState>();
        if (targetLabLevel >= 5)
        {
            if (ps == null) ps = player.AddComponent<PlayerState>();
            ps.playerBody = player;
            if (hb != null) hb.playerState = player;
            if (cb != null) cb.playerState = player;
            if (hyb != null) hyb.playerState = player;
        }
        else if (ps != null)
        {
            Object.DestroyImmediate(ps);
        }

        // 4. Camera, SelectionManager & ToolHolder
        Camera cam = player.GetComponentInChildren<Camera>();
        GameObject toolHolderObj = null;
        if (cam != null)
        {
            SelectionManager sm = cam.GetComponent<SelectionManager>();
            if (sm == null) sm = cam.gameObject.AddComponent<SelectionManager>();
            sm.interaction_Info_UI = interactionUI;
            sm.centerDotImage = (targetLabLevel >= 5) ? centerDotImg : null;
            sm.handIcon = (targetLabLevel >= 5) ? handIconImg : null;
            sm.maxDistance = 4.5f;

            Transform existingTh = cam.transform.Find("ToolHolder");
            if (existingTh != null) Object.DestroyImmediate(existingTh.gameObject);

            toolHolderObj = new GameObject("ToolHolder");
            toolHolderObj.transform.SetParent(cam.transform, false);
            toolHolderObj.transform.localPosition = Vector3.zero;
            toolHolderObj.transform.localRotation = Quaternion.identity;
        }

        // 5. EquipSystem (Lab 7-8)
        EquipSystem eq = player.GetComponent<EquipSystem>();
        if (targetLabLevel >= 7)
        {
            if (eq == null) eq = player.AddComponent<EquipSystem>();
            eq.quickSlotsPanel = quickSlotsPanel;
            eq.numbersHolder = numbersHolder;
            eq.toolHolder = toolHolderObj;
            eq.PopulateSlotList();
        }
        else if (eq != null)
        {
            Object.DestroyImmediate(eq);
        }

        EditorUtility.SetDirty(player);
    }

    private static void PopulateStarterInventoryItems(Transform slotsGrid, Transform quickSlotsPanel, int targetLabLevel)
    {
        // Đặt sẵn 3 Stone + 3 Stick (để mở C là chế tạo được Stone Axe ngay!) + 1 Mushroom + 1 Berries (để chuột phải ăn thử ngay!)
        string[] starterItems = { "Stone", "Stone", "Stone", "Stick", "Stick", "Stick", "Mushroom", "Berries" };

        for (int i = 0; i < starterItems.Length && i < slotsGrid.childCount; i++)
        {
            Transform slot = slotsGrid.GetChild(i);
            GameObject resPrefab = Resources.Load<GameObject>(starterItems[i]);
            if (resPrefab != null)
            {
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(resPrefab);
                item.name = starterItems[i];
                item.transform.SetParent(slot, false);
                item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                DragDrop dd = item.GetComponent<DragDrop>();
                if (dd != null) dd.currentSlot = slot.GetComponent<ItemSlot>();

                InventoryItem invItem = item.GetComponent<InventoryItem>();
                if (invItem != null) invItem.EnsureDefaultMetadata();
            }
        }

        // Ở Lab 7 & 8: Đặt sẵn 1 chiếc Rìu Đá (Axe) vào QuickSlot_1 để bấm phím [1] là cầm rìu trên tay ngay!
        if (targetLabLevel >= 7 && quickSlotsPanel.childCount > 0)
        {
            Transform qSlot1 = quickSlotsPanel.GetChild(0);
            GameObject axePrefab = Resources.Load<GameObject>("Axe");
            if (axePrefab != null)
            {
                GameObject axeItem = (GameObject)PrefabUtility.InstantiatePrefab(axePrefab);
                axeItem.name = "Axe";
                axeItem.transform.SetParent(qSlot1, false);
                axeItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                DragDrop dd = axeItem.GetComponent<DragDrop>();
                if (dd != null) dd.currentSlot = qSlot1.GetComponent<ItemSlot>();

                InventoryItem invItem = axeItem.GetComponent<InventoryItem>();
                if (invItem != null)
                {
                    invItem.isInsideQuickSlot = true;
                    invItem.EnsureDefaultMetadata();
                }
            }
        }

        InventorySystem inv = Object.FindAnyObjectByType<InventorySystem>();
        if (inv != null) inv.ReCalculateList();
    }

    public static void SetupAndPreviewOnly()
    {
        SetupCompleteSurvivalScene(8);
        RenderVerificationScreenshots();
    }

    public static void RenderVerificationScreenshots()
    {
        string outDir = @"C:\Users\admin\.gemini\antigravity\brain\b1e50bae-5caa-4496-b35b-e42c12a468ee\scratch";
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null) return;

        Transform camTr = cam.transform;
        Transform oldParent = camTr.parent;
        Vector3 oldLocalPos = camTr.localPosition;
        Quaternion oldLocalRot = camTr.localRotation;

        bool prevAsync = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;

        camTr.SetParent(null);

        // Warm-up render để đảm bảo mọi biến thể Shader URP, Mây và Terrain Splatmap đã nạp hoàn tất trước khi chụp
        RenderSingleShot(cam, new Vector3(183f, 9.9f, 200f), Quaternion.Euler(4f, 90f, 0f),
            Path.Combine(outDir, "shot1_player_view.png"));

        // 1. Góc nhìn thứ nhất của người chơi từ cổng Căn cứ (183, 9.8, 200) nhìn thẳng vào Lửa trại, Nhà gỗ & Cầu
        RenderSingleShot(cam, new Vector3(183f, 9.9f, 200f), Quaternion.Euler(4f, 90f, 0f),
            Path.Combine(outDir, "shot1_player_view.png"));

        // 2. Góc nhìn Toàn cảnh Khu Căn Cứ Sinh Tồn (nhìn từ trên cao hướng Tây Nam xuống Nhà gỗ house1, house2, Hut, Shelter, Lửa trại & Cầu)
        RenderSingleShot(cam, new Vector3(178f, 18.5f, 184f), Quaternion.Euler(22f, 52f, 0f),
            Path.Combine(outDir, "shot2_camp_overview.png"));

        // 3. Góc nhìn Sông, Hồ Nước & Cầu Gỗ Cong (nhìn từ bờ hồ phía Nam lên Cầu Gỗ & Căn cứ)
        RenderSingleShot(cam, new Vector3(218f, 11.8f, 168f), Quaternion.Euler(12f, 25f, 0f),
            Path.Combine(outDir, "shot3_river_bridge_lake.png"));

        // 4. Góc nhìn Cận cảnh Nhà Gỗ 3D & Khu Chế Tác
        RenderSingleShot(cam, new Vector3(192f, 10.5f, 193f), Quaternion.Euler(6f, 35f, 0f),
            Path.Combine(outDir, "shot4_houses_closeup.png"));

        camTr.SetParent(oldParent, false);
        camTr.localPosition = oldLocalPos;
        camTr.localRotation = oldLocalRot;
        ShaderUtil.allowAsyncCompilation = prevAsync;
    }

    private static void RenderSingleShot(Camera cam, Vector3 pos, Quaternion rot, string filePath)
    {
        cam.transform.position = pos;
        cam.transform.rotation = rot;

        int w = 1280;
        int h = 720;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevTarget = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log("📸 Đã chụp ảnh kiểm tra bản đồ tại: " + filePath);
    }
}
#endif
