using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ItemSlot - Gắn lên mỗi ô chứa đồ trong giao diện Inventory UI và QuickSlots.
/// Triển khai IDropHandler để nhận các vật phẩm được kéo thả vào ô này.
/// Theo đúng thiết kế từ Mike's Code 3D Survival Game Tutorial (Episode 5, 6, 12).
/// </summary>
public class ItemSlot : MonoBehaviour, IDropHandler
{
    [Header("=== Trạng thái Ô Chứa ===")]
    public bool isOccupied = false;

    public GameObject Item
    {
        get
        {
            if (transform.childCount > 0)
            {
                return transform.GetChild(0).gameObject;
            }
            return null;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = DragDrop.itemBeingDragged != null ? DragDrop.itemBeingDragged : eventData.pointerDrag;
        if (draggedObject == null) return;

        DragDrop dragDrop = draggedObject.GetComponent<DragDrop>();
        if (dragDrop == null) return;

        bool isQuickSlot = gameObject.CompareTag("QuickSlot") || gameObject.name.StartsWith("QuickSlot");
        InventoryItem invItem = draggedObject.GetComponent<InventoryItem>();

        if (!Item)
        {
            draggedObject.transform.SetParent(transform);
            draggedObject.transform.localPosition = Vector2.zero;
            RectTransform rect = draggedObject.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = Vector2.zero;

            isOccupied = true;
            dragDrop.currentSlot = this;

            if (invItem != null)
            {
                invItem.isInsideQuickSlot = isQuickSlot;
            }

            if (InventorySystem.Instance != null) InventorySystem.Instance.ReCalculateList();
            if (CraftingSystem.Instance != null) CraftingSystem.Instance.RefreshNeededItems();
        }
        else
        {
            // Hoán đổi vị trí giữa 2 ô nếu cả 2 có vật phẩm
            Transform currentItem = transform.GetChild(0);
            ItemSlot sourceSlot = dragDrop.currentSlot;

            if (sourceSlot != null)
            {
                currentItem.SetParent(sourceSlot.transform);
                RectTransform curRect = currentItem.GetComponent<RectTransform>();
                if (curRect != null) curRect.anchoredPosition = Vector2.zero;

                DragDrop curDragDrop = currentItem.GetComponent<DragDrop>();
                if (curDragDrop != null) curDragDrop.currentSlot = sourceSlot;

                InventoryItem curInvItem = currentItem.GetComponent<InventoryItem>();
                if (curInvItem != null)
                {
                    curInvItem.isInsideQuickSlot = sourceSlot.gameObject.CompareTag("QuickSlot") || sourceSlot.gameObject.name.StartsWith("QuickSlot");
                }

                draggedObject.transform.SetParent(transform);
                RectTransform rect = draggedObject.GetComponent<RectTransform>();
                if (rect != null) rect.anchoredPosition = Vector2.zero;

                dragDrop.currentSlot = this;
                if (invItem != null) invItem.isInsideQuickSlot = isQuickSlot;

                if (InventorySystem.Instance != null) InventorySystem.Instance.ReCalculateList();
                if (CraftingSystem.Instance != null) CraftingSystem.Instance.RefreshNeededItems();
            }
        }
    }
}
