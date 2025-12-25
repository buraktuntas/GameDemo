// [FORCE SYNC 1.1] - Restoration of WeaponSystem API
using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Effects;
using TacticalCombat.Player; 
using System.Collections;
using System.Collections.Generic; 
using static TacticalCombat.Core.GameLogger;

namespace TacticalCombat.Combat
{
    [RequireComponent(typeof(AudioSource))]
    public class WeaponSystem : NetworkBehaviour
    {
        private static readonly int FireHash = Animator.StringToHash("Fire");
        private static readonly int ReloadHash = Animator.StringToHash("Reload");
        
        [Header("📦 WEAPON CONFIG")]
        [SerializeField] private WeaponConfig currentWeapon;
        
        [Header("🎯 REFERENCES")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform weaponHolder;
        private WeaponAudioController audioController;
        private WeaponVFXController vfxController;
        
        [SyncVar(hook = nameof(OnAmmoChanged_Hook))] private int currentAmmo;
        [SyncVar(hook = nameof(OnReserveAmmoChanged_Hook))] private int reserveAmmo;
        private bool isReloading;
        private float nextFireTime;
        private WeaponRecoil recoilController;
        
        [SyncVar] private int spreadSeed = 0;
        private Vector3 originalWeaponPos;
        private Quaternion originalWeaponRot;
        private bool isAiming;
        private InputManager inputManager;
        private Animator weaponAnimator;
        private bool hasFireParam;
        private bool hasReloadParam;
        
        // Changed to method for compatibility
        public bool IsReloading() => isReloading;
        
        public event System.Action<int, int> OnAmmoChanged;
        public event System.Action OnReloadStarted;
        public event System.Action OnReloadComplete;
        public event System.Action OnWeaponFired;
        
        private void Awake()
        {
            audioController = GetComponent<WeaponAudioController>() ?? gameObject.AddComponent<WeaponAudioController>();
            vfxController = GetComponent<WeaponVFXController>() ?? gameObject.AddComponent<WeaponVFXController>();
            recoilController = GetComponent<WeaponRecoil>() ?? gameObject.AddComponent<WeaponRecoil>();
            
            if (weaponHolder == null) weaponHolder = transform.Find("WeaponHolder") ?? transform.Find("Camera/WeaponHolder") ?? transform;
            weaponAnimator = GetComponent<Animator>() ?? (weaponHolder != null ? weaponHolder.GetComponent<Animator>() : null);

            // ✅ FIX: Check if Animator actually has the parameters to prevent "Parameter X does not exist" warnings
            if (weaponAnimator != null)
            {
                foreach (var param in weaponAnimator.parameters)
                {
                    if (param.nameHash == FireHash) hasFireParam = true;
                    if (param.nameHash == ReloadHash) hasReloadParam = true;
                }
            }
        }

        private void Start()
        {
            inputManager = GetComponent<InputManager>();
            TryAssignCamera();

            if (isServer && currentWeapon != null)
            {
                currentAmmo = currentWeapon.magazineSize;
                reserveAmmo = currentWeapon.maxAmmo;
                OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            }
        }

        /// <summary>
        /// ✅ MEMORY LEAK FIX: Clear all event subscriptions on destroy
        /// Prevents memory leaks from orphaned event handlers
        /// </summary>
        private void OnDestroy()
        {
            // Clear all event subscriptions to prevent memory leaks
            if (OnAmmoChanged != null)
            {
                foreach (System.Delegate d in OnAmmoChanged.GetInvocationList())
                {
                    OnAmmoChanged -= (System.Action<int, int>)d;
                }
            }

            if (OnReloadStarted != null)
            {
                foreach (System.Delegate d in OnReloadStarted.GetInvocationList())
                {
                    OnReloadStarted -= (System.Action)d;
                }
            }

            if (OnReloadComplete != null)
            {
                foreach (System.Delegate d in OnReloadComplete.GetInvocationList())
                {
                    OnReloadComplete -= (System.Action)d;
                }
            }

            if (OnWeaponFired != null)
            {
                foreach (System.Delegate d in OnWeaponFired.GetInvocationList())
                {
                    OnWeaponFired -= (System.Action)d;
                }
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[WeaponSystem] OnDestroy - Event subscriptions cleared");
            #endif
        }

        private void Update()
        {
            if (!enabled || !isLocalPlayer) return;
            if (playerCamera == null || inputManager == null) return;
            if (inputManager.IsInBuildMode) return;
            
            HandleInput();
            if (recoilController != null) recoilController.UpdateRecoil(5f, 10f);
        }
        
        private void HandleInput()
        {
            if (inputManager == null || currentWeapon == null) return;
            if (inputManager.IsInBuildMode || inputManager.BlockShootInput) return;

            if (MatchManager.Instance != null)
            {
                Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                if (currentPhase == Phase.Build || currentPhase == Phase.Lobby) return;
            }

            if (currentWeapon.fireMode == FireMode.Auto ? inputManager.FireHeld : inputManager.FirePressed)
            {
                if (CanFire()) Fire();
            }

            if (inputManager.ReloadPressed && CanReload()) StartReload();
        }
        
        private bool CanFire() => !isReloading && currentAmmo > 0 && Time.time >= nextFireTime;
        private bool CanReload() => !isReloading && currentAmmo < currentWeapon.magazineSize && reserveAmmo > 0;
        
        public int GetCurrentAmmo() => currentAmmo;
        public int GetReserveAmmo() => reserveAmmo;

        public void DisableWeapon() { enabled = false; if (weaponHolder != null) weaponHolder.gameObject.SetActive(false); }
        public void EnableWeapon() { enabled = true; if (weaponHolder != null) weaponHolder.gameObject.SetActive(true); }

        private void Fire()
        {
            if (!isServer) { CmdFire((float)NetworkTime.time); PlayLocalFireEffects(); PerformRaycast(); return; }
            ProcessFireServer((float)NetworkTime.time);
        }
        
        [Server]
        private void ProcessFireServer(float clientTime)
        {
            if (Time.time < nextFireTime || currentAmmo <= 0 || isReloading) return;
            spreadSeed = Random.Range(0, int.MaxValue);
            nextFireTime = Time.time + (1f / currentWeapon.fireRate);
            currentAmmo--;
            PerformServerRaycast();
            Vector3 muzzlePos = weaponHolder != null ? weaponHolder.position + weaponHolder.forward * 0.5f : transform.position;
            RpcPlayFireEffects(muzzlePos, weaponHolder != null ? weaponHolder.forward : transform.forward);
            OnWeaponFired?.Invoke();
        }
        
        [Command] private void CmdFire(float time) => ProcessFireServer(time);

        [ClientRpc]
        private void RpcPlayFireEffects(Vector3 pos, Vector3 dir)
        {
            vfxController?.PlayMuzzleFlashAt(pos, dir);
            audioController?.PlayFireSoundAt(pos, !isLocalPlayer);
            if (weaponAnimator != null && hasFireParam) weaponAnimator.SetTrigger(FireHash);
        }

        private void PlayLocalFireEffects()
        {
            if (recoilController != null) recoilController.ApplyRecoil(currentWeapon.recoilAmount);
            audioController?.PlayFireSound(true);
        }

        private void PerformRaycast() { }
        [Server] private void PerformServerRaycast() { }

        private void StartReload() { if (isServer) StartReloadServer(); else CmdStartReload(); }
        [Command] private void CmdStartReload() => StartReloadServer();
        [Server] private void StartReloadServer() { if (isReloading) return; StartCoroutine(ReloadCoroutine()); RpcOnReloadStarted(); }
        [ClientRpc] private void RpcOnReloadStarted() { if (weaponAnimator != null && hasReloadParam) weaponAnimator.SetTrigger(ReloadHash); audioController?.PlayReloadSound(); OnReloadStarted?.Invoke(); }

        private IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            yield return new WaitForSeconds(currentWeapon.reloadTime);
            if (isServer)
            {
                int needed = currentWeapon.magazineSize - currentAmmo;
                int amount = Mathf.Min(needed, reserveAmmo);
                currentAmmo += amount; reserveAmmo -= amount;
            }
            isReloading = false;
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            OnReloadComplete?.Invoke();
        }

        private void OnAmmoChanged_Hook(int o, int n) => OnAmmoChanged?.Invoke(n, reserveAmmo);
        private void OnReserveAmmoChanged_Hook(int o, int n) => OnAmmoChanged?.Invoke(currentAmmo, n);
        private bool TryAssignCamera() { if (playerCamera != null) return true; var fps = GetComponentInParent<FPSController>(); if (fps != null) playerCamera = fps.GetCamera(); return playerCamera != null; }
    }
}
