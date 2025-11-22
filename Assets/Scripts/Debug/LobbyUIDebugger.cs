using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using TacticalCombat.UI;

namespace TacticalCombat.Debugging
{
    /// <summary>
    /// Debug tool to diagnose lobby UI visibility issues
    /// Attach this to any GameObject in the scene and press F9 to run diagnostics
    /// </summary>
    public class LobbyUIDebugger : MonoBehaviour
    {
        [Header("Hotkey")]
        [SerializeField] private KeyCode debugKey = KeyCode.F9;

        [Header("Auto-Run")]
        [SerializeField] private bool autoRunOnStart = false;
        [SerializeField] private float autoRunDelay = 2f;

        private void Start()
        {
            if (autoRunOnStart)
            {
                Invoke(nameof(RunDiagnostics), autoRunDelay);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(debugKey))
            {
                RunDiagnostics();
            }
        }

        [ContextMenu("Run Lobby UI Diagnostics")]
        public void RunDiagnostics()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine("🔍 LOBBY UI DIAGNOSTIC REPORT");
            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine();

            // Find Canvas
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            report.AppendLine($"📊 Found {canvases.Length} Canvas(es) in scene");

            Canvas mainCanvas = null;
            foreach (var canvas in canvases)
            {
                report.AppendLine($"  - {canvas.name}: RenderMode={canvas.renderMode}, SortOrder={canvas.sortingOrder}, Enabled={canvas.enabled}");
                if (canvas.name == "Canvas" || canvas.name == "LobbyCanvas")
                {
                    mainCanvas = canvas;
                }
            }
            report.AppendLine();

            if (mainCanvas == null)
            {
                report.AppendLine("❌ Main Canvas not found!");
                Debug.LogError(report.ToString());
                return;
            }

            // Find LobbyPanel
            Transform lobbyPanel = mainCanvas.transform.Find("LobbyPanel");
            if (lobbyPanel == null)
            {
                report.AppendLine("❌ LobbyPanel not found under Canvas!");
                Debug.LogError(report.ToString());
                return;
            }

            report.AppendLine($"✅ LobbyPanel found: Active={lobbyPanel.gameObject.activeSelf}, ActiveInHierarchy={lobbyPanel.gameObject.activeInHierarchy}");
            RectTransform lobbyRect = lobbyPanel.GetComponent<RectTransform>();
            if (lobbyRect != null)
            {
                report.AppendLine($"   Position: {lobbyRect.anchoredPosition}, Size: {lobbyRect.sizeDelta}");
            }
            report.AppendLine();

            // Find PlayerListContainer
            Transform container = lobbyPanel.Find("PlayerListContainer/ScrollView/Viewport/Content");
            if (container == null)
            {
                report.AppendLine("❌ PlayerListContainer/ScrollView/Viewport/Content not found!");

                // Try to find partial path
                Transform playerListContainer = lobbyPanel.Find("PlayerListContainer");
                if (playerListContainer != null)
                {
                    report.AppendLine($"   Found PlayerListContainer: Active={playerListContainer.gameObject.activeSelf}");
                    Transform scrollView = playerListContainer.Find("ScrollView");
                    if (scrollView != null)
                    {
                        report.AppendLine($"   Found ScrollView: Active={scrollView.gameObject.activeSelf}");
                        Transform viewport = scrollView.Find("Viewport");
                        if (viewport != null)
                        {
                            report.AppendLine($"   Found Viewport: Active={viewport.gameObject.activeSelf}");
                            Transform content = viewport.Find("Content");
                            if (content != null)
                            {
                                report.AppendLine($"   Found Content: Active={content.gameObject.activeSelf}");
                                container = content;
                            }
                            else
                            {
                                report.AppendLine("   ❌ Content not found under Viewport!");
                            }
                        }
                        else
                        {
                            report.AppendLine("   ❌ Viewport not found under ScrollView!");
                        }
                    }
                    else
                    {
                        report.AppendLine("   ❌ ScrollView not found under PlayerListContainer!");
                    }
                }
                else
                {
                    report.AppendLine("   ❌ PlayerListContainer not found under LobbyPanel!");
                }
            }
            else
            {
                report.AppendLine($"✅ Content container found: Active={container.gameObject.activeSelf}");
            }

            if (container == null)
            {
                Debug.LogError(report.ToString());
                return;
            }

            report.AppendLine();

            // Check container
            RectTransform containerRect = container.GetComponent<RectTransform>();
            report.AppendLine($"📦 Container Info:");
            report.AppendLine($"   Position: {containerRect.anchoredPosition}");
            report.AppendLine($"   Size: {containerRect.sizeDelta}");
            report.AppendLine($"   Child Count: {container.childCount}");
            report.AppendLine();

            // Check each player item
            if (container.childCount == 0)
            {
                report.AppendLine("⚠️ NO PLAYER ITEMS FOUND IN CONTAINER!");
            }
            else
            {
                report.AppendLine($"👥 Found {container.childCount} player item(s):");
                report.AppendLine();

                for (int i = 0; i < container.childCount; i++)
                {
                    Transform item = container.GetChild(i);
                    report.AppendLine($"  [{i}] {item.name}");
                    report.AppendLine($"      Active: {item.gameObject.activeSelf}");
                    report.AppendLine($"      ActiveInHierarchy: {item.gameObject.activeInHierarchy}");

                    RectTransform itemRect = item.GetComponent<RectTransform>();
                    if (itemRect != null)
                    {
                        report.AppendLine($"      Position: {itemRect.anchoredPosition}");
                        report.AppendLine($"      Size: {itemRect.sizeDelta}");
                        report.AppendLine($"      Scale: {itemRect.localScale}");
                    }

                    // Check Image component
                    Image itemImage = item.GetComponent<Image>();
                    if (itemImage != null)
                    {
                        Color c = itemImage.color;
                        report.AppendLine($"      Image Color: R={c.r:F2}, G={c.g:F2}, B={c.b:F2}, A={c.a:F2}");
                        report.AppendLine($"      Image Enabled: {itemImage.enabled}");
                        report.AppendLine($"      Raycast Target: {itemImage.raycastTarget}");

                        if (c.a < 0.01f)
                        {
                            report.AppendLine($"      ⚠️ WARNING: Image alpha is almost 0! Item is invisible!");
                        }
                    }
                    else
                    {
                        report.AppendLine($"      ⚠️ No Image component found!");
                    }

                    // Check NameText
                    Transform nameText = item.Find("NameText");
                    if (nameText != null)
                    {
                        report.AppendLine($"      ✅ NameText found: Active={nameText.gameObject.activeSelf}");

                        TextMeshProUGUI tmp = nameText.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            report.AppendLine($"         Text: \"{tmp.text}\"");
                            report.AppendLine($"         Font: {(tmp.font != null ? tmp.font.name : "NULL")}");
                            report.AppendLine($"         Color: R={tmp.color.r:F2}, G={tmp.color.g:F2}, B={tmp.color.b:F2}, A={tmp.color.a:F2}");
                            report.AppendLine($"         Enabled: {tmp.enabled}");
                            report.AppendLine($"         Font Size: {tmp.fontSize}");

                            if (tmp.font == null)
                            {
                                report.AppendLine($"         ❌ ERROR: Font is NULL! Text won't render!");
                            }
                            if (tmp.color.a < 0.01f)
                            {
                                report.AppendLine($"         ⚠️ WARNING: Text alpha is almost 0! Text is invisible!");
                            }
                        }
                        else
                        {
                            report.AppendLine($"         ⚠️ No TextMeshProUGUI component found!");
                        }
                    }
                    else
                    {
                        report.AppendLine($"      ❌ NameText child not found!");
                    }

                    report.AppendLine();
                }
            }

            // Check LobbyUIController
            report.AppendLine("🎮 LobbyUIController Status:");
            LobbyUIController lobbyController = FindFirstObjectByType<LobbyUIController>();
            if (lobbyController != null)
            {
                report.AppendLine($"   ✅ LobbyUIController found: {lobbyController.name}");
                report.AppendLine($"   Enabled: {lobbyController.enabled}");
            }
            else
            {
                report.AppendLine($"   ❌ LobbyUIController not found!");
            }

            report.AppendLine();
            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine("END OF DIAGNOSTIC REPORT");
            report.AppendLine("═══════════════════════════════════════");

            Debug.Log(report.ToString());
        }

        [ContextMenu("Force Show PlayerItem_0")]
        public void ForceShowPlayerItem()
        {
            Canvas mainCanvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            if (mainCanvas == null)
            {
                Debug.LogError("Canvas not found!");
                return;
            }

            Transform item = mainCanvas.transform.Find("LobbyPanel/PlayerListContainer/ScrollView/Viewport/Content/PlayerItem_0");
            if (item == null)
            {
                Debug.LogError("PlayerItem_0 not found!");
                return;
            }

            Debug.Log("🔧 Force enabling PlayerItem_0 and fixing visibility...");

            // Force active
            item.gameObject.SetActive(true);

            // Fix Image alpha
            Image img = item.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = 1f;
                img.color = c;
                img.enabled = true;
                Debug.Log("✅ Image alpha set to 1.0");
            }

            // Fix NameText
            Transform nameText = item.Find("NameText");
            if (nameText != null)
            {
                TextMeshProUGUI tmp = nameText.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    Color c = tmp.color;
                    c.a = 1f;
                    tmp.color = c;
                    tmp.enabled = true;

                    if (string.IsNullOrEmpty(tmp.text))
                    {
                        tmp.text = "Player0 (FIXED)";
                    }

                    Debug.Log($"✅ NameText fixed: \"{tmp.text}\"");
                }
            }

            Debug.Log("✅ PlayerItem_0 force show complete! Check Game view.");
        }
    }
}
