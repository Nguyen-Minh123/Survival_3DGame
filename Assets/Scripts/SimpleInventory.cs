using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SimpleInventory - Lưu trữ danh sách vật phẩm cơ bản cho Lab 2.
/// Khi đã nâng cấp lên InventorySystem (Lab 3 -> Lab 8), tự động ẩn bảng OnGUI cũ để nhường chỗ cho Status Bars & Inventory UI.
/// </summary>
public class SimpleInventory : MonoBehaviour
{
    public static SimpleInventory Instance { get; private set; }

    [Header("=== Danh sách đồ trong túi ===")]
    public Dictionary<string, int> items = new Dictionary<string, int>();

    [Header("=== Cấu hình hiển thị HUD ===")]
    public bool showInventoryHUD = true;

    private string lastPickupMessage = "";
    private float messageTimer = 0f;
    private const float MessageDuration = 2.5f;

    private GUIStyle headerStyle;
    private GUIStyle itemStyle;
    private GUIStyle popupStyle;
    private GUIStyle boxStyle;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddItem(string itemName, int count = 1)
    {
        if (items.ContainsKey(itemName))
        {
            items[itemName] += count;
        }
        else
        {
            items[itemName] = count;
        }

        lastPickupMessage = "+ Đã nhặt: " + itemName + " (x" + items[itemName] + ")";
        messageTimer = MessageDuration;
    }

    private void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }
    }

    private void OnGUI()
    {
        // Khi đã có InventorySystem nâng cao (Lab 3+), tắt HUD chữ cũ ở góc trái để giao diện sạch đẹp
        if (InventorySystem.Instance != null) return;

        InitStyles();

        if (messageTimer > 0f)
        {
            float alpha = Mathf.Clamp01(messageTimer / 0.5f);
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            float msgW = 320f;
            float msgH = 40f;
            Rect msgRect = new Rect((Screen.width - msgW) * 0.5f, Screen.height * 0.72f, msgW, msgH);
            GUI.Box(msgRect, "", boxStyle);
            GUI.Label(msgRect, lastPickupMessage, popupStyle);

            GUI.color = oldColor;
        }

        if (showInventoryHUD)
        {
            float panelW = 220f;
            float panelH = 45f + Mathf.Max(1, items.Count) * 26f;
            Rect panelRect = new Rect(20f, 20f, panelW, panelH);

            GUI.Box(panelRect, "", boxStyle);

            GUILayout.BeginArea(new Rect(28f, 26f, panelW - 16f, panelH - 12f));
            GUILayout.Label("🎒 TÚI ĐỒ (INVENTORY)", headerStyle);
            GUILayout.Space(4);

            if (items.Count == 0)
            {
                GUILayout.Label("(Túi đồ đang trống - hãy nhặt vật phẩm)", itemStyle);
            }
            else
            {
                foreach (var pair in items)
                {
                    GUILayout.Label("• " + pair.Key + ":  x" + pair.Value, itemStyle);
                }
            }
            GUILayout.EndArea();
        }
    }

    private void InitStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };

            itemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                normal = { textColor = Color.white }
            };

            popupStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.4f, 1.0f, 0.4f) }
            };

            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.08f, 0.75f));
            bgTex.Apply();

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = bgTex }
            };
        }
    }
}
