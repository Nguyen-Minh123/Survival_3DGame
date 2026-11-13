using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SelectionManager - Quản lý tia Raycast từ tâm màn hình tới các đồ vật tương tác (InteractableObject).
/// Hỗ trợ chuyển đổi giữa Tâm chấm tròn (centerDotImage) và Biểu tượng bàn tay (handIcon) trong Lab 5 (Episode 9).
/// </summary>
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; set; }

    public bool onTarget;
    public GameObject selectedObject;

    [Header("=== Giao Diện Tương Tác UI ===")]
    public GameObject interaction_Info_UI;
    private Text interaction_text;

    [Header("=== Custom Crosshair & Hand Icon (Lab 5) ===")]
    public Image centerDotImage;
    public Image handIcon;

    [Header("=== Cấu Hình Khoảng Cách & Phím ===")]
    public float maxDistance = 4.5f;
    public LayerMask interactableLayers = ~0;
    public KeyCode interactionKey = KeyCode.E;

    private Camera mainCam;
    private InteractableObject currentTarget = null;
    private GUIStyle promptStyle;
    private GUIStyle promptBoxStyle;

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
        onTarget = false;
        mainCam = Camera.main;
        if (mainCam == null) mainCam = GetComponent<Camera>();

        if (interaction_Info_UI != null)
        {
            interaction_text = interaction_Info_UI.GetComponentInChildren<Text>();
            interaction_Info_UI.SetActive(false);
        }

        if (centerDotImage != null) centerDotImage.gameObject.SetActive(true);
        if (handIcon != null) handIcon.gameObject.SetActive(false);
    }

    private void Update()
    {
        bool uiOpen = (InventorySystem.Instance != null && InventorySystem.Instance.isOpen) ||
                      (CraftingSystem.Instance != null && CraftingSystem.Instance.isOpen);

        if (uiOpen)
        {
            ClearTarget();
            if (centerDotImage != null) centerDotImage.gameObject.SetActive(false);
            if (handIcon != null) handIcon.gameObject.SetActive(false);
            return;
        }

        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, interactableLayers))
        {
            var selectionTransform = hit.transform;
            InteractableObject interactable = selectionTransform.GetComponent<InteractableObject>();
            if (interactable == null)
            {
                interactable = selectionTransform.GetComponentInParent<InteractableObject>();
            }

            if (interactable != null && hit.distance <= interactable.interactionDistance)
            {
                onTarget = true;
                selectedObject = interactable.gameObject;
                currentTarget = interactable;
                currentTarget.playerInRange = true;

                if (interaction_Info_UI != null && interaction_text != null)
                {
                    interaction_text.text = currentTarget.GetItemName();
                    interaction_Info_UI.SetActive(true);
                }

                // Chuyển tâm ngắm sang Hand Icon (Lab 5 - Episode 9)
                if (centerDotImage != null && handIcon != null)
                {
                    centerDotImage.gameObject.SetActive(false);
                    handIcon.gameObject.SetActive(true);
                }

                if (Input.GetKeyDown(interactionKey))
                {
                    currentTarget.PickUp();
                    ClearTarget();
                }
                return;
            }
        }

        ClearTarget();
    }

    private void ClearTarget()
    {
        onTarget = false;
        selectedObject = null;

        if (currentTarget != null)
        {
            currentTarget.playerInRange = false;
            currentTarget = null;
        }

        if (interaction_Info_UI != null)
        {
            interaction_Info_UI.SetActive(false);
        }

        if (centerDotImage != null) centerDotImage.gameObject.SetActive(true);
        if (handIcon != null) handIcon.gameObject.SetActive(false);
    }

    private void OnGUI()
    {
        bool uiOpen = (InventorySystem.Instance != null && InventorySystem.Instance.isOpen) ||
                      (CraftingSystem.Instance != null && CraftingSystem.Instance.isOpen);
        if (uiOpen) return;

        InitGUIStyles();

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        // Nếu chưa có Canvas centerDotImage thì vẽ tâm ngắm OnGUI dự phòng
        if (centerDotImage == null && handIcon == null)
        {
            Color prevColor = GUI.color;
            GUI.color = currentTarget != null ? new Color(0.2f, 1.0f, 0.3f, 0.95f) : new Color(1f, 1f, 1f, 0.85f);
            float dotSize = currentTarget != null ? 8f : 5f;
            GUI.Box(new Rect(cx - dotSize * 0.5f, cy - dotSize * 0.5f, dotSize, dotSize), GUIContent.none);
            GUI.color = prevColor;
        }

        if (currentTarget != null)
        {
            string prompt = "[E] Nhặt: " + currentTarget.GetItemName();
            float boxW = 240f;
            float boxH = 38f;
            Rect promptRect = new Rect(cx - boxW * 0.5f, cy + 22f, boxW, boxH);

            GUI.Box(promptRect, "", promptBoxStyle);
            GUI.Label(promptRect, prompt, promptStyle);
        }
    }

    private void InitGUIStyles()
    {
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.95f, 0.4f) }
            };

            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.05f, 0.78f));
            tex.Apply();

            promptBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = tex }
            };
        }
    }
}
