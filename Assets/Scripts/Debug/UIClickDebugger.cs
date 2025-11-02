using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticalCombat.Debugging
{
    /// <summary>
    /// UI Click Debugger - Shows why buttons might not be clickable
    /// Attach to any GameObject in scene
    /// </summary>
    public class UIClickDebugger : MonoBehaviour
    {
        [Header("Auto-run on Start")]
        [SerializeField] private bool runOnStart = true;

        private void Start()
        {
            if (runOnStart)
            {
                Invoke(nameof(RunDiagnostics), 0.5f); // Wait for scene to fully load
            }
        }

        private void Update()
        {
            // Press F1 to run diagnostics manually
            if (Input.GetKeyDown(KeyCode.F1))
            {
                RunDiagnostics();
            }

            // Show mouse position and raycast info
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"🖱️ MOUSE CLICK at position: {Input.mousePosition}");
                Debug.Log($"   Cursor state: {Cursor.lockState}, Visible: {Cursor.visible}");

                CheckRaycastAtMousePosition();
            }
        }

        [ContextMenu("Run UI Click Diagnostics")]
        public void RunDiagnostics()
        {
            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log("🔍 UI CLICK DIAGNOSTICS");
            Debug.Log("═══════════════════════════════════════════════════════");

            // 1. Check EventSystem
            CheckEventSystem();

            // 2. Check Canvas
            CheckCanvas();

            // 3. Check Cursor State
            CheckCursorState();

            // 4. Check UI Buttons
            CheckButtons();

            // 5. Check FPS Controller
            CheckFPSController();

            // 6. Check Time Scale
            CheckTimeScale();

            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log("✅ DIAGNOSTICS COMPLETE - Check logs above");
            Debug.Log("═══════════════════════════════════════════════════════");
        }

        private void CheckEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                Debug.LogError("❌ CRITICAL: EventSystem NOT FOUND in scene!");
                Debug.LogError("   Solution: Create EventSystem manually:");
                Debug.LogError("   GameObject → UI → Event System");
                return;
            }

            Debug.Log($"✅ EventSystem found: {eventSystem.name}");
            Debug.Log($"   Enabled: {eventSystem.enabled}");
            Debug.Log($"   Current selected: {eventSystem.currentSelectedGameObject}");

            StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule == null)
            {
                Debug.LogWarning("⚠️ StandaloneInputModule missing on EventSystem!");
            }
            else
            {
                Debug.Log($"   Input Module: {inputModule.GetType().Name} (enabled: {inputModule.enabled})");
            }
        }

        private void CheckCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

            if (canvases.Length == 0)
            {
                Debug.LogError("❌ CRITICAL: No Canvas found in scene!");
                return;
            }

            Debug.Log($"✅ Found {canvases.Length} Canvas(es)");

            foreach (Canvas canvas in canvases)
            {
                Debug.Log($"\n📐 Canvas: {canvas.name}");
                Debug.Log($"   Render Mode: {canvas.renderMode}");
                Debug.Log($"   Sorting Order: {canvas.sortingOrder}");
                Debug.Log($"   Enabled: {canvas.enabled}");

                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    Debug.LogError($"   ❌ CRITICAL: GraphicRaycaster MISSING on {canvas.name}!");
                    Debug.LogError($"      Solution: Add GraphicRaycaster component to Canvas");
                }
                else
                {
                    Debug.Log($"   ✅ GraphicRaycaster: enabled={raycaster.enabled}");
                    Debug.Log($"      Ignore Reversed Graphics: {raycaster.ignoreReversedGraphics}");
                    Debug.Log($"      Blocking Objects: {raycaster.blockingObjects}");
                }

                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    Debug.Log($"   Canvas Scaler: {scaler.uiScaleMode}");
                }
            }
        }

        private void CheckCursorState()
        {
            Debug.Log($"\n🖱️ Cursor State:");
            Debug.Log($"   Lock State: {Cursor.lockState}");
            Debug.Log($"   Visible: {Cursor.visible}");
            Debug.Log($"   Position: {Input.mousePosition}");

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Debug.LogWarning("⚠️ Cursor is LOCKED - UI clicks won't work!");
                Debug.LogWarning("   FPSController might be locking cursor");
            }

            if (!Cursor.visible)
            {
                Debug.LogWarning("⚠️ Cursor is INVISIBLE - might be hard to click!");
            }
        }

        private void CheckButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);

            Debug.Log($"\n🔘 Found {buttons.Length} Button(s) in scene");

            foreach (Button button in buttons)
            {
                if (!button.gameObject.activeInHierarchy)
                {
                    Debug.Log($"   ⚪ {GetFullPath(button.gameObject)} - INACTIVE");
                    continue;
                }

                Debug.Log($"   🔵 {GetFullPath(button.gameObject)}");
                Debug.Log($"      Interactable: {button.interactable}");
                Debug.Log($"      Enabled: {button.enabled}");
                Debug.Log($"      Listeners: {button.onClick.GetPersistentEventCount()}");

                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    Debug.Log($"      Raycast Target: {image.raycastTarget}");
                    if (!image.raycastTarget)
                    {
                        Debug.LogWarning($"         ⚠️ Raycast Target is FALSE - button won't receive clicks!");
                    }
                }

                // Check if blocked by other UI
                Canvas parentCanvas = button.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    Debug.Log($"      Parent Canvas: {parentCanvas.name} (order: {parentCanvas.sortingOrder})");
                }
            }
        }

        private void CheckFPSController()
        {
            var fpsController = FindFirstObjectByType<TacticalCombat.Player.FPSController>();

            if (fpsController != null)
            {
                Debug.Log($"\n🎮 FPSController found: {fpsController.name}");
                Debug.Log($"   This might be interfering with UI clicks");
                Debug.Log($"   Check IsAnyUIOpen() method is working correctly");
            }
            else
            {
                Debug.Log($"\n✅ No FPSController found (UI should work)");
            }
        }

        private void CheckTimeScale()
        {
            Debug.Log($"\n⏱️ Time Scale: {Time.timeScale}");

            if (Time.timeScale == 0f)
            {
                Debug.LogError("❌ CRITICAL: Time.timeScale is 0!");
                Debug.LogError("   UI input won't work when time is paused!");
            }
        }

        private void CheckRaycastAtMousePosition()
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            Debug.Log($"   Raycast results: {results.Count} hits");

            for (int i = 0; i < results.Count && i < 5; i++)
            {
                Debug.Log($"      [{i}] {GetFullPath(results[i].gameObject)} (distance: {results[i].distance})");
            }

            if (results.Count == 0)
            {
                Debug.LogWarning("   ⚠️ No UI elements detected at mouse position!");
            }
        }

        private string GetFullPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private void OnGUI()
        {
            // Show instructions
            GUI.Label(new Rect(10, Screen.height - 30, 400, 30),
                "Press F1 to run UI diagnostics | Click anywhere to see raycast info");
        }
    }
}
