#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class SetupLab3Scene
{
    private const string ProjectRoot = "Assets";
    private const string PrefabsFolder = ProjectRoot + "/Prefabs";
    private const string TexturesFolder = ProjectRoot + "/Textures";
    private const string ScenesFolder = ProjectRoot + "/Scenes";

    // Menu item đã được tinh gọn vào SetupLabs4To8 -> "Nộp Bài Từng Lab"
    public static void RunFullSetupLab3()
    {
        Debug.Log(">>> [Lab 3] BẮT ĐẦU THIẾT LẬP KHO CHỨA HÀNG GAME UI (MSSV: 2312690) <<<");

        string scenePath = ScenesFolder + "/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Tạo Icons cho vật phẩm
        Sprite stickIcon = CreateItemIconSprite("Icon_Stick", new Color(0.55f, 0.35f, 0.15f), "Stick");
        Sprite rockIcon = CreateItemIconSprite("Icon_Rock", new Color(0.45f, 0.48f, 0.50f), "Rock");
        Sprite mushroomIcon = CreateItemIconSprite("Icon_Mushroom", new Color(0.85f, 0.20f, 0.20f), "Shroom");
        Sprite berryIcon = CreateItemIconSprite("Icon_Berry", new Color(0.85f, 0.10f, 0.35f), "Berry");

        // Gán Icon vào các Pickup Prefabs trong thế giới
        AssignIconToPrefab(PrefabsFolder + "/Pickup_Stick.prefab", stickIcon);
        AssignIconToPrefab(PrefabsFolder + "/Pickup_Rock.prefab", rockIcon);
        AssignIconToPrefab(PrefabsFolder + "/Pickup_Mushroom.prefab", mushroomIcon);
        AssignIconToPrefab(PrefabsFolder + "/Pickup_Berry.prefab", berryIcon);

        // 2. Tạo UI Item Prefab (dùng để spawn bên trong các ô Inventory)
        GameObject uiItemPrefab = CreateUIItemPrefab();

        // 3. Tạo Canvas & EventSystem
        Canvas canvas = EnsureCanvas();
        EnsureEventSystem();

        // 4. Tạo Giao diện Inventory UI Panel với Grid các ô Slot
        GameObject inventoryUI = BuildInventoryUIPanel(canvas.transform, uiItemPrefab, out Transform slotsGrid);

        // 5. Cấu hình InventorySystem trên Player
        SetupInventorySystemOnPlayer(inventoryUI, slotsGrid, uiItemPrefab);

        // 6. Thêm sẵn 3 vật phẩm mẫu vào các ô đầu tiên để người chơi mở túi là test được kéo thả ngay
        AddStarterItemsToSlots(slotsGrid, uiItemPrefab, stickIcon, rockIcon, mushroomIcon);

        // Lưu lại Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("🎉 [Lab 3] HOÀN TẤT THIẾT LẬP THÀNH CÔNG! Kho chứa UI, Drag and Drop và liên kết nhặt đồ đã sẵn sàng!");
    }

    // Menu item đã được tinh gọn vào SetupLabs4To8 -> "Nộp Bài Từng Lab"
    public static void BuildExecutableLab3()
    {
        RunFullSetupLab3();

        string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Build_Lab3"));
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

        string exePath = Path.Combine(buildDir, "2312690_3DSurvivalGame.exe");
        Debug.Log(">>> [Lab 3] Bắt đầu Build Standalone Windows EXE tại: " + exePath);

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
            Debug.Log("🎉 [Lab 3] BUILD THÀNH CÔNG! File thực thi tại: " + exePath);
        }
        else
        {
            Debug.LogError("❌ [Lab 3] BUILD THẤT BẠI: " + report.summary.result);
        }
    }

    private static Sprite CreateItemIconSprite(string name, Color color, string tag)
    {
        string path = TexturesFolder + "/" + name + ".png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            int size = 128;
            tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color borderCol = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            Color darkCol = color * 0.7f;
            Color highlightCol = Color.Lerp(color, Color.white, 0.35f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size * 0.5f) / (size * 0.5f);
                    float dy = (y - size * 0.5f) / (size * 0.5f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 0.92f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > 0.82f)
                    {
                        tex.SetPixel(x, y, borderCol);
                    }
                    else
                    {
                        float blend = (dy + 1f) * 0.5f;
                        Color c = Color.Lerp(darkCol, highlightCol, blend);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void AssignIconToPrefab(string prefabPath, Sprite icon)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            InteractableObject io = prefab.GetComponent<InteractableObject>();
            if (io != null)
            {
                io.itemIcon = icon;
                EditorUtility.SetDirty(prefab);
            }
        }
    }

    private static GameObject CreateUIItemPrefab()
    {
        string path = PrefabsFolder + "/UI_Item.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        GameObject root = new GameObject("UI_Item");
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(68f, 68f);
        rt.anchoredPosition = Vector2.zero;

        Image img = root.AddComponent<Image>();
        img.color = Color.white;

        CanvasGroup cg = root.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;

        root.AddComponent<DragDrop>();

        // Text label bên dưới
        GameObject textObj = new GameObject("Text_Name");
        textObj.transform.SetParent(root.transform);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 0.35f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = "Item";
        text.fontSize = 11;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;

        // Shadow cho text
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(1, -1);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Canvas EnsureCanvas()
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

    private static GameObject BuildInventoryUIPanel(Transform canvasTransform, GameObject uiItemPrefab, out Transform slotsGridTransform)
    {
        Transform existing = canvasTransform.Find("Inventory_Screen_UI");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // 1. Root Panel
        GameObject panelObj = new GameObject("Inventory_Screen_UI");
        panelObj.transform.SetParent(canvasTransform);
        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        panelRt.sizeDelta = new Vector2(680f, 440f);
        panelRt.anchoredPosition = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.10f, 0.12f, 0.90f);

        // 2. Title Header
        GameObject headerObj = new GameObject("Header_Title");
        headerObj.transform.SetParent(panelObj.transform);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1);
        headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.anchoredPosition = new Vector2(0, -15f);
        headerRt.sizeDelta = new Vector2(0, 45f);

        Text headerText = headerObj.AddComponent<Text>();
        headerText.text = "🎒 KHO CHỨA HÀNG (INVENTORY) - NHẤN [TAB] ĐỂ ĐÓNG";
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 20;
        headerText.fontStyle = FontStyle.Bold;
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.color = new Color(1.0f, 0.85f, 0.35f);

        // Subtitle hướng dẫn kéo thả
        GameObject subObj = new GameObject("Sub_Instruction");
        subObj.transform.SetParent(panelObj.transform);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0, 1);
        subRt.anchorMax = new Vector2(1, 1);
        subRt.pivot = new Vector2(0.5f, 1);
        subRt.anchoredPosition = new Vector2(0, -55f);
        subRt.sizeDelta = new Vector2(0, 25f);

        Text subText = subObj.AddComponent<Text>();
        subText.text = "💡 Nhấn giữ chuột trái để KÉO & THẢ các vật phẩm qua lại giữa các ô chứa";
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = 13;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.85f, 0.85f, 0.85f);

        // 3. Grid Container chứa các ô Slot
        GameObject gridObj = new GameObject("Slots_Grid");
        gridObj.transform.SetParent(panelObj.transform);
        RectTransform gridRt = gridObj.AddComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.5f, 0.5f);
        gridRt.anchorMax = new Vector2(0.5f, 0.5f);
        gridRt.pivot = new Vector2(0.5f, 0.5f);
        gridRt.anchoredPosition = new Vector2(0, -20f);
        gridRt.sizeDelta = new Vector2(620f, 290f);

        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(76f, 76f);
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7; // 7 cột x 3 hàng = 21 ô chứa
        grid.childAlignment = TextAnchor.MiddleCenter;

        // 4. Sinh 21 ô ItemSlot
        for (int i = 1; i <= 21; i++)
        {
            GameObject slotObj = new GameObject("Slot_" + i);
            slotObj.transform.SetParent(gridObj.transform);
            RectTransform slotRt = slotObj.AddComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(76f, 76f);

            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.18f, 0.22f, 0.26f, 0.85f);

            slotObj.AddComponent<ItemSlot>();
        }

        slotsGridTransform = gridObj.transform;
        return panelObj;
    }

    private static void SetupInventorySystemOnPlayer(GameObject inventoryUI, Transform slotsGrid, GameObject uiItemPrefab)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");

        if (player != null)
        {
            InventorySystem invSys = player.GetComponent<InventorySystem>();
            if (invSys == null) invSys = player.AddComponent<InventorySystem>();

            invSys.inventoryScreenUI = inventoryUI;
            invSys.slotsParent = slotsGrid;
            invSys.uiItemPrefab = uiItemPrefab;
            invSys.toggleKey = KeyCode.Tab;
            invSys.secondaryToggleKey = KeyCode.I;

            invSys.PopulateSlotList();
            Debug.Log(">>> Đã cấu hình InventorySystem trên Player với " + invSys.slotList.Count + " ô chứa.");
        }
    }

    private static void AddStarterItemsToSlots(Transform slotsGrid, GameObject uiItemPrefab, Sprite stick, Sprite rock, Sprite shroom)
    {
        if (slotsGrid.childCount < 3) return;

        // Slot 1: Cành cây
        SpawnItemInSlot(slotsGrid.GetChild(0), uiItemPrefab, "Cành cây (Stick)", stick);
        // Slot 2: Hòn đá
        SpawnItemInSlot(slotsGrid.GetChild(1), uiItemPrefab, "Hòn đá (Stone)", rock);
        // Slot 3: Nấm rừng
        SpawnItemInSlot(slotsGrid.GetChild(2), uiItemPrefab, "Nấm rừng (Mushroom)", shroom);
    }

    private static void SpawnItemInSlot(Transform slotTransform, GameObject prefab, string name, Sprite icon)
    {
        if (slotTransform.childCount > 0) return;

        GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        item.name = name;
        item.transform.SetParent(slotTransform);
        RectTransform rt = item.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = Vector2.zero;

        Image img = item.GetComponent<Image>();
        if (img != null && icon != null) img.sprite = icon;

        Text text = item.GetComponentInChildren<Text>();
        if (text != null) text.text = name;

        DragDrop dd = item.GetComponent<DragDrop>();
        if (dd != null)
        {
            dd.itemName = name;
            dd.currentSlot = slotTransform.GetComponent<ItemSlot>();
        }

        ItemSlot slot = slotTransform.GetComponent<ItemSlot>();
        if (slot != null) slot.isOccupied = true;
    }
}
#endif
