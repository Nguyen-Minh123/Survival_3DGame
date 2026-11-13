using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EquipSystem - Quản lý thanh công cụ nhanh QuickSlots (phím 1 -> 7) và hiển thị mô hình 3D công cụ trên tay nhân vật.
/// Theo đúng thiết kế từ Mike's Code 3D Survival Game Tutorial (Lab 7 & Lab 8 - Episode 12 & 13).
/// </summary>
public class EquipSystem : MonoBehaviour
{
    public static EquipSystem Instance { get; set; }

    [Header("=== Giao diện Quick Slots ===")]
    public GameObject quickSlotsPanel;
    public List<GameObject> quickSlotsList = new List<GameObject>();
    public List<string> itemList = new List<string>();

    public GameObject numbersHolder;
    public int selectedNumber = -1;
    public GameObject selectedItem;

    [Header("=== Vị trí Cầm Công Cụ 3D (Tool Holder) ===")]
    public GameObject toolHolder;
    public GameObject selectedItemModel;

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
        PopulateSlotList();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectQuickSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectQuickSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectQuickSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectQuickSlot(4);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectQuickSlot(5);
        else if (Input.GetKeyDown(KeyCode.Alpha6)) SelectQuickSlot(6);
        else if (Input.GetKeyDown(KeyCode.Alpha7)) SelectQuickSlot(7);
    }

    public void SelectQuickSlot(int number)
    {
        if (CheckIfSlotIsFull(number))
        {
            if (selectedNumber != number)
            {
                selectedNumber = number;

                // Bỏ chọn vật phẩm cũ
                if (selectedItem != null)
                {
                    InventoryItem prevItem = selectedItem.GetComponent<InventoryItem>();
                    if (prevItem != null) prevItem.isSelected = false;
                }

                selectedItem = GetSelectedItem(number);
                if (selectedItem != null)
                {
                    InventoryItem curItem = selectedItem.GetComponent<InventoryItem>();
                    if (curItem != null) curItem.isSelected = true;
                }

                SetEquippedModel(selectedItem);

                // Đổi màu số thứ tự trên thanh QuickSlots
                UpdateNumbersHighlight(number);
            }
            else
            {
                // Bấm lại phím số đang chọn -> Cất vũ khí/công cụ
                selectedNumber = -1;
                if (selectedItem != null)
                {
                    InventoryItem prevItem = selectedItem.GetComponent<InventoryItem>();
                    if (prevItem != null) prevItem.isSelected = false;
                    selectedItem = null;
                }

                if (selectedItemModel != null)
                {
                    DestroyImmediate(selectedItemModel.gameObject);
                    selectedItemModel = null;
                }

                UpdateNumbersHighlight(-1);
            }
        }
    }

    private void UpdateNumbersHighlight(int activeNumber)
    {
        if (numbersHolder == null) return;

        foreach (Transform child in numbersHolder.transform)
        {
            Text numText = child.GetComponentInChildren<Text>();
            if (numText != null)
            {
                numText.color = Color.gray;
            }
        }

        if (activeNumber >= 1 && activeNumber <= 7)
        {
            Transform targetNum = numbersHolder.transform.Find("number" + activeNumber);
            if (targetNum == null && activeNumber - 1 < numbersHolder.transform.childCount)
            {
                targetNum = numbersHolder.transform.GetChild(activeNumber - 1);
            }
            if (targetNum != null)
            {
                Text activeText = targetNum.GetComponentInChildren<Text>();
                if (activeText != null) activeText.color = Color.white;
            }
        }
    }

    private void SetEquippedModel(GameObject selectedItem)
    {
        if (selectedItemModel != null)
        {
            DestroyImmediate(selectedItemModel.gameObject);
            selectedItemModel = null;
        }

        if (selectedItem == null) return;

        string selectedItemName = selectedItem.name.Replace("(Clone)", "").Trim();
        if (selectedItemName.Contains("Axe")) selectedItemName = "Axe";

        if (toolHolder == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Transform th = cam.transform.Find("ToolHolder");
                if (th != null) toolHolder = th.gameObject;
            }
        }

        if (toolHolder == null) return;

        GameObject prefab = Resources.Load<GameObject>(selectedItemName + "_Model");
        if (prefab != null)
        {
            selectedItemModel = Instantiate(prefab, toolHolder.transform);
            selectedItemModel.transform.localPosition = new Vector3(0.45f, -0.38f, 0.65f);
            selectedItemModel.transform.localRotation = Quaternion.Euler(10f, -15f, -8f);
            selectedItemModel.transform.localScale = Vector3.one;
        }
        else
        {
            // Fallback tạo trực tiếp mô hình 3D Stone Axe trên tay nếu chưa tìm thấy trong Resources
            selectedItemModel = CreateRuntimeAxeModel(toolHolder.transform);
        }

        if (selectedItemModel.GetComponent<EquippableItem>() == null)
        {
            selectedItemModel.AddComponent<EquippableItem>();
        }
    }

    private GameObject CreateRuntimeAxeModel(Transform parent)
    {
        GameObject root = new GameObject("Axe_Model");
        root.transform.SetParent(parent);
        root.transform.localPosition = new Vector3(0.45f, -0.38f, 0.65f);
        root.transform.localRotation = Quaternion.Euler(10f, -15f, -8f);

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.transform.SetParent(root.transform);
        handle.transform.localPosition = Vector3.zero;
        handle.transform.localScale = new Vector3(0.07f, 0.45f, 0.09f);
        Destroy(handle.GetComponent<Collider>());

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.transform.SetParent(root.transform);
        blade.transform.localPosition = new Vector3(0, 0.28f, 0.14f);
        blade.transform.localScale = new Vector3(0.06f, 0.20f, 0.34f);
        Destroy(blade.GetComponent<Collider>());

        root.AddComponent<Animator>();
        return root;
    }

    private GameObject GetSelectedItem(int slotNumber)
    {
        if (slotNumber - 1 >= 0 && slotNumber - 1 < quickSlotsList.Count)
        {
            GameObject slot = quickSlotsList[slotNumber - 1];
            if (slot.transform.childCount > 0)
            {
                return slot.transform.GetChild(0).gameObject;
            }
        }
        return null;
    }

    private bool CheckIfSlotIsFull(int slotNumber)
    {
        if (quickSlotsList.Count == 0) PopulateSlotList();
        if (slotNumber - 1 >= 0 && slotNumber - 1 < quickSlotsList.Count)
        {
            if (quickSlotsList[slotNumber - 1].transform.childCount > 0)
            {
                return true;
            }
        }
        return false;
    }

    public void PopulateSlotList()
    {
        quickSlotsList.Clear();
        if (quickSlotsPanel == null) return;

        foreach (Transform child in quickSlotsPanel.transform)
        {
            if (child.CompareTag("QuickSlot") || child.GetComponent<ItemSlot>() != null || child.name.StartsWith("QuickSlot"))
            {
                quickSlotsList.Add(child.gameObject);
            }
        }
    }

    public void AddToQuickSlots(GameObject itemToEquip)
    {
        if (quickSlotsList.Count == 0) PopulateSlotList();

        GameObject availableSlot = FindNextEmptySlot();
        if (availableSlot != null)
        {
            itemToEquip.transform.SetParent(availableSlot.transform, false);
            RectTransform rt = itemToEquip.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;

            string cleanName = itemToEquip.name.Replace("(Clone)", "").Trim();
            itemList.Add(cleanName);

            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.ReCalculateList();
            }
        }
    }

    private GameObject FindNextEmptySlot()
    {
        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount == 0)
            {
                return slot;
            }
        }
        return null;
    }

    public bool CheckIfFull()
    {
        if (quickSlotsList.Count == 0) PopulateSlotList();
        int counter = 0;

        foreach (GameObject slot in quickSlotsList)
        {
            if (slot.transform.childCount > 0)
            {
                counter += 1;
            }
        }

        return counter >= 7;
    }
}
