using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerState - Quản lý các chỉ số sinh tồn của nhân vật: Máu (Health), Thức ăn/Năng lượng (Calories), Nước uống (Hydration).
/// Theo đúng thiết kế từ Mike's Code 3D Survival Game Tutorial (Lab 5 - Episode 10).
/// </summary>
public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; set; }

    [Header("=== Health (Máu) ===")]
    public float currentHealth;
    public float maxHealth = 100f;

    [Header("=== Calories (Thức ăn / Năng lượng) ===")]
    public float currentCalories;
    public float maxCalories = 2000f;
    public float distanceTravelled = 0f;
    private Vector3 lastPosition;
    public GameObject playerBody;

    [Header("=== Hydration (Nước uống) ===")]
    public float currentHydrationPercent;
    public float maxHydrationPercent = 100f;
    public bool isHydrationActive = true;

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
        currentHealth = maxHealth;
        currentCalories = 1800f; // Khởi tạo 1800/2000 để người chơi có thể ăn thử đồ ăn và thấy tăng ngay
        currentHydrationPercent = 98f; // Khởi tạo 98% giống trong hình minh họa Lab 5

        if (playerBody == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerBody = p;
            else playerBody = gameObject;
        }

        if (playerBody != null)
        {
            lastPosition = playerBody.transform.position;
        }

        StartCoroutine(decreaseHydration());
    }

    private IEnumerator decreaseHydration()
    {
        while (isHydrationActive)
        {
            yield return new WaitForSeconds(10f);
            if (currentHydrationPercent > 0f)
            {
                currentHydrationPercent = Mathf.Max(0f, currentHydrationPercent - 1f);
            }
        }
    }

    private void Update()
    {
        if (playerBody != null)
        {
            distanceTravelled += Vector3.Distance(playerBody.transform.position, lastPosition);
            lastPosition = playerBody.transform.position;

            // Cứ di chuyển 5 mét thì giảm 1 Calorie
            if (distanceTravelled >= 5f)
            {
                distanceTravelled = 0f;
                if (currentCalories > 0f)
                {
                    currentCalories = Mathf.Max(0f, currentCalories - 1f);
                }
            }
        }

        // Phím tắt test giảm máu [N] trong video 10 của Mike's Code
        if (Input.GetKeyDown(KeyCode.N))
        {
            currentHealth = Mathf.Max(0f, currentHealth - 10f);
        }
    }

    public void setHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
    }

    public void setCalories(float newCalories)
    {
        currentCalories = Mathf.Clamp(newCalories, 0f, maxCalories);
    }

    public void setHydration(float newHydration)
    {
        currentHydrationPercent = Mathf.Clamp(newHydration, 0f, maxHydrationPercent);
    }
}
