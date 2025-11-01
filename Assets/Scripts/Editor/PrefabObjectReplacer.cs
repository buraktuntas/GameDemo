using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Prefab içindeki child objeleri değiştirmek için Editor tool
/// Kullanım: Tools -> Prefab Object Replacer
/// </summary>
public class PrefabObjectReplacer : EditorWindow
{
    private GameObject targetPrefab;
    private string childObjectPath = "";
    private GameObject replacementObject;

    private bool keepPosition = true;
    private bool keepRotation = true;
    private bool keepScale = true;
    private bool keepName = true;

    private Vector2 scrollPos;
    private string[] availableChildren = new string[0];

    [MenuItem("Tools/Prefab Object Replacer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabObjectReplacer>("Prefab Object Replacer");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.HelpBox(
            "Bu tool ile prefab içindeki herhangi bir child objeyi yeni bir obje ile değiştirebilirsin.\n\n" +
            "Örnek Kullanımlar:\n" +
            "• Player karakterini Mixamo karakteri ile değiştir\n" +
            "• Silah modelini yeni model ile değiştir\n" +
            "• Herhangi bir visual objeyi değiştir",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Target Prefab Selection
        EditorGUILayout.LabelField("1. PREFAB SEÇ", EditorStyles.boldLabel);
        GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Target Prefab",
            targetPrefab,
            typeof(GameObject),
            false
        );

        if (newPrefab != targetPrefab)
        {
            targetPrefab = newPrefab;
            UpdateAvailableChildren();
        }

        if (targetPrefab == null)
        {
            EditorGUILayout.HelpBox("Önce bir prefab seç!", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space(10);

        // Child Object Selection
        EditorGUILayout.LabelField("2. DEĞİŞTİRİLECEK OBJEYİ SEÇ", EditorStyles.boldLabel);

        if (availableChildren.Length > 0)
        {
            EditorGUILayout.LabelField("Mevcut Child Objeler:", EditorStyles.miniLabel);

            EditorGUI.indentLevel++;
            foreach (string child in availableChildren)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("• " + child, EditorStyles.miniLabel);
                if (GUILayout.Button("Seç", GUILayout.Width(50)))
                {
                    childObjectPath = child;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);
        }

        childObjectPath = EditorGUILayout.TextField("Child Object Path", childObjectPath);
        EditorGUILayout.HelpBox(
            "Path formatı: 'ChildName' veya 'Parent/Child/GrandChild'\n" +
            "Boş bırakırsan, prefab'ın kendisi değiştirilir.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Replacement Object
        EditorGUILayout.LabelField("3. YENİ OBJEYİ SEÇ", EditorStyles.boldLabel);
        replacementObject = (GameObject)EditorGUILayout.ObjectField(
            "Yeni Obje",
            replacementObject,
            typeof(GameObject),
            false
        );

        EditorGUILayout.HelpBox(
            "Bu obje eskisinin yerine koyulacak.\n" +
            "Scene'deki bir obje veya başka bir prefab olabilir.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Options
        EditorGUILayout.LabelField("4. AYARLAR", EditorStyles.boldLabel);
        keepPosition = EditorGUILayout.Toggle("Position'u Koru", keepPosition);
        keepRotation = EditorGUILayout.Toggle("Rotation'u Koru", keepRotation);
        keepScale = EditorGUILayout.Toggle("Scale'i Koru", keepScale);
        keepName = EditorGUILayout.Toggle("İsmi Koru", keepName);

        EditorGUILayout.Space(20);

        // Replace Button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("DEĞİŞTİR", GUILayout.Height(40)))
        {
            ReplaceObject();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // Quick Actions
        EditorGUILayout.LabelField("HIZLI AKSIYONLAR", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(1f, 0.8f, 0.4f);
        if (GUILayout.Button("🎮 Player Karakterini Değiştir (Mixamo)"))
        {
            SetupForPlayerCharacter();
        }

        if (GUILayout.Button("🔫 Silah Modelini Değiştir"))
        {
            SetupForWeaponModel();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    private void UpdateAvailableChildren()
    {
        if (targetPrefab == null)
        {
            availableChildren = new string[0];
            return;
        }

        Transform[] allChildren = targetPrefab.GetComponentsInChildren<Transform>(true);
        availableChildren = allChildren
            .Where(t => t != targetPrefab.transform)
            .Select(t => GetPathFromRoot(t, targetPrefab.transform))
            .ToArray();
    }

    private string GetPathFromRoot(Transform child, Transform root)
    {
        string path = child.name;
        Transform current = child.parent;

        while (current != null && current != root)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private void ReplaceObject()
    {
        if (targetPrefab == null)
        {
            EditorUtility.DisplayDialog("Hata", "Target prefab seçilmedi!", "Tamam");
            return;
        }

        if (replacementObject == null)
        {
            EditorUtility.DisplayDialog("Hata", "Yeni obje seçilmedi!", "Tamam");
            return;
        }

        // Open prefab for editing
        string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            Transform targetTransform;

            if (string.IsNullOrEmpty(childObjectPath))
            {
                // Replace entire prefab (just the visual children)
                targetTransform = prefabRoot.transform;
            }
            else
            {
                // Find child by path
                targetTransform = prefabRoot.transform.Find(childObjectPath);

                if (targetTransform == null)
                {
                    EditorUtility.DisplayDialog("Hata",
                        $"Child obje bulunamadı: {childObjectPath}\n\n" +
                        "Path'i kontrol et veya yukarıdaki listeden seç.",
                        "Tamam");
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                    return;
                }
            }

            // Store original properties
            Vector3 originalPosition = targetTransform.localPosition;
            Quaternion originalRotation = targetTransform.localRotation;
            Vector3 originalScale = targetTransform.localScale;
            string originalName = targetTransform.name;
            Transform originalParent = targetTransform.parent;
            int originalSiblingIndex = targetTransform.GetSiblingIndex();

            // Instantiate replacement
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(replacementObject, originalParent);

            if (newObject == null)
            {
                // If not a prefab, try instantiating normally
                newObject = Instantiate(replacementObject, originalParent);
            }

            // Apply settings
            if (keepPosition) newObject.transform.localPosition = originalPosition;
            if (keepRotation) newObject.transform.localRotation = originalRotation;
            if (keepScale) newObject.transform.localScale = originalScale;
            if (keepName) newObject.name = originalName;

            // Set sibling index
            newObject.transform.SetSiblingIndex(originalSiblingIndex);

            // Delete old object
            DestroyImmediate(targetTransform.gameObject);

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

            EditorUtility.DisplayDialog("Başarılı!",
                $"Obje başarıyla değiştirildi!\n\n" +
                $"Prefab: {targetPrefab.name}\n" +
                $"Path: {(string.IsNullOrEmpty(childObjectPath) ? "Root" : childObjectPath)}",
                "Tamam");

            Debug.Log($"✅ Obje değiştirildi: {prefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        // Refresh
        AssetDatabase.Refresh();
        UpdateAvailableChildren();
    }

    private void SetupForPlayerCharacter()
    {
        // Try to find Player prefab automatically
        string[] guids = AssetDatabase.FindAssets("t:Prefab Player");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            UpdateAvailableChildren();
        }

        childObjectPath = ""; // Usually visual model is a direct child
        keepPosition = true;
        keepRotation = true;
        keepScale = false; // Mixamo characters need scale adjustment
        keepName = false;

        EditorUtility.DisplayDialog("Player Karakter Değiştir",
            "1. Mixamo'dan karakterini indir (FBX for Unity)\n" +
            "2. Unity'e import et (Assets/Models/)\n" +
            "3. 'Yeni Obje' alanına sürükle\n" +
            "4. 'Child Object Path' alanına eski model'in path'ini yaz\n" +
            "   (örn: 'VisualModel' veya 'PlayerModel')\n" +
            "5. DEĞİŞTİR butonuna bas\n\n" +
            "Not: Mixamo karakterler genelde çok büyük gelir,\n" +
            "scale'i ~0.01 yapman gerekebilir.",
            "Anladım");
    }

    private void SetupForWeaponModel()
    {
        // Try to find weapon-related prefabs
        string[] guids = AssetDatabase.FindAssets("t:Prefab Weapon");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            UpdateAvailableChildren();
        }

        childObjectPath = "WeaponHolder/CurrentWeapon"; // Common path
        keepPosition = true;
        keepRotation = true;
        keepScale = true;
        keepName = false;

        EditorUtility.DisplayDialog("Silah Modeli Değiştir",
            "1. Yeni silah modelini import et\n" +
            "2. 'Target Prefab' olarak Player prefab'ını seç\n" +
            "3. 'Child Object Path' alanına silahın path'ini yaz\n" +
            "   (örn: 'WeaponHolder/AK47Model')\n" +
            "4. 'Yeni Obje' alanına yeni silah modelini sürükle\n" +
            "5. DEĞİŞTİR butonuna bas",
            "Anladım");
    }
}
