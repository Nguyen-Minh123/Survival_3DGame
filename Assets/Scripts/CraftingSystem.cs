using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CraftingSystem - Quản lý giao diện và logic chế tạo vật phẩm (Lab 4 - Video 7 & 8).
/// Nhấn phím [C] để đóng/mở giao diện Crafting, kiểm tra số lượng nguyên liệu trong Inventory,
/// chế tạo Stone Axe (3 Stone + 3 Stick) và trừ nguyên liệu tương ứng.
/// </summary>
public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance { get; set; }

    [Header("=== Giao Diện Crafting UI ===")]
    public GameObject craftingScreenUI;
    public GameObject toolsScreenUI;

    [Header("=== Danh Sách Vật Phẩm Hiện Có ===")]
    public List<string> inventoryItemList = new List<string>();

    // Category Buttons
    public Button toolsBTN;
    public Button backBTN;

    // Craft Buttons
    public Button craftAxeBTN;

    // Requirement Text
    public Text AxeReq1, AxeReq2;

    public bool isOpen;

    // All Blueprints
    public Blueprint AxeBLP = new Blueprint("Axe", 2, "Stone", 3, "Stick", 3);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        isOpen = false;

        if (craftingScreenUI != null)
        {
            Transform tBtn = craftingScreenUI.transform.Find("ToolsButton");
            if (tBtn != null && toolsBTN == null) toolsBTN = tBtn.GetComponent<Button>();

            if (toolsBTN != null)
            {
                toolsBTN.onClick.RemoveAllListeners();
                toolsBTN.onClick.AddListener(delegate { OpenToolsCategory(); });
            }
            craftingScreenUI.SetActive(false);
        }

        if (toolsScreenUI != null)
        {
            Transform axeNode = toolsScreenUI.transform.Find("Axe");
            if (axeNode != null)
            {
                if (AxeReq1 == null)
                {
                    Transform r1 = axeNode.Find("req1");
                    if (r1 != null) AxeReq1 = r1.GetComponent<Text>();
                }
                if (AxeReq2 == null)
                {
                    Transform r2 = axeNode.Find("req2");
                    if (r2 != null) AxeReq2 = r2.GetComponent<Text>();
                }
                if (craftAxeBTN == null)
                {
                    Transform cBtn = axeNode.Find("Button");
                    if (cBtn != null) craftAxeBTN = cBtn.GetComponent<Button>();
                }
            }

            Transform bBtn = toolsScreenUI.transform.Find("BackButton");
            if (bBtn != null && backBTN == null) backBTN = bBtn.GetComponent<Button>();
            if (backBTN != null)
            {
                backBTN.onClick.RemoveAllListeners();
                backBTN.onClick.AddListener(delegate { BackToMainCrafting(); });
            }

            if (craftAxeBTN != null)
            {
                craftAxeBTN.onClick.RemoveAllListeners();
                craftAxeBTN.onClick.AddListener(delegate { CraftAnyItem(AxeBLP); });
            }

            toolsScreenUI.SetActive(false);
        }

        RefreshNeededItems();
    }

    public void OpenToolsCategory()
    {
        if (craftingScreenUI != null) craftingScreenUI.SetActive(false);
        if (toolsScreenUI != null) toolsScreenUI.SetActive(true);
        RefreshNeededItems();
    }

    public void BackToMainCrafting()
    {
        if (toolsScreenUI != null) toolsScreenUI.SetActive(false);
        if (craftingScreenUI != null) craftingScreenUI.SetActive(true);
    }

    public void CraftAnyItem(Blueprint blueprintToCraft)
    {
        if (InventorySystem.Instance == null) return;

        // Thêm vật phẩm vừa chế tạo vào Inventory
        InventorySystem.Instance.AddToInventory(blueprintToCraft.itemName);

        // Trừ nguyên liệu khỏi Inventory
        if (blueprintToCraft.numOfRequirements == 1)
        {
            InventorySystem.Instance.RemoveItem(blueprintToCraft.Req1, blueprintToCraft.Req1amount);
        }
        else if (blueprintToCraft.numOfRequirements == 2)
        {
            InventorySystem.Instance.RemoveItem(blueprintToCraft.Req1, blueprintToCraft.Req1amount);
            InventorySystem.Instance.RemoveItem(blueprintToCraft.Req2, blueprintToCraft.Req2amount);
        }

        // Cập nhật lại danh sách sau 1 frame
        StartCoroutine(calculate());
    }

    public IEnumerator calculate()
    {
        yield return 0; // Đợi Destroy hoàn tất
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ReCalculateList();
        }
        RefreshNeededItems();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCraftingUI();
        }
    }

    public void ToggleCraftingUI()
    {
        if (!isOpen)
        {
            if (craftingScreenUI != null) craftingScreenUI.SetActive(true);
            if (toolsScreenUI != null) toolsScreenUI.SetActive(false);
            isOpen = true;
            RefreshNeededItems();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (craftingScreenUI != null) craftingScreenUI.SetActive(false);
            if (toolsScreenUI != null) toolsScreenUI.SetActive(false);
            isOpen = false;

            if (InventorySystem.Instance == null || !InventorySystem.Instance.isOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void RefreshNeededItems()
    {
        int stone_count = 0;
        int stick_count = 0;

        if (InventorySystem.Instance != null)
        {
            inventoryItemList = InventorySystem.Instance.itemList;
        }

        foreach (string itemName in inventoryItemList)
        {
            if (itemName == "Stone" || itemName.Contains("Stone") || itemName.Contains("Rock"))
            {
                stone_count += 1;
            }
            else if (itemName == "Stick" || itemName.Contains("Stick"))
            {
                stick_count += 1;
            }
        }

        // Cập nhật Text yêu cầu nguyên liệu cho Stone Axe
        if (AxeReq1 != null)
        {
            AxeReq1.text = "3 Stone [" + stone_count + "]";
            AxeReq1.color = (stone_count >= 3) ? new Color(0.35f, 0.95f, 0.45f) : new Color(1.0f, 0.75f, 0.65f);
        }

        if (AxeReq2 != null)
        {
            AxeReq2.text = "3 Stick [" + stick_count + "]";
            AxeReq2.color = (stick_count >= 3) ? new Color(0.35f, 0.95f, 0.45f) : new Color(1.0f, 0.75f, 0.65f);
        }

        if (craftAxeBTN != null)
        {
            bool canCraft = (stone_count >= 3 && stick_count >= 3) &&
                            (InventorySystem.Instance == null || !InventorySystem.Instance.CheckIfFull());
            craftAxeBTN.gameObject.SetActive(canCraft);
        }
    }
}
