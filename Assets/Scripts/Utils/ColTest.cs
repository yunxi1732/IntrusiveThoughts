using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ColTest : MonoBehaviour
{
    public bool enableRaycast = true;

    void Update()
    {
        if (!enableRaycast) return;

        // 新 Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            Debug.Log($"射线检测到 {results.Count} 个物体：");
            foreach (var result in results)
            {
                Debug.Log($"  - {result.gameObject.name} (层级: {result.gameObject.layer})");
            }
        }
    }
}