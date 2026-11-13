using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HealthBar - Hiển thị thanh máu (Health) của nhân vật trên giao diện HUD (Lab 5 - Episode 10).
/// </summary>
public class HealthBar : MonoBehaviour
{
    private Slider slider;
    public Text healthCounter;
    public GameObject playerState;

    private float currentHealth, maxHealth;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (healthCounter == null)
        {
            healthCounter = GetComponentInChildren<Text>();
        }
    }

    private void Update()
    {
        PlayerState ps = (playerState != null) ? playerState.GetComponent<PlayerState>() : PlayerState.Instance;
        if (ps == null) return;

        currentHealth = ps.currentHealth;
        maxHealth = ps.maxHealth;

        float fillValue = (maxHealth > 0f) ? (currentHealth / maxHealth) : 0f;
        if (slider != null) slider.value = fillValue;

        if (healthCounter != null)
        {
            healthCounter.text = Mathf.RoundToInt(currentHealth) + "/" + Mathf.RoundToInt(maxHealth);
        }
    }
}
