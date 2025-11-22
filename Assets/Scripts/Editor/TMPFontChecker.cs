using UnityEngine;
using UnityEditor;
using TMPro;

namespace TacticalCombat.Editor
{
    public static class TMPFontChecker
    {
        [MenuItem("Tools/Tactical Combat/Check TMP Font Settings", false, 103)]
        public static void CheckTMPFontSettings()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔤 TEXTMESHPRO FONT CHECKER");
            Debug.Log("═══════════════════════════════════════");

            // Check TMP Settings
            if (TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogError("❌ TMP_Settings.defaultFontAsset is NULL!");
                Debug.LogError("This is why lobby player names are invisible!");

                // Try to find and assign a font
                TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                Debug.Log($"Found {fonts.Length} TMP Font Assets in project");

                if (fonts.Length > 0)
                {
                    TMP_FontAsset liberationSans = null;
                    foreach (var font in fonts)
                    {
                        Debug.Log($"  • {font.name} at {AssetDatabase.GetAssetPath(font)}");
                        if (font.name.Contains("LiberationSans"))
                        {
                            liberationSans = font;
                        }
                    }

                    bool autoFix = EditorUtility.DisplayDialog(
                        "TMP Font Missing",
                        $"❌ TextMeshPro default font is NULL!\n\n" +
                        $"This is why lobby player names are INVISIBLE.\n\n" +
                        $"Found {fonts.Length} TMP fonts in project.\n\n" +
                        $"Auto-assign {(liberationSans != null ? "LiberationSans SDF" : fonts[0].name)} as default font?",
                        "Yes, Fix It",
                        "No");

                    if (autoFix)
                    {
                        TMP_FontAsset fontToAssign = liberationSans != null ? liberationSans : fonts[0];
                        TMP_Settings.defaultFontAsset = fontToAssign;
                        EditorUtility.SetDirty(TMP_Settings.instance);
                        AssetDatabase.SaveAssets();

                        Debug.Log($"✅ Assigned {fontToAssign.name} as default TMP font!");
                        EditorUtility.DisplayDialog("Fixed",
                            $"✅ Default TMP font set to:\n{fontToAssign.name}\n\n" +
                            "Now run the game again and lobby should work!",
                            "OK");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "No TMP Fonts Found",
                        "❌ No TextMeshPro fonts found in project!\n\n" +
                        "Import TMP Essential Resources:\n" +
                        "Window → TextMeshPro → Import TMP Essential Resources",
                        "OK");
                }
            }
            else
            {
                Debug.Log($"✅ Default TMP Font: {TMP_Settings.defaultFontAsset.name}");
                Debug.Log($"   Path: {AssetDatabase.GetAssetPath(TMP_Settings.defaultFontAsset)}");

                EditorUtility.DisplayDialog(
                    "TMP Font OK",
                    $"✅ Default TMP font is set:\n\n{TMP_Settings.defaultFontAsset.name}\n\n" +
                    "Font settings are OK. The issue might be elsewhere.",
                    "OK");
            }

            Debug.Log("═══════════════════════════════════════");
        }

        [MenuItem("Tools/Tactical Combat/Import TMP Essential Resources", false, 104)]
        public static void ImportTMPResources()
        {
            EditorUtility.DisplayDialog(
                "Import TMP Resources",
                "To import TextMeshPro resources:\n\n" +
                "1. Window → TextMeshPro → Import TMP Essential Resources\n" +
                "2. Click 'Import' in the dialog\n" +
                "3. Then run 'Check TMP Font Settings' again",
                "OK");
        }
    }
}
