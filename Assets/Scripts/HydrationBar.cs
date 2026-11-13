using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HydrationBar - Hiển thị thanh nước uống (Hydration %) trên giao diện HUD (Lab 5 - Episode 10).
/// </summary>
public class HydrationBar : MonoBehaviour
{
    private Slider slider;
    public Text hydrationCounter;
    public GameObject playerState;

    private float currentHydration, maxHydration;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (hydrationCounter == null)
        {
            hydrationCounter = GetComponentInChildren<Text>();
        }
    }

    private void Update()
    {
        PlayerState ps = (playerState != null) ? playerState.GetComponent<PlayerState>() : PlayerState.Instance;
        if (ps == null) return;

        currentHydration = ps.currentHydrationPercent;
        maxHydration = ps.maxHydrationPercent;

        float fillValue = (maxHydration > 0f) ? (currentHydration / maxHydration) : 0f;
        if (slider != null) slider.value = fillValue;

        if (hydrationCounter != null)
        {
            hydrationCounter.text = Mathf.RoundToInt(currentHydration) + "%";
        }
    }
}
