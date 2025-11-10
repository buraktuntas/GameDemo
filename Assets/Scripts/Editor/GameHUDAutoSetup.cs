using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ KAPSAMLI: GameHUD'daki tüm boş referansları otomatik bulup assign eder
    /// </summary>
    public class GameHUDAutoSetup : EditorWindow
    {
        [MenuItem("TacticalCombat/Tools/Auto-Setup GameHUD (Complete)")]
        public static void ShowWindow()
        {
            GetWindow<GameHUDAutoSetup>("GameHUD Auto-Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("GameHUD Auto-Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu tool:\n" +
                "1. GameHUD'u scene'de bulur\n" +
                "2. Tüm boş referansları otomatik bulur ve assign eder\n" +
                "3. Eksik UI elementlerini oluşturur\n" +
                "4. Tüm referansları bağlar",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("🔧 Auto-Setup GameHUD (Complete)", GUILayout.Height(40)))
            {
                AutoSetupGameHUD();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🔍 Find Missing References", GUILayout.Height(30)))
            {
                FindMissingReferences();
            }
        }

        private void AutoSetupGameHUD()
        {
            // Find GameHUD in scene
            GameHUD gameHUD = FindFirstObjectByType<GameHUD>();

            if (gameHUD == null)
            {
                EditorUtility.DisplayDialog(
                    "Not Found",
                    "GameHUD bulunamadı!\n\n" +
                    "Önce GameHUD oluştur:\n" +
                    "Tools > Tactical Combat > Create GameHUD",
                    "OK"
                );
                return;
            }

            // Ensure Canvas exists first
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("✅ [AutoSetup] Created Canvas");
            }

            // Make sure GameHUD is child of Canvas
            if (gameHUD.transform.parent == null || gameHUD.transform.parent.GetComponent<Canvas>() == null)
            {
                gameHUD.transform.SetParent(canvas.transform, false);
                Debug.Log("✅ [AutoSetup] Moved GameHUD under Canvas");
            }

            int assignedCount = 0;
            int createdCount = 0;
            int skippedCount = 0;

            // Use reflection to get all SerializeField properties
            var type = typeof(GameHUD);
            var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            SerializedObject so = new SerializedObject(gameHUD);

            Debug.Log($"🔍 [AutoSetup] Scanning {fields.Length} fields in GameHUD...");

            foreach (var field in fields)
            {
                // Check if it's a SerializeField
                if (System.Attribute.IsDefined(field, typeof(SerializeField)) || field.IsPublic)
                {
                    SerializedProperty prop = so.FindProperty(field.Name);
                    if (prop != null)
                    {
                        // Check if it's null or empty
                        if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (prop.objectReferenceValue == null)
                            {
                                // Try to find in hierarchy (search in GameHUD and Canvas)
                                UnityEngine.Object found = FindInHierarchy(gameHUD.transform, field.Name, field.FieldType);
                                
                                if (found != null)
                                {
                                    prop.objectReferenceValue = found;
                                    assignedCount++;
                                    Debug.Log($"✅ [AutoSetup] Assigned {field.Name} → {found.name} ({field.FieldType.Name})");
                                }
                                else
                                {
                                    // Create if it's a common UI element
                                    UnityEngine.Object created = CreateMissingElement(gameHUD, field.Name, field.FieldType);
                                    if (created != null)
                                    {
                                        prop.objectReferenceValue = created;
                                        createdCount++;
                                        Debug.Log($"✅ [AutoSetup] Created and assigned {field.Name} ({field.FieldType.Name})");
                                    }
                                    else
                                    {
                                        skippedCount++;
                                        Debug.LogWarning($"⚠️ [AutoSetup] Could not find or create {field.Name} ({field.FieldType.Name})");
                                    }
                                }
                            }
                            else
                            {
                                // Already assigned
                                Debug.Log($"⏭️ [AutoSetup] {field.Name} already assigned: {prop.objectReferenceValue.name}");
                            }
                        }
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gameHUD);

            string resultMessage = $"✅ Assigned: {assignedCount} referans\n" +
                                  $"✅ Created: {createdCount} element\n" +
                                  $"⏭️ Skipped: {skippedCount} referans\n\n";
            
            if (assignedCount + createdCount > 0)
            {
                resultMessage += "GameHUD artık tamamen setup edildi!";
            }
            else if (skippedCount == 0)
            {
                resultMessage += "Tüm referanslar zaten dolu! ✅";
            }
            else
            {
                resultMessage += "Bazı referanslar bulunamadı. Console'u kontrol edin.";
            }

            EditorUtility.DisplayDialog("Setup Complete!", resultMessage, "Tamam");

            Debug.Log($"✅ [AutoSetup] GameHUD setup tamamlandı! ({assignedCount} assigned, {createdCount} created, {skippedCount} skipped)");
        }

        private UnityEngine.Object FindInHierarchy(Transform root, string fieldName, System.Type fieldType)
        {
            // Generate all possible name variations
            string[] nameVariations = GenerateNameVariations(fieldName);
            
            // Search in GameHUD's children first
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
            
            foreach (Transform child in allChildren)
            {
                foreach (string variation in nameVariations)
                {
                    if (child.name.Equals(variation, System.StringComparison.OrdinalIgnoreCase))
                    {
                        UnityEngine.Object found = TryGetComponent(child, fieldType);
                        if (found != null) return found;
                    }
                }
                
                // Also check if name contains the key part (e.g., "phase" in "phaseText")
                string keyPart = GetKeyPart(fieldName);
                if (!string.IsNullOrEmpty(keyPart) && child.name.IndexOf(keyPart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    UnityEngine.Object found = TryGetComponent(child, fieldType);
                    if (found != null) return found;
                }
            }
            
            // Also search in Canvas if GameHUD is under Canvas
            Canvas canvas = root.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.transform != root)
            {
                Transform[] canvasChildren = canvas.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in canvasChildren)
                {
                    foreach (string variation in nameVariations)
                    {
                        if (child.name.Equals(variation, System.StringComparison.OrdinalIgnoreCase))
                        {
                            UnityEngine.Object found = TryGetComponent(child, fieldType);
                            if (found != null) return found;
                        }
                    }
                }
            }

            return null;
        }
        
        private string[] GenerateNameVariations(string fieldName)
        {
            System.Collections.Generic.List<string> variations = new System.Collections.Generic.List<string>();
            
            // Original name
            variations.Add(fieldName);
            
            // Remove common suffixes and add variations
            string baseName = fieldName;
            if (fieldName.EndsWith("Text"))
            {
                baseName = fieldName.Substring(0, fieldName.Length - 4);
                variations.Add(baseName + " Text");
                variations.Add(baseName + "_Text");
                variations.Add(baseName + "Text");
                variations.Add(baseName);
            }
            else if (fieldName.EndsWith("Panel"))
            {
                baseName = fieldName.Substring(0, fieldName.Length - 5);
                variations.Add(baseName + " Panel");
                variations.Add(baseName + "_Panel");
                variations.Add(baseName + "Panel");
                variations.Add(baseName);
            }
            else if (fieldName.EndsWith("Slider"))
            {
                baseName = fieldName.Substring(0, fieldName.Length - 6);
                variations.Add(baseName + " Slider");
                variations.Add(baseName + "_Slider");
                variations.Add(baseName + "Slider");
                variations.Add(baseName);
            }
            else if (fieldName.EndsWith("Bar"))
            {
                baseName = fieldName.Substring(0, fieldName.Length - 3);
                variations.Add(baseName + " Bar");
                variations.Add(baseName + "_Bar");
                variations.Add(baseName + "Bar");
                variations.Add(baseName);
            }
            
            // Add camelCase variations
            if (fieldName.Length > 0)
            {
                string camelCase = char.ToLower(fieldName[0]) + fieldName.Substring(1);
                variations.Add(camelCase);
                
                string pascalCase = char.ToUpper(fieldName[0]) + fieldName.Substring(1);
                variations.Add(pascalCase);
            }
            
            return variations.ToArray();
        }
        
        private string GetKeyPart(string fieldName)
        {
            // Extract key part (remove common suffixes)
            string key = fieldName;
            if (key.EndsWith("Text")) key = key.Substring(0, key.Length - 4);
            else if (key.EndsWith("Panel")) key = key.Substring(0, key.Length - 5);
            else if (key.EndsWith("Slider")) key = key.Substring(0, key.Length - 6);
            else if (key.EndsWith("Bar")) key = key.Substring(0, key.Length - 3);
            else if (key.EndsWith("Icon")) key = key.Substring(0, key.Length - 4);
            else if (key.EndsWith("Overlay")) key = key.Substring(0, key.Length - 7);
            
            return key;
        }
        
        private UnityEngine.Object TryGetComponent(Transform child, System.Type fieldType)
        {
            if (fieldType == typeof(TextMeshProUGUI))
            {
                TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
                if (text != null) return text;
            }
            else if (fieldType == typeof(GameObject))
            {
                return child.gameObject;
            }
            else if (fieldType == typeof(Slider))
            {
                Slider slider = child.GetComponent<Slider>();
                if (slider != null) return slider;
            }
            else if (fieldType == typeof(Image))
            {
                Image image = child.GetComponent<Image>();
                if (image != null) return image;
            }
            else if (fieldType == typeof(Transform))
            {
                return child;
            }
            
            return null;
        }

        private UnityEngine.Object CreateMissingElement(GameHUD gameHUD, string fieldName, System.Type fieldType)
        {
            // Don't create prefabs (playerEntryPrefab, etc.)
            if (fieldName.Contains("Prefab") || fieldName.Contains("prefab"))
            {
                return null;
            }

            GameObject parent = gameHUD.gameObject;
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            if (parentRect == null)
            {
                parentRect = parent.AddComponent<RectTransform>();
                parentRect.anchorMin = Vector2.zero;
                parentRect.anchorMax = Vector2.one;
                parentRect.sizeDelta = Vector2.zero;
                parentRect.anchoredPosition = Vector2.zero;
            }

            // Get position based on field name (smart positioning)
            Vector2 position = GetSmartPosition(fieldName, fieldType);
            Vector2 size = GetSmartSize(fieldName, fieldType);

            // Create based on type
            if (fieldType == typeof(TextMeshProUGUI))
            {
                GameObject textObj = new GameObject(fieldName);
                textObj.transform.SetParent(parent.transform, false);
                
                RectTransform rect = textObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
                
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = GetDisplayText(fieldName);
                text.fontSize = GetFontSize(fieldName);
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                
                return text;
            }
            else if (fieldType == typeof(GameObject))
            {
                GameObject panel = new GameObject(fieldName);
                panel.transform.SetParent(parent.transform, false);
                
                RectTransform rect = panel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
                
                Image img = panel.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.5f);
                
                return panel;
            }
            else if (fieldType == typeof(Slider))
            {
                GameObject sliderObj = new GameObject(fieldName);
                sliderObj.transform.SetParent(parent.transform, false);
                
                RectTransform rect = sliderObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
                
                Slider slider = sliderObj.AddComponent<Slider>();
                slider.minValue = 0;
                slider.maxValue = 100;
                slider.value = 100;
                
                // Add background and fill area for slider
                GameObject bg = new GameObject("Background");
                bg.transform.SetParent(sliderObj.transform, false);
                Image bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                RectTransform bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                slider.targetGraphic = bgImg;
                
                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(sliderObj.transform, false);
                RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.sizeDelta = Vector2.zero;
                
                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                Image fillImg = fill.AddComponent<Image>();
                fillImg.color = Color.green;
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;
                
                slider.fillRect = fillRect;
                
                return slider;
            }
            else if (fieldType == typeof(Image))
            {
                GameObject imgObj = new GameObject(fieldName);
                imgObj.transform.SetParent(parent.transform, false);

                RectTransform rect = imgObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;

                Image img = imgObj.AddComponent<Image>();

                // ✅ HIT MARKER - Create cross shape
                if (fieldName.Contains("hitMarker") || fieldName.Contains("HitMarker"))
                {
                    img.enabled = false; // Parent image disabled, children are the cross
                    img.raycastTarget = false;

                    // Set color based on type
                    Color crossColor = Color.white;
                    if (fieldName.Contains("headshot"))
                        crossColor = Color.red;

                    // Create cross lines (horizontal + vertical)
                    CreateCrossLines(imgObj.transform, size.x, 2f, crossColor);

                    // Initially hidden
                    imgObj.SetActive(false);
                }
                else
                {
                    img.color = Color.white;
                }

                return img;
            }

            return null;
        }

        /// <summary>
        /// ✅ Create cross shape for hit marker (+ shape)
        /// </summary>
        private void CreateCrossLines(Transform parent, float size, float thickness, Color color)
        {
            // Horizontal line
            GameObject horizontal = new GameObject("CrossLine_Horizontal");
            horizontal.transform.SetParent(parent, false);
            Image hLine = horizontal.AddComponent<Image>();
            hLine.color = color;
            hLine.raycastTarget = false;

            RectTransform hRect = horizontal.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 0.5f);
            hRect.anchorMax = new Vector2(0.5f, 0.5f);
            hRect.pivot = new Vector2(0.5f, 0.5f);
            hRect.anchoredPosition = Vector2.zero;
            hRect.sizeDelta = new Vector2(size, thickness);

            // Vertical line
            GameObject vertical = new GameObject("CrossLine_Vertical");
            vertical.transform.SetParent(parent, false);
            Image vLine = vertical.AddComponent<Image>();
            vLine.color = color;
            vLine.raycastTarget = false;

            RectTransform vRect = vertical.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0.5f, 0.5f);
            vRect.anchorMax = new Vector2(0.5f, 0.5f);
            vRect.pivot = new Vector2(0.5f, 0.5f);
            vRect.anchoredPosition = Vector2.zero;
            vRect.sizeDelta = new Vector2(thickness, size);
        }

        private Vector2 GetSmartPosition(string fieldName, System.Type fieldType)
        {
            // Screen positions (assuming 1920x1080 base resolution)
            // Positions are relative to center (0,0)
            float screenWidth = 1920f;
            float screenHeight = 1080f;
            
            // Phase & Timer - Top Center
            if (fieldName.Contains("phase") || fieldName.Contains("Phase"))
                return new Vector2(0, screenHeight * 0.4f);
            if (fieldName.Contains("timer") || fieldName.Contains("Timer"))
                return new Vector2(0, screenHeight * 0.35f);
            if (fieldName.Contains("gameMode") || fieldName.Contains("GameMode"))
                return new Vector2(0, screenHeight * 0.3f);
            
            // Health - Top Right
            if (fieldName.Contains("health") || fieldName.Contains("Health"))
            {
                if (fieldType == typeof(Slider))
                    return new Vector2(screenWidth * 0.35f, screenHeight * 0.4f);
                return new Vector2(screenWidth * 0.4f, screenHeight * 0.4f);
            }
            
            // Ammo - Bottom Right
            if (fieldName.Contains("ammo") || fieldName.Contains("Ammo"))
                return new Vector2(screenWidth * 0.4f, -screenHeight * 0.4f);
            if (fieldName.Contains("reserveAmmo") || fieldName.Contains("ReserveAmmo"))
                return new Vector2(screenWidth * 0.4f, -screenHeight * 0.35f);
            
            // Resources - Top Left (Build Phase)
            if (fieldName.Contains("wallPoints") || fieldName.Contains("WallPoints"))
                return new Vector2(-screenWidth * 0.4f, screenHeight * 0.4f);
            if (fieldName.Contains("elevationPoints") || fieldName.Contains("ElevationPoints"))
                return new Vector2(-screenWidth * 0.4f, screenHeight * 0.35f);
            if (fieldName.Contains("trapPoints") || fieldName.Contains("TrapPoints"))
                return new Vector2(-screenWidth * 0.4f, screenHeight * 0.3f);
            if (fieldName.Contains("utilityPoints") || fieldName.Contains("UtilityPoints"))
                return new Vector2(-screenWidth * 0.4f, screenHeight * 0.25f);
            if (fieldName.Contains("resourcePanel") || fieldName.Contains("ResourcePanel"))
                return new Vector2(-screenWidth * 0.4f, screenHeight * 0.32f);
            
            // Ability - Bottom Left
            if (fieldName.Contains("ability") || fieldName.Contains("Ability"))
            {
                if (fieldType == typeof(Image))
                    return new Vector2(-screenWidth * 0.4f, -screenHeight * 0.4f);
                return new Vector2(-screenWidth * 0.4f, -screenHeight * 0.35f);
            }
            
            // Team Score - Top Left (Team Mode)
            if (fieldName.Contains("teamScore") || fieldName.Contains("TeamScore"))
                return new Vector2(-screenWidth * 0.4f, screenHeight * 0.45f);
            if (fieldName.Contains("teamStatus") || fieldName.Contains("TeamStatus"))
                return new Vector2(-screenWidth * 0.4f, screenHeight * 0.42f);
            
            // Core Carrying - Bottom Center (below crosshair, not blocking)
            if (fieldName.Contains("coreCarrying") || fieldName.Contains("CoreCarrying"))
                return new Vector2(0, -screenHeight * 0.25f); // ✅ Below crosshair
            if (fieldName.Contains("returnCore") || fieldName.Contains("ReturnCore"))
                return new Vector2(0, -screenHeight * 0.2f); // ✅ Below core carrying text
            
            // Sudden Death - Top Center (above crosshair, not blocking)
            if (fieldName.Contains("suddenDeath") || fieldName.Contains("SuddenDeath"))
                return new Vector2(0, screenHeight * 0.35f); // ✅ Above crosshair, near phase text
            
            // Kill Feed - Top Right (below health)
            if (fieldName.Contains("killFeed") || fieldName.Contains("KillFeed"))
                return new Vector2(screenWidth * 0.35f, screenHeight * 0.3f);
            
            // Headshot Indicator - Top Center (above crosshair, brief flash)
            if (fieldName.Contains("headshot") || fieldName.Contains("Headshot"))
                return new Vector2(0, screenHeight * 0.3f); // ✅ Above crosshair, brief indicator
            
            // Respawn - Bottom Center (below crosshair, not blocking)
            if (fieldName.Contains("respawn") || fieldName.Contains("Respawn"))
                return new Vector2(0, -screenHeight * 0.3f); // ✅ Below crosshair
            
            // Control Point - Top Center (above crosshair)
            if (fieldName.Contains("controlPoint") || fieldName.Contains("ControlPoint"))
                return new Vector2(0, screenHeight * 0.28f); // ✅ Above crosshair
            
            // Sabotage - Bottom Center (below crosshair)
            if (fieldName.Contains("sabotage") || fieldName.Contains("Sabotage"))
                return new Vector2(0, -screenHeight * 0.35f); // ✅ Below crosshair

            // Build Feedback - Bottom Left (less intrusive)
            if (fieldName.Contains("buildFeedback") || fieldName.Contains("BuildFeedback"))
                return new Vector2(-screenWidth * 0.4f, -screenHeight * 0.3f);

            // ✅ HIT MARKER - SCREEN CENTER (0, 0) - CRITICAL!
            if (fieldName.Contains("hitMarker") || fieldName.Contains("HitMarker"))
                return Vector2.zero; // ✅ EKRANIN TAM ORTASI - Crosshair ile aynı pozisyon!

            // Info Tower Hack - Bottom Center (below crosshair, not blocking)
            if (fieldName.Contains("infoTowerHack") || fieldName.Contains("InfoTowerHack"))
                return new Vector2(0, -screenHeight * 0.25f); // ✅ Below crosshair (was -0.15f, now -0.25f for more clearance)

            // Default: Top Right corner
            return new Vector2(screenWidth * 0.4f, screenHeight * 0.4f);
        }
        
        private Vector2 GetSmartSize(string fieldName, System.Type fieldType)
        {
            if (fieldType == typeof(TextMeshProUGUI))
            {
                // Phase/Timer texts - larger
                if (fieldName.Contains("phase") || fieldName.Contains("Phase") || 
                    fieldName.Contains("timer") || fieldName.Contains("Timer") ||
                    fieldName.Contains("suddenDeath") || fieldName.Contains("SuddenDeath"))
                    return new Vector2(400, 60);
                
                // Health/Ammo numbers - medium
                if (fieldName.Contains("health") || fieldName.Contains("Health") ||
                    fieldName.Contains("ammo") || fieldName.Contains("Ammo"))
                    return new Vector2(150, 50);
                
                // Resource points - smaller
                if (fieldName.Contains("Points") || fieldName.Contains("points"))
                    return new Vector2(200, 30);
                
                // Default text size
                return new Vector2(300, 50);
            }
            else if (fieldType == typeof(Slider))
            {
                // Health slider - wide
                if (fieldName.Contains("health") || fieldName.Contains("Health"))
                    return new Vector2(300, 20);
                
                // Other sliders - medium
                return new Vector2(200, 20);
            }
            else if (fieldType == typeof(GameObject))
            {
                // Panels - vary by type
                if (fieldName.Contains("resource") || fieldName.Contains("Resource"))
                    return new Vector2(250, 150);
                if (fieldName.Contains("ammo") || fieldName.Contains("Ammo"))
                    return new Vector2(200, 80);
                if (fieldName.Contains("ability") || fieldName.Contains("Ability"))
                    return new Vector2(100, 100);
                
                // Default panel size
                return new Vector2(300, 100);
            }
            else if (fieldType == typeof(Image))
            {
                // Ability icon - square
                if (fieldName.Contains("ability") || fieldName.Contains("Ability"))
                    return new Vector2(64, 64);

                // ✅ HIT MARKER - Small crosshair size (FPS standard)
                if (fieldName.Contains("hitMarker") || fieldName.Contains("HitMarker"))
                {
                    // Normal hit marker = 20x20
                    // Headshot marker = 30x30 (handled by CreateMissingElement)
                    if (fieldName.Contains("headshot"))
                        return new Vector2(30, 30);
                    return new Vector2(20, 20);
                }

                // Default image size
                return new Vector2(50, 50);
            }

            return new Vector2(200, 50);
        }
        
        private string GetDisplayText(string fieldName)
        {
            // Remove common suffixes
            string text = fieldName;
            if (text.EndsWith("Text")) text = text.Substring(0, text.Length - 4);
            else if (text.EndsWith("Panel")) text = text.Substring(0, text.Length - 5);
            else if (text.EndsWith("Slider")) text = text.Substring(0, text.Length - 6);
            else if (text.EndsWith("Bar")) text = text.Substring(0, text.Length - 3);
            
            // Convert camelCase to readable text
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && char.IsUpper(text[i]))
                    sb.Append(" ");
                sb.Append(text[i]);
            }
            
            return sb.ToString().Trim();
        }
        
        private int GetFontSize(string fieldName)
        {
            // Phase/Timer - larger
            if (fieldName.Contains("phase") || fieldName.Contains("Phase") || 
                fieldName.Contains("timer") || fieldName.Contains("Timer") ||
                fieldName.Contains("suddenDeath") || fieldName.Contains("SuddenDeath"))
                return 32;
            
            // Health/Ammo - medium-large
            if (fieldName.Contains("health") || fieldName.Contains("Health") ||
                fieldName.Contains("ammo") || fieldName.Contains("Ammo"))
                return 24;
            
            // Resource points - smaller
            if (fieldName.Contains("Points") || fieldName.Contains("points"))
                return 18;
            
            // Default
            return 20;
        }

        private void FindMissingReferences()
        {
            GameHUD gameHUD = FindFirstObjectByType<GameHUD>();

            if (gameHUD == null)
            {
                EditorUtility.DisplayDialog("Not Found", "GameHUD bulunamadı!", "OK");
                return;
            }

            System.Text.StringBuilder missing = new System.Text.StringBuilder();
            missing.AppendLine("Eksik Referanslar:\n");

            var type = typeof(GameHUD);
            var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            SerializedObject so = new SerializedObject(gameHUD);

            int missingCount = 0;

            foreach (var field in fields)
            {
                if (System.Attribute.IsDefined(field, typeof(SerializeField)) || field.IsPublic)
                {
                    SerializedProperty prop = so.FindProperty(field.Name);
                    if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (prop.objectReferenceValue == null)
                        {
                            missing.AppendLine($"  ❌ {field.Name} ({field.FieldType.Name})");
                            missingCount++;
                        }
                    }
                }
            }

            if (missingCount == 0)
            {
                EditorUtility.DisplayDialog("Perfect!", "Tüm referanslar dolu! ✅", "Harika!");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Missing References",
                    missing.ToString() + $"\nToplam: {missingCount} eksik referans",
                    "Tamam"
                );
            }
        }
    }
}

