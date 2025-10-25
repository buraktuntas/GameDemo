using UnityEngine;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Professional Input Manager - Production Ready
    /// Manages input states, cursor modes, pause menu, and events
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // Singleton
        private static InputManager _instance;
        public static InputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<InputManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[InputManager]");
                        _instance = go.AddComponent<InputManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        // Cursor modes
        public enum CursorMode
        {
            Locked,    // FPS gameplay - cursor locked, camera + movement enabled
            Unlocked,  // Cursor visible - camera blocked, movement enabled
            Confined,  // Cursor confined - camera + movement enabled
            Free,      // Cursor free - camera + movement blocked
            Menu       // Full menu - everything blocked
        }
        
        // Events
        public event System.Action OnCursorLocked;
        public event System.Action OnCursorUnlocked;
        public event System.Action OnBuildModeEnter;
        public event System.Action OnBuildModeExit;
        public event System.Action OnPause;
        public event System.Action OnResume;
        
        // State
        private CursorMode currentMode = CursorMode.Locked;
        
        // Public properties
        public bool IsInBuildMode { get; set; }
        public bool IsInMenu { get; private set; }
        public bool IsPaused { get; private set; }
        
        // Input blocking
        public bool BlockCameraInput { get; set; }
        public bool BlockMovementInput { get; set; }
        public bool BlockJumpInput { get; set; }
        public bool BlockSprintInput { get; set; }
        public bool BlockInteractInput { get; set; }
        public bool BlockShootInput { get; set; }
        
        [Header("Settings")]
        [SerializeField] private bool allowPauseInBuildMode = false;
        [SerializeField] private bool showDebugInfo = false;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("✅ InputManager initialized");
            }
            else if (_instance != this)
            {
                Debug.Log("⚠️ Duplicate InputManager destroyed");
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            SetCursorMode(CursorMode.Locked);
        }
        
        private void Update()
        {
            HandleEscapeKey();
            HandleDebugKeys();
        }
        
        private void HandleEscapeKey()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            
            // Priority 1: If paused, resume
            if (IsPaused)
            {
                Resume();
                return;
            }
            
            // Priority 2: If in build mode, exit build mode
            if (IsInBuildMode)
            {
                ExitBuildMode();
                return;
            }
            
            // Priority 3: If in menu, close menu
            if (IsInMenu)
            {
                ExitMenu();
                return;
            }
            
            // Priority 4: Open pause menu
            Pause();
        }
        
        private void HandleDebugKeys()
        {
            // F1 = Debug info
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ShowDebugInfo();
            }
            
            // F2 = Toggle debug display
            if (Input.GetKeyDown(KeyCode.F2))
            {
                showDebugInfo = !showDebugInfo;
                Debug.Log($"Debug info: {(showDebugInfo ? "ON" : "OFF")}");
            }
        }
        
        private void ShowDebugInfo()
        {
            Debug.Log($"═══════════════════════════════\n" +
                      $"INPUT MANAGER STATUS\n" +
                      $"═══════════════════════════════\n" +
                      $"Cursor Mode: {currentMode}\n" +
                      $"Paused: {IsPaused}\n" +
                      $"Build Mode: {IsInBuildMode}\n" +
                      $"Menu Mode: {IsInMenu}\n" +
                      $"─────────────────────────────\n" +
                      $"Camera Input: {(BlockCameraInput ? "BLOCKED ❌" : "ENABLED ✅")}\n" +
                      $"Movement Input: {(BlockMovementInput ? "BLOCKED ❌" : "ENABLED ✅")}\n" +
                      $"Jump Input: {(BlockJumpInput ? "BLOCKED ❌" : "ENABLED ✅")}\n" +
                      $"Sprint Input: {(BlockSprintInput ? "BLOCKED ❌" : "ENABLED ✅")}\n" +
                      $"─────────────────────────────\n" +
                      $"Cursor Lock: {Cursor.lockState}\n" +
                      $"Cursor Visible: {Cursor.visible}\n" +
                      $"Time Scale: {Time.timeScale}\n" +
                      $"═══════════════════════════════");
        }
        
        // ═══════════════════════════════════════════════════════════
        // CURSOR MODE CONTROL
        // ═══════════════════════════════════════════════════════════
        
        public void SetCursorMode(CursorMode mode)
        {
            if (currentMode == mode) return;
            
            currentMode = mode;
            
            switch (mode)
            {
                case CursorMode.Locked:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    BlockCameraInput = false;
                    BlockMovementInput = false;
                    UnblockGameplayInput();
                    Debug.Log("🔒 FPS Mode: Gameplay enabled");
                    OnCursorLocked?.Invoke();
                    break;
                    
                case CursorMode.Unlocked:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    BlockCameraInput = true;
                    BlockMovementInput = false;
                    Debug.Log("🔓 Cursor Mode: Camera blocked, Movement enabled");
                    OnCursorUnlocked?.Invoke();
                    break;
                    
                case CursorMode.Confined:
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                    BlockCameraInput = false;
                    BlockMovementInput = false;
                    Debug.Log("🔒 Confined Mode: Cursor confined, Camera + Movement enabled");
                    break;
                    
                case CursorMode.Free:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    BlockCameraInput = true;
                    BlockMovementInput = true;
                    Debug.Log("🔓 Free Mode: Cursor free, Camera + Movement blocked");
                    break;
                    
                case CursorMode.Menu:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    BlockCameraInput = true;
                    BlockMovementInput = true;
                    BlockAllGameplayInput();
                    Debug.Log("📋 Menu Mode: All gameplay input blocked");
                    break;
            }
        }
        
        public CursorMode GetCurrentMode() => currentMode;
        
        // ═══════════════════════════════════════════════════════════
        // BUILD MODE
        // ═══════════════════════════════════════════════════════════
        
        public void EnterBuildMode()
        {
            IsInBuildMode = true;
            
            // Fortnite tarzı: Build mode'da durarak yapı yerleştir
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            BlockCameraInput = true;   // Kamerayı durdur
            BlockMovementInput = true; // Hareketi durdur
            BlockJumpInput = true;
            BlockSprintInput = true;
            
            Debug.Log("🏗️ Build Mode: Fortnite tarzı - Hareket durduruldu, Cursor confined");
            OnBuildModeEnter?.Invoke();
        }
        
        public void ExitBuildMode()
        {
            IsInBuildMode = false;
            SetCursorMode(CursorMode.Locked);
            Debug.Log("🎮 Exit Build Mode: Back to FPS mode");
            OnBuildModeExit?.Invoke();
        }
        
        // ═══════════════════════════════════════════════════════════
        // MENU MODE
        // ═══════════════════════════════════════════════════════════
        
        public void EnterMenu()
        {
            IsInMenu = true;
            SetCursorMode(CursorMode.Menu);
        }
        
        public void ExitMenu()
        {
            IsInMenu = false;
            SetCursorMode(CursorMode.Locked);
        }
        
        // ═══════════════════════════════════════════════════════════
        // PAUSE MENU
        // ═══════════════════════════════════════════════════════════
        
        public void Pause()
        {
            // Don't pause in build mode unless allowed
            if (IsInBuildMode && !allowPauseInBuildMode)
            {
                Debug.Log("⚠️ Cannot pause in build mode");
                return;
            }
            
            IsPaused = true;
            Time.timeScale = 0f;
            SetCursorMode(CursorMode.Menu);
            
            Debug.Log("⏸️ Game Paused");
            OnPause?.Invoke();
        }
        
        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            
            // Return to appropriate mode
            if (IsInBuildMode)
            {
                EnterBuildMode();
            }
            else
            {
                SetCursorMode(CursorMode.Locked);
            }
            
            Debug.Log("▶️ Game Resumed");
            OnResume?.Invoke();
        }
        
        public void TogglePause()
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
        
        // ═══════════════════════════════════════════════════════════
        // INPUT BLOCKING CONTROL
        // ═══════════════════════════════════════════════════════════
        
        public void BlockAllGameplayInput()
        {
            BlockJumpInput = true;
            BlockSprintInput = true;
            BlockInteractInput = true;
            BlockShootInput = true;
        }
        
        public void UnblockGameplayInput()
        {
            BlockJumpInput = false;
            BlockSprintInput = false;
            BlockInteractInput = false;
            BlockShootInput = false;
        }
        
        public void BlockAllInput()
        {
            BlockCameraInput = true;
            BlockMovementInput = true;
            BlockAllGameplayInput();
        }
        
        public void UnblockAllInput()
        {
            BlockCameraInput = false;
            BlockMovementInput = false;
            UnblockGameplayInput();
        }
        
        // ═══════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════
        
        public bool IsGameplayInputAllowed()
        {
            return !BlockMovementInput && !IsPaused && !IsInMenu;
        }
        
        public bool IsCameraInputAllowed()
        {
            return !BlockCameraInput && !IsPaused;
        }
        
        // ═══════════════════════════════════════════════════════════
        // DEBUG VISUALIZATION
        // ═══════════════════════════════════════════════════════════
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 300));
            
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Input Manager Debug</b>");
            GUILayout.Space(5);
            
            GUILayout.Label($"Mode: <color={(currentMode == CursorMode.Locked ? "green" : "yellow")}>{currentMode}</color>");
            GUILayout.Label($"Paused: <color={(IsPaused ? "red" : "green")}>{IsPaused}</color>");
            GUILayout.Label($"Build Mode: {IsInBuildMode}");
            
            GUILayout.Space(5);
            GUILayout.Label("<b>Input States:</b>");
            GUILayout.Label($"Camera: {StatusIcon(!BlockCameraInput)}");
            GUILayout.Label($"Movement: {StatusIcon(!BlockMovementInput)}");
            GUILayout.Label($"Jump: {StatusIcon(!BlockJumpInput)}");
            GUILayout.Label($"Sprint: {StatusIcon(!BlockSprintInput)}");
            
            GUILayout.Space(5);
            GUILayout.Label("<b>Cursor:</b>");
            GUILayout.Label($"Lock: {Cursor.lockState}");
            GUILayout.Label($"Visible: {Cursor.visible}");
            
            GUILayout.Space(5);
            GUILayout.Label("<size=10>F1=Full Info | F2=Toggle</size>");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private string StatusIcon(bool enabled)
        {
            return enabled ? "<color=green>✓</color>" : "<color=red>✗</color>";
        }
    }
}