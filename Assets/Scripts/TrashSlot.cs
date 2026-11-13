using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// TrashSlot - Ô thùng rác trong Inventory cho phép kéo thả vật phẩm vào để xóa bỏ kèm hộp thoại xác nhận Yes/No.
/// Theo đúng thiết kế từ Mike's Code 3D Survival Game Tutorial (Lab 6 - Episode 11).
/// </summary>
public class TrashSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject trashAlertUI;
    private Text textToModify;

    public Sprite trash_closed;
    public Sprite trash_opened;

    private Image imageComponent;
    public Button YesBTN, NoBTN;

    private GameObject draggedItem
    {
        get { return DragDrop.itemBeingDragged; }
    }

    private GameObject itemToBeDeleted;

    public string itemName
    {
        get
        {
            if (itemToBeDeleted == null) return "";
            string name = itemToBeDeleted.name;
            string toRemove = "(Clone)";
            return name.Replace(toRemove, "").Trim();
        }
    }

    private void Start()
    {
        Transform bg = transform.Find("background");
        if (bg != null) imageComponent = bg.GetComponent<Image>();
        if (imageComponent == null) imageComponent = GetComponent<Image>();

        if (trashAlertUI != null)
        {
            Transform txt = trashAlertUI.transform.Find("Text");
            if (txt != null) textToModify = txt.GetComponent<Text>();

            Transform yesNode = trashAlertUI.transform.Find("yes");
            if (yesNode != null) YesBTN = yesNode.GetComponent<Button>();
            if (YesBTN != null)
            {
                YesBTN.onClick.RemoveAllListeners();
                YesBTN.onClick.AddListener(delegate { DeleteItem(); });
            }

            Transform noNode = trashAlertUI.transform.Find("no");
            if (noNode != null) NoBTN = noNode.GetComponent<Button>();
            if (NoBTN != null)
            {
                NoBTN.onClick.RemoveAllListeners();
                NoBTN.onClick.AddListener(delegate { CancelDeletion(); });
            }

            trashAlertUI.SetActive(false);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject item = draggedItem != null ? draggedItem : eventData.pointerDrag;
        if (item == null) return;

        InventoryItem invItem = item.GetComponent<InventoryItem>();
        if (invItem == null || invItem.isTrashable)
        {
            itemToBeDeleted = item;
            StartCoroutine(notifyBeforeDeletion());
        }
    }

    private IEnumerator notifyBeforeDeletion()
    {
        if (trashAlertUI != null)
        {
            trashAlertUI.SetActive(true);
            if (textToModify != null)
            {
                textToModify.text = "Throw away this " + itemName + "?";
            }
        }
        yield return new WaitForSeconds(1f);
    }

    private void CancelDeletion()
    {
        if (imageComponent != null && trash_closed != null)
        {
            imageComponent.sprite = trash_closed;
        }
        if (trashAlertUI != null)
        {
            trashAlertUI.SetActive(false);
        }
    }

    private void DeleteItem()
    {
        if (imageComponent != null && trash_closed != null)
        {
            imageComponent.sprite = trash_closed;
        }
        if (itemToBeDeleted != null)
        {
            DestroyImmediate(itemToBeDeleted);
        }
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ReCalculeList();
        }
        if (CraftingSystem.Instance != null)
        {
            CraftingSystem.Instance.RefreshNeededItems();
        }
        if (trashAlertUI != null)
        {
            trashAlertUI.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (draggedItem != null)
        {
            InventoryItem invItem = draggedItem.GetComponent<InventoryItem>();
            if (invItem == null || invItem.isTrashable)
            {
                if (imageComponent != null && trash_opened != null)
                {
                    imageComponent.sprite = trash_opened;
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (draggedItem != null)
        {
            InventoryItem invItem = draggedItem.GetComponent<InventoryItem>();
            if (invItem == null || invItem.isTrashable)
            {
                if (imageComponent != null && trash_closed != null)
                {
                    imageComponent.sprite = trash_closed;
                }
            }
        }
    }
}
