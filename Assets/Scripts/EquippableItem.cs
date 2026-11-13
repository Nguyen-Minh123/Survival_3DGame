using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EquippableItem - Điều khiển hoạt ảnh vung vũ khí/công cụ (Stone Axe) khi bấm Chuột Trái.
/// Theo đúng thiết kế từ Mike's Code 3D Survival Game Tutorial (Lab 8 - Episode 13).
/// </summary>
[RequireComponent(typeof(Animator))]
public class EquippableItem : MonoBehaviour
{
    public Animator animator;
    private bool isSwinging = false;
    private Quaternion baseRotation;
    private Vector3 basePosition;

    private void Start()
    {
        animator = GetComponent<Animator>();
        baseRotation = transform.localRotation;
        basePosition = transform.localPosition;
    }

    private void Update()
    {
        bool uiBusy = (InventorySystem.Instance != null && InventorySystem.Instance.isOpen) ||
                      (CraftingSystem.Instance != null && CraftingSystem.Instance.isOpen);

        if (Input.GetMouseButtonDown(0) && !uiBusy && !isSwinging)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("hit");
            }
            StartCoroutine(SwingToolAnimation());
        }
    }

    private IEnumerator SwingToolAnimation()
    {
        isSwinging = true;

        Quaternion downRot = baseRotation * Quaternion.Euler(58f, -22f, 15f);
        Vector3 downPos = basePosition + new Vector3(-0.08f, -0.06f, 0.14f);

        // Pha bổ xuống (nhanh, dứt khoát)
        float t = 0f;
        float downDuration = 0.13f;
        while (t < downDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / downDuration);
            transform.localRotation = Quaternion.Slerp(baseRotation, downRot, p);
            transform.localPosition = Vector3.Lerp(basePosition, downPos, p);
            yield return null;
        }

        // Pha thu rìu về tư thế chuẩn bị
        t = 0f;
        float upDuration = 0.22f;
        while (t < upDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / upDuration);
            transform.localRotation = Quaternion.Slerp(downRot, baseRotation, p);
            transform.localPosition = Vector3.Lerp(downPos, basePosition, p);
            yield return null;
        }

        transform.localRotation = baseRotation;
        transform.localPosition = basePosition;
        isSwinging = false;
    }
}
