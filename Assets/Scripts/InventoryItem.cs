using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// InventoryItem - Quản lý hiển thị thông tin khi rê chuột (Hover), tiêu thụ vật phẩm bằng Chuột Phải (Consuming),
/// và trang bị nhanh công cụ vào QuickSlots (Lab 6 & Lab 7 - Episode 11 & 12).
/// </summary>
public class InventoryItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("=== Thuộc tính Xóa ===")]
    public bool isTrashable = true;

    [Header("=== Giao diện Thông tin Hover ===")]
    private GameObject itemInfoUI;
    private Text itemInfoUI_itemName;
    private Text itemInfoUI_itemDescription;
    private Text itemInfoUI_itemFunctionality;

    public string thisName, thisDescription, thisFunctionality;

    [Header("=== Thuộc tính Tiêu thụ (Consumable) ===")]
    private GameObject itemPendingConsumption;
    public bool isConsumable;
    public float healthEffect;
    public float caloriesEffect;
    public float hydrationEffect;

    [Header("=== Thuộc tính Trang bị (Equippable - Lab 7/8) ===")]
    public bool isEquippable;
    private GameObject itemPendingEquipping;
    public bool isInsideQuickSlot;
    public bool isSelected;

    private void Start()
    {
        EnsureDefaultMetadata();
        BindItemInfoUI();
    }

    private void BindItemInfoUI()
    {
        if (InventorySystem.Instance != null && InventorySystem.Instance.ItemInfoUI != null)
        {
            itemInfoUI = InventorySystem.Instance.ItemInfoUI;
            Transform n = itemInfoUI.transform.Find("itemName");
            Transform d = itemInfoUI.transform.Find("itemDescription");
            Transform f = itemInfoUI.transform.Find("itemFunctionality");

            if (n != null) itemInfoUI_itemName = n.GetComponent<Text>();
            if (d != null) itemInfoUI_itemDescription = d.GetComponent<Text>();
            if (f != null) itemInfoUI_itemFunctionality = f.GetComponent<Text>();
        }
    }

    public void EnsureDefaultMetadata()
    {
        string clean = gameObject.name.Replace("(Clone)", "").Trim();
        if (string.IsNullOrEmpty(thisName)) thisName = clean;

        if (clean.Contains("Stick"))
        {
            thisName = "Stick";
            if (string.IsNullOrEmpty(thisDescription))
                thisDescription = "A small dry stick. Probably fell from a tree.";
            if (string.IsNullOrEmpty(thisFunctionality))
                thisFunctionality = "Can be used to craft various items.";
            isConsumable = false;
            isEquippable = false;
            isTrashable = true;
        }
        else if (clean.Contains("Stone") || clean.Contains("Rock"))
        {
            thisName = "Stone";
            if (string.IsNullOrEmpty(thisDescription))
                thisDescription = "A hard piece of grey rock found on the ground.";
            if (string.IsNullOrEmpty(thisFunctionality))
                thisFunctionality = "Can be used to craft tools and campfires.";
            isConsumable = false;
            isEquippable = false;
            isTrashable = true;
        }
        else if (clean.Contains("Mushroom"))
        {
            thisName = "Mushroom";
            if (string.IsNullOrEmpty(thisDescription))
                thisDescription = "A wild forest mushroom with a red spotted cap.";
            if (string.IsNullOrEmpty(thisFunctionality))
                thisFunctionality = "Right-click to eat: +10 Health, +120 Calories, +5% Hydration.";
            isConsumable = true;
            if (healthEffect == 0 && caloriesEffect == 0)
            {
                healthEffect = 10f;
                caloriesEffect = 120f;
                hydrationEffect = 5f;
            }
            isEquippable = false;
            isTrashable = true;
        }
        else if (clean.Contains("Berries") || clean.Contains("Berry"))
        {
            thisName = "Berries";
            if (string.IsNullOrEmpty(thisDescription))
                thisDescription = "Sweet and juicy wild berries picked from forest bushes.";
            if (string.IsNullOrEmpty(thisFunctionality))
                thisFunctionality = "Right-click to eat: +5 Health, +80 Calories, +15% Hydration.";
            isConsumable = true;
            if (healthEffect == 0 && caloriesEffect == 0)
            {
                healthEffect = 5f;
                caloriesEffect = 80f;
                hydrationEffect = 15f;
            }
            isEquippable = false;
            isTrashable = true;
        }
        else if (clean.Contains("Axe"))
        {
            thisName = "Axe";
            if (string.IsNullOrEmpty(thisDescription))
                thisDescription = "A handcrafted Stone Axe bound to a sturdy wooden handle.";
            if (string.IsNullOrEmpty(thisFunctionality))
                thisFunctionality = "Right-click or drag to QuickSlot (1-7) to equip and chop trees.";
            isConsumable = false;
            isEquippable = true;
            isTrashable = true;
        }
    }

    // Khi đưa con trỏ chuột vào vùng vật phẩm
    public void OnPointerEnter(PointerEventData eventData)
    {
        EnsureDefaultMetadata();
        if (itemInfoUI == null) BindItemInfoUI();

        if (itemInfoUI != null)
        {
            itemInfoUI.SetActive(true);
            if (itemInfoUI_itemName != null) itemInfoUI_itemName.text = thisName;
            if (itemInfoUI_itemDescription != null) itemInfoUI_itemDescription.text = thisDescription;
            if (itemInfoUI_itemFunctionality != null) itemInfoUI_itemFunctionality.text = thisFunctionality;
        }
    }

    // Khi con trỏ chuột rời khỏi vùng vật phẩm
    public void OnPointerExit(PointerEventData eventData)
    {
        if (itemInfoUI != null)
        {
            itemInfoUI.SetActive(false);
        }
    }

    // Khi bấm chuột trên vật phẩm
    public void OnPointerDown(PointerEventData eventData)
    {
        // Chuột phải (Right Mouse Button)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (isConsumable)
            {
                itemPendingConsumption = gameObject;
                consumingFunction(healthEffect, caloriesEffect, hydrationEffect);
            }

            if (isEquippable && !isInsideQuickSlot && EquipSystem.Instance != null && !EquipSystem.Instance.CheckIfFull())
            {
                itemPendingEquipping = gameObject;
            }
        }
    }

    // Khi nhả nút chuột trên vật phẩm
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (isConsumable && itemPendingConsumption == gameObject)
            {
                DestroyImmediate(gameObject);
                if (InventorySystem.Instance != null) InventorySystem.Instance.ReCalculeList();
                if (CraftingSystem.Instance != null) CraftingSystem.Instance.RefreshNeededItems();
            }

            if (isEquippable && itemPendingEquipping == gameObject && EquipSystem.Instance != null)
            {
                if (itemInfoUI != null) itemInfoUI.SetActive(false);
                EquipSystem.Instance.AddToQuickSlots(gameObject);
                isInsideQuickSlot = true;
            }
        }
    }

    private void consumingFunction(float healthEffect, float caloriesEffect, float hydrationEffect)
    {
        if (itemInfoUI != null) itemInfoUI.SetActive(false);

        healthEffectCalculation(healthEffect);
        caloriesEffectCalculation(caloriesEffect);
        hydrationEffectCalculation(hydrationEffect);
    }

    private static void healthEffectCalculation(float healthEffect)
    {
        if (PlayerState.Instance == null) return;
        float healthBeforeConsumption = PlayerState.Instance.currentHealth;
        float maxHealth = PlayerState.Instance.maxHealth;

        if (healthEffect != 0)
        {
            if ((healthBeforeConsumption + healthEffect) > maxHealth)
            {
                PlayerState.Instance.setHealth(maxHealth);
            }
            else
            {
                PlayerState.Instance.setHealth(healthBeforeConsumption + healthEffect);
            }
        }
    }

    private static void caloriesEffectCalculation(float caloriesEffect)
    {
        if (PlayerState.Instance == null) return;
        float caloriesBeforeConsumption = PlayerState.Instance.currentCalories;
        float maxCalories = PlayerState.Instance.maxCalories;

        if (caloriesEffect != 0)
        {
            if ((caloriesBeforeConsumption + caloriesEffect) > maxCalories)
            {
                PlayerState.Instance.setCalories(maxCalories);
            }
            else
            {
                PlayerState.Instance.setCalories(caloriesBeforeConsumption + caloriesEffect);
            }
        }
    }

    private static void hydrationEffectCalculation(float hydrationEffect)
    {
        if (PlayerState.Instance == null) return;
        float hydrationBeforeConsumption = PlayerState.Instance.currentHydrationPercent;
        float maxHydration = PlayerState.Instance.maxHydrationPercent;

        if (hydrationEffect != 0)
        {
            if ((hydrationBeforeConsumption + hydrationEffect) > maxHydration)
            {
                PlayerState.Instance.setHydration(maxHydration);
            }
            else
            {
                PlayerState.Instance.setHydration(hydrationBeforeConsumption + hydrationEffect);
            }
        }
    }
}
