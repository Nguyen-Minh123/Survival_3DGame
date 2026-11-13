using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// InteractableObject - Gắn lên các đồ vật trong thế giới game mà người chơi có thể nhìn vào và nhặt được.
/// Theo đúng thiết kế từ Mike's Code 3D Survival Game Tutorial (Episode 3 & 4).
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("=== Cấu Hình Vật Phẩm ===")]
    [Tooltip("Tên vật phẩm hiển thị khi nhìn vào")]
    public string ItemName = "Default Item";

    [Tooltip("Khoảng cách tối đa người chơi có thể nhặt được (mét)")]
    public float interactionDistance = 3.5f;

    [Tooltip("Icon hiển thị của vật phẩm trong kho chứa UI")]
    public Sprite itemIcon;

    [Tooltip("Mô tả ngắn")]
    public string description = "";

    [HideInInspector]
    public bool playerInRange = false;

    public string GetItemName()
    {
        return ItemName;
    }

    /// <summary>
    /// Hàm xử lý khi người chơi bấm nút nhặt (phím E hoặc chuột)
    /// </summary>
    public virtual void PickUp()
    {
        Debug.Log(">>> [Survival Game] Đang nhặt vật phẩm: " + ItemName);

        // 1. Thêm vào kho chứa UI nâng cao (InventorySystem) nếu có
        if (InventorySystem.Instance != null)
        {
            bool added = InventorySystem.Instance.AddToInventory(ItemName, itemIcon);
            if (!added)
            {
                // Kho đồ đầy, không nhặt được
                return;
            }
        }

        // 2. Cộng vào bộ đếm SimpleInventory nếu có
        SimpleInventory inventory = Object.FindAnyObjectByType<SimpleInventory>();
        if (inventory != null)
        {
            inventory.AddItem(ItemName);
        }

        // 3. Xóa vật thể khỏi thế giới sau khi đã vào kho thành công
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
