using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CaloriesBar - Hiển thị thanh thức ăn / năng lượng (Calories) trên giao diện HUD (Lab 5 - Episode 10).
/// </summary>
public class CaloriesBar : MonoBehaviour
{
    private Slider slider;
    public Text caloriesCounter;
    public GameObject playerState;

    private float currentCalories, maxCalories;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (caloriesCounter == null)
        {
            caloriesCounter = GetComponentInChildren<Text>();
        }
    }

    private void Update()
    {
        PlayerState ps = (playerState != null) ? playerState.GetComponent<PlayerState>() : PlayerState.Instance;
        if (ps == null) return;

        currentCalories = ps.currentCalories;
        maxCalories = ps.maxCalories;

        float fillValue = (maxCalories > 0f) ? (currentCalories / maxCalories) : 0f;
        if (slider != null) slider.value = fillValue;

        if (caloriesCounter != null)
        {
            caloriesCounter.text = Mathf.RoundToInt(currentCalories) + "/" + Mathf.RoundToInt(maxCalories);
        }
    }
}
