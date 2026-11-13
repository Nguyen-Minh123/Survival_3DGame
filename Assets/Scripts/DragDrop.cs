using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// DragDrop - Gắn lên mỗi vật phẩm trong ô Inventory / QuickSlot để hỗ trợ kéo và thả chuột.
/// Theo đúng thiết kế từ Mike's Code 3D Survival Game Tutorial (Episode 5, 11, 12).
/// </summary>
public class DragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("=== Thông tin ===")]
    public string itemName;
    [HideInInspector]
    public ItemSlot currentSlot;

    public static GameObject itemBeingDragged;
    private Vector3 startPosition;
    private Transform startParent;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rootCanvas = GetComponentInParent<Canvas>();
        if (transform.parent != null)
        {
            currentSlot = transform.parent.GetComponent<ItemSlot>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.6f;
        // Tắt chặn raycast để tia chuột xuyên qua vật thể, chạm vào ô ItemSlot hoặc TrashSlot phía dưới
        canvasGroup.blocksRaycasts = false;
        startPosition = transform.position;
        startParent = transform.parent;

        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform);
        }

        itemBeingDragged = gameObject;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
        }
        else
        {
            rectTransform.anchoredPosition += eventData.delta;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        itemBeingDragged = null;

        // Nếu không được thả vào một ItemSlot mới nào -> trả về ô ban đầu
        if (transform.parent == startParent || (rootCanvas != null && transform.parent == rootCanvas.transform))
        {
            transform.position = startPosition;
            transform.SetParent(startParent);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (InventorySystem.Instance != null) InventorySystem.Instance.ReCalculateList();
        if (CraftingSystem.Instance != null) CraftingSystem.Instance.RefreshNeededItems();
    }
}
