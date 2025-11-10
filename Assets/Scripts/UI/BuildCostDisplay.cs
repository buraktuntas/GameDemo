using UnityEngine;
using TMPro;

namespace TacticalCombat.UI
{
    /// <summary>
    /// Build ghost preview üzerinde cost gösterir
    /// Ghost'un üstünde "Wall - 50₺" gibi text
    /// </summary>
    public class BuildCostDisplay : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Canvas canvas;

        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2, 0);
        [SerializeField] private float fontSize = 18f; // ✅ Smaller font - less intrusive
        [SerializeField] private Color affordableColor = new Color(0.3f, 1f, 0.3f, 0.7f); // ✅ More transparent green
        [SerializeField] private Color unaffordableColor = new Color(1f, 0.3f, 0.3f, 0.7f); // ✅ More transparent red

        private Camera playerCamera;

        private void Start()
        {
            // Create canvas if not assigned
            if (canvas == null)
            {
                CreateCanvas();
            }

            // Create text if not assigned
            if (costText == null)
            {
                CreateCostText();
            }

            // Find player camera
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerCamera = player.GetComponentInChildren<Camera>();
                }
            }
        }

        private void LateUpdate()
        {
            // Billboard effect - always face camera
            if (playerCamera != null && canvas != null)
            {
                canvas.transform.LookAt(canvas.transform.position + playerCamera.transform.rotation * Vector3.forward,
                    playerCamera.transform.rotation * Vector3.up);
            }

            // Position above ghost
            if (canvas != null)
            {
                canvas.transform.position = transform.position + offset;
            }
        }

        /// <summary>
        /// Update cost display
        /// </summary>
        public void UpdateCost(string structureName, int cost, bool canAfford)
        {
            if (costText != null)
            {
                costText.text = $"{structureName}\n{cost} ₺";
                costText.color = canAfford ? affordableColor : unaffordableColor;
            }

            // Show canvas
            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Hide cost display
        /// </summary>
        public void Hide()
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        private void CreateCanvas()
        {
            GameObject canvasObj = new GameObject("CostCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = offset;

            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 100);
            rect.localScale = Vector3.one * 0.01f; // Scale down for world space

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        }

        private void CreateCostText()
        {
            if (canvas == null)
            {
                CreateCanvas();
            }

            GameObject textObj = new GameObject("CostText");
            textObj.transform.SetParent(canvas.transform, false);

            costText = textObj.AddComponent<TextMeshProUGUI>();
            costText.fontSize = fontSize;
            costText.color = affordableColor;
            costText.alignment = TextAlignmentOptions.Center;
            costText.text = "Wall - 50₺";

            RectTransform rect = costText.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Add outline for better visibility
            var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, 2);
        }

        private void OnDestroy()
        {
            if (canvas != null && canvas.gameObject != null)
            {
                Destroy(canvas.gameObject);
            }
        }
    }
}
