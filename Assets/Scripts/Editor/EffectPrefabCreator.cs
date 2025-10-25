using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Basit görsel efekt prefabları oluşturur
    /// </summary>
    public class EffectPrefabCreator
    {
        [MenuItem("Tools/Tactical Combat/Create Effect Prefabs")]
        public static void CreateEffectPrefabs()
        {
            Debug.Log("🎨 Creating effect prefabs...");
            
            // Muzzle Flash prefab
            CreateMuzzleFlashPrefab();
            
            // Hit Effect prefab
            CreateHitEffectPrefab();
            
            // Damage Number prefab
            CreateDamageNumberPrefab();
            
            Debug.Log("✅ All effect prefabs created!");
        }
        
        private static void CreateMuzzleFlashPrefab()
        {
            // Muzzle flash için basit bir GameObject
            GameObject muzzleFlash = new GameObject("MuzzleFlash");
            
            // Particle System ekle
            ParticleSystem particles = muzzleFlash.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.1f;
            main.startSpeed = 5f;
            main.startSize = 0.1f;
            main.startColor = Color.yellow;
            main.maxParticles = 20;
            
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0.0f, 20)
            });
            
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            
            // Prefab olarak kaydet
            string path = "Assets/Prefabs/Effects/MuzzleFlash.prefab";
            CreateDirectoryIfNotExists("Assets/Prefabs/Effects");
            PrefabUtility.SaveAsPrefabAsset(muzzleFlash, path);
            Object.DestroyImmediate(muzzleFlash);
            
            Debug.Log($"✅ Created: {path}");
        }
        
        private static void CreateHitEffectPrefab()
        {
            // Hit effect için basit bir GameObject
            GameObject hitEffect = new GameObject("HitEffect");
            
            // Particle System ekle
            ParticleSystem particles = hitEffect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 3f;
            main.startSize = 0.05f;
            main.startColor = Color.red;
            main.maxParticles = 10;
            
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0.0f, 10)
            });
            
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
            
            // Prefab olarak kaydet
            string path = "Assets/Prefabs/Effects/HitEffect.prefab";
            CreateDirectoryIfNotExists("Assets/Prefabs/Effects");
            PrefabUtility.SaveAsPrefabAsset(hitEffect, path);
            Object.DestroyImmediate(hitEffect);
            
            Debug.Log($"✅ Created: {path}");
        }
        
        private static void CreateDamageNumberPrefab()
        {
            // Damage number için Canvas ve Text
            GameObject canvas = new GameObject("DamageNumberCanvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvasComponent.worldCamera = Camera.main;
            
            GameObject textGO = new GameObject("DamageText");
            textGO.transform.SetParent(canvas.transform);
            
            UnityEngine.UI.Text text = textGO.AddComponent<UnityEngine.UI.Text>();
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "100";
            
            // Outline ekle
            UnityEngine.UI.Outline outline = textGO.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, 2);
            
            // Prefab olarak kaydet
            string path = "Assets/Prefabs/Effects/DamageNumber.prefab";
            CreateDirectoryIfNotExists("Assets/Prefabs/Effects");
            PrefabUtility.SaveAsPrefabAsset(canvas, path);
            Object.DestroyImmediate(canvas);
            
            Debug.Log($"✅ Created: {path}");
        }
        
        private static void CreateDirectoryIfNotExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentPath = System.IO.Path.GetDirectoryName(path);
                string folderName = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }
    }
}
