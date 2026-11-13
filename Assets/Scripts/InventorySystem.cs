using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InventorySystem - Quản lý toàn bộ hệ thống kho chứa đồ dạng ô (Slots) trong game (Lab 3 -> Lab 8).
/// Hỗ trợ: Đóng/mở bằng [Tab] hoặc [I], kéo thả vật phẩm, trừ nguyên liệu khi chế tạo (RemoveItem),
/// hiển thị thông báo vừa nhặt đồ (Pickup Alert - Lab 5), và bảng thông tin chi tiết khi rê chuột (ItemInfoUI - Lab 6).
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; set; }

    [Header("=== Giao Diện Túi Đồ UI ===")]
    public GameObject inventoryScreenUI;
    public Transform slotsParent;
    public GameObject uiItemPrefab;

    [Header("=== Giao Diện Thông Tin Vật Phẩm (Lab 6) ===")]
    public GameObject ItemInfoUI;

    [Header("=== Giao Diện Thông Báo Nhặt Đồ (Lab 5) ===")]
    public GameObject pickupAlert;
    public Text pickupName;
    public Image pickupImage;
    private Coroutine pickupAlertCoroutine;

    [Header("=== Kho Icon Mặc Định ===")]
    public Sprite stickIcon;
    public Sprite stoneIcon;
    public Sprite mushroomIcon;
    public Sprite berryIcon;
    public Sprite axeIcon;

    [Header("=== Cấu Hình Phím ===")]
    public KeyCode toggleKey = KeyCode.Tab;
    public KeyCode secondaryToggleKey = KeyCode.I;

    [Header("=== Trạng Thái & Danh Sách ===")]
    public bool isOpen = false;
    public List<GameObject> slotList = new List<GameObject>();
    public List<string> itemList = new List<string>();

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
        PopulateSlotList();
        ReCalculateList();

        if (inventoryScreenUI != null) inventoryScreenUI.SetActive(false);
        if (ItemInfoUI != null) ItemInfoUI.SetActive(false);
        if (pickupAlert != null) pickupAlert.SetActive(false);
    }

    public void PopulateSlotList()
    {
        slotList.Clear();
        if (slotsParent != null)
        {
            foreach (Transform child in slotsParent)
            {
                if (child.GetComponent<ItemSlot>() != null || child.name.StartsWith("Slot"))
                {
                    slotList.Add(child.gameObject);
                }
            }
        }
        else if (inventoryScreenUI != null)
        {
            foreach (Transform child in inventoryScreenUI.transform)
            {
                if (child.CompareTag("Slot") || child.GetComponent<ItemSlot>() != null || child.name.StartsWith("Slot"))
                {
                    slotList.Add(child.gameObject);
                }
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(secondaryToggleKey))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;

        if (inventoryScreenUI != null)
        {
            inventoryScreenUI.SetActive(isOpen);
        }

        if (!isOpen && ItemInfoUI != null)
        {
            ItemInfoUI.SetActive(false);
        }

        if (isOpen)
        {
            ReCalculateList();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (CraftingSystem.Instance == null || !CraftingSystem.Instance.isOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public bool AddToInventory(string itemName, Sprite itemSprite = null)
    {
        if (slotList.Count == 0) PopulateSlotList();

        if (CheckIfFull())
        {
            Debug.Log("Inventory is full!");
            return false;
        }

        string cleanName = NormalizeItemName(itemName);
        if (itemSprite == null) itemSprite = ResolveIconForItem(cleanName);

        GameObject whatSlotToEquip = FindNextEmptySlot();
        if (whatSlotToEquip == null) return false;

        GameObject itemToAdd = null;
        GameObject resPrefab = Resources.Load<GameObject>(cleanName);
        if (resPrefab != null)
        {
            itemToAdd = Instantiate(resPrefab, whatSlotToEquip.transform.position, whatSlotToEquip.transform.rotation);
            itemToAdd.transform.SetParent(whatSlotToEquip.transform, false);
        }
        else if (uiItemPrefab != null)
        {
            itemToAdd = Instantiate(uiItemPrefab, whatSlotToEquip.transform);
        }
        else
        {
            itemToAdd = new GameObject(cleanName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(DragDrop), typeof(InventoryItem));
            itemToAdd.transform.SetParent(whatSlotToEquip.transform, false);
        }

        itemToAdd.name = cleanName;
        RectTransform rect = itemToAdd.GetComponent<RectTransform>();
        if (rect != null) rect.anchoredPosition = Vector2.zero;

        Image img = itemToAdd.GetComponent<Image>();
        if (img != null && itemSprite != null)
        {
            img.sprite = itemSprite;
            img.color = Color.white;
        }

        Text label = itemToAdd.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = cleanName;
        }

        DragDrop dd = itemToAdd.GetComponent<DragDrop>();
        if (dd == null) dd = itemToAdd.AddComponent<DragDrop>();
        dd.itemName = cleanName;
        dd.currentSlot = whatSlotToEquip.GetComponent<ItemSlot>();

        InventoryItem invItem = itemToAdd.GetComponent<InventoryItem>();
        if (invItem == null) invItem = itemToAdd.AddComponent<InventoryItem>();
        invItem.thisName = cleanName;
        invItem.EnsureDefaultMetadata();

        ItemSlot slotComp = whatSlotToEquip.GetComponent<ItemSlot>();
        if (slotComp != null) slotComp.isOccupied = true;

        itemList.Add(cleanName);

        // Hiển thị thông báo nhặt đồ (Lab 5 - Episode 9)
        TriggerPickupPopUp(cleanName, itemSprite);

        // Làm mới bảng Crafting nếu có (Lab 4 - Episode 8)
        if (CraftingSystem.Instance != null)
        {
            CraftingSystem.Instance.RefreshNeededItems();
        }

        return true;
    }

    public void TriggerPickupPopUp(string itemName, Sprite itemSprite)
    {
        if (pickupAlert == null) return;

        pickupAlert.SetActive(true);
        if (pickupName != null) pickupName.text = "+1 " + itemName;
        if (pickupImage != null && itemSprite != null) pickupImage.sprite = itemSprite;

        if (pickupAlertCoroutine != null) StopCoroutine(pickupAlertCoroutine);
        pickupAlertCoroutine = StartCoroutine(HidePickupAlertAfterDelay(2.4f));
    }

    private IEnumerator HidePickupAlertAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (pickupAlert != null) pickupAlert.SetActive(false);
    }

    private GameObject FindNextEmptySlot()
    {
        foreach (GameObject slot in slotList)
        {
            if (slot != null && slot.transform.childCount == 0)
            {
                return slot;
            }
        }
        return null;
    }

    public bool CheckIfFull()
    {
        if (slotList.Count == 0) PopulateSlotList();
        int counter = 0;
        foreach (GameObject slot in slotList)
        {
            if (slot != null && slot.transform.childCount > 0)
            {
                counter += 1;
            }
        }
        return counter >= slotList.Count && slotList.Count > 0;
    }

    public void RemoveItem(string nameToRemove, int amountToRemove)
    {
        int counter = amountToRemove;
        string targetClean = NormalizeItemName(nameToRemove);

        for (int i = slotList.Count - 1; i >= 0; i--)
        {
            if (slotList[i].transform.childCount > 0)
            {
                Transform child = slotList[i].transform.GetChild(0);
                string childClean = NormalizeItemName(child.name);

                if ((childClean == targetClean || child.name == nameToRemove + "(Clone)" || child.name == nameToRemove) && counter != 0)
                {
                    DestroyImmediate(child.gameObject);
                    counter -= 1;
                }
            }
        }

        ReCalculateList();
        if (CraftingSystem.Instance != null)
        {
            CraftingSystem.Instance.RefreshNeededItems();
        }
    }

    public void ReCalculateList()
    {
        itemList.Clear();
        foreach (GameObject slot in slotList)
        {
            if (slot != null && slot.transform.childCount > 0)
            {
                string name = slot.transform.GetChild(0).name;
                string result = NormalizeItemName(name.Replace("(Clone)", ""));
                itemList.Add(result);
            }
        }
    }

    // Alias cho tên hàm trong Pastebin của Mike's Code (Episode 11)
    public void ReCalculeList()
    {
        ReCalculateList();
    }

    public static string NormalizeItemName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string s = raw.Replace("(Clone)", "").Trim();
        if (s.Contains("Stick")) return "Stick";
        if (s.Contains("Stone") || s.Contains("Rock")) return "Stone";
        if (s.Contains("Mushroom")) return "Mushroom";
        if (s.Contains("Berries") || s.Contains("Berry")) return "Berries";
        if (s.Contains("Axe")) return "Axe";
        return s;
    }

    public Sprite ResolveIconForItem(string cleanName)
    {
        if (cleanName == "Stick") return stickIcon;
        if (cleanName == "Stone") return stoneIcon;
        if (cleanName == "Mushroom") return mushroomIcon;
        if (cleanName == "Berries") return berryIcon;
        if (cleanName == "Axe") return axeIcon;
        return null;
    }
}
