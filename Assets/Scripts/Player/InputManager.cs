using UnityEngine;
using UnityEngine.InputSystem;
using TacticalCombat.Core; // ✅ FIX: For MatchManager and Phase

namespace TacticalCombat.Player
{
    /// <summary>
    /// ✅ AAA QUALITY: Single Source of Truth for ALL Input
    /// Professional Input Manager - Production Ready
    /// Manages input states, cursor modes, pause menu, and events
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ✅ AAA: INPUT PROPERTIES - Single Source of Truth
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Movement input (WASD/Left Stick) - Normalized Vector2</summary>
        public Vector2 MoveInput { get; private set; }
        
        /// <summary>Look input (Mouse Delta/Right Stick) - Raw delta values</summary>
        public Vector2 LookInput { get; private set; }
        
        /// <summary>Jump pressed this frame</summary>
        public bool JumpPressed { get; private set; }
        
        /// <summary>Sprint held (Shift/Left Trigger)</summary>
        public bool SprintHeld { get; private set; }
        
        /// <summary>Fire pressed (Mouse Left/Right Trigger)</summary>
        public bool FirePressed { get; private set; }
        
        /// <summary>Fire held (for auto-fire weapons)</summary>
        public bool FireHeld { get; private set; }
        
        /// <summary>Reload pressed (R/X Button)</summary>
        public bool ReloadPressed { get; private set; }
        
        // ═══════════════════════════════════════════════════════════
        // CURSOR MODES
        // ═══════════════════════════════════════════════════════════
        
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
            Debug.Log("✅ InputManager initialized - Single Source of Truth");
        }
        
        private void Update()
        {
            // ✅ AAA: Read ALL input here - Single Source of Truth
            ReadAllInput();
        }

        private void OnEnable()
        {
            // ✅ CRITICAL FIX: Reset state when enabled (e.g. on respawn)
            IsInBuildMode = false;
            IsInMenu = false;
            IsPaused = false;
            
            BlockCameraInput = false;
            BlockMovementInput = false;
            UnblockGameplayInput();
            
            // ✅ CRITICAL FIX: Don't force lock cursor on enable - check UI state first
            // UI might be open (lobby, menu, etc.) - let UI systems manage cursor
            // Only lock if we're actually in gameplay (not in lobby/menu)
            if (MatchManager.Instance != null)
            {
                Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                if (currentPhase != Phase.Lobby && currentPhase != Phase.End)
                {
                    // In gameplay - lock cursor
                    currentMode = CursorMode.Locked;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    // In lobby/menu - unlock cursor
                    currentMode = CursorMode.Menu;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            else
            {
                // MatchManager not ready yet - default to Menu mode (safe)
                currentMode = CursorMode.Menu;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ✅ AAA: INPUT READING - Single Source of Truth
        // ═══════════════════════════════════════════════════════════
        
        private void ReadAllInput()
        {
            // Reset per-frame inputs
            JumpPressed = false;
            FirePressed = false;
            ReloadPressed = false;
            
            // ✅ Movement Input (WASD / Left Stick)
            if (!BlockMovementInput && Keyboard.current != null)
            {
                float h = 0, v = 0;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1;
                
                MoveInput = new Vector2(h, v);
                
                // Normalize to prevent diagonal speed boost
                if (MoveInput.magnitude > 1f)
                {
                    MoveInput = MoveInput.normalized;
                }
            }
            else
            {
                MoveInput = Vector2.zero;
            }
            
            // ✅ Look Input (Mouse Delta / Right Stick)
            if (!BlockCameraInput && Mouse.current != null)
            {
                LookInput = Mouse.current.delta.ReadValue();
            }
            else
            {
                LookInput = Vector2.zero;
            }
            
            // ✅ Jump Input (Space / A Button)
            if (!BlockJumpInput && Keyboard.current != null)
            {
                JumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            }
            
            // ✅ Sprint Input (Shift / Left Trigger)
            if (!BlockSprintInput && Keyboard.current != null)
            {
                SprintHeld = Keyboard.current.leftShiftKey.isPressed;
            }
            else
            {
                SprintHeld = false;
            }
            
            // ✅ Fire Input (Mouse Left / Right Trigger)
            if (!BlockShootInput && Mouse.current != null)
            {
                FirePressed = Mouse.current.leftButton.wasPressedThisFrame;
                FireHeld = Mouse.current.leftButton.isPressed;
            }
            else
            {
                FirePressed = false;
                FireHeld = false;
            }
            
            // ✅ Reload Input (R / X Button)
            if (!BlockShootInput && Keyboard.current != null)
            {
                ReloadPressed = Keyboard.current.rKey.wasPressedThisFrame;
            }
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
            
            // ✅ FORTNITE STYLE: Build while moving!
            // Players can walk and build, but can't sprint (balance)
            Cursor.lockState = CursorLockMode.Locked; // ✅ Keep cursor locked for camera control
            Cursor.visible = false; // ✅ Hide cursor (crosshair visible)
            BlockCameraInput = false;   // ✅ Allow camera movement
            BlockMovementInput = false; // ✅ Allow walking (Fortnite style!)
            BlockJumpInput = false;     // ✅ Allow jumping
            BlockSprintInput = true;    // ❌ Disable sprint (balance - can't run while building)
            
            Debug.Log("🏗️ Build Mode: Fortnite style - Walk + Build enabled, Sprint disabled");
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