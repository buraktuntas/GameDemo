using UnityEngine;
using System.Collections;
using Mirror;
using TacticalCombat.UI;
using TacticalCombat.Player;
using static TacticalCombat.Core.GameLogger;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Handles client-side visual updates for players, cameras, and HUD.
    /// Extracted from MatchManager to reduce its complexity.
    /// </summary>
    public class MatchPlayerVisualsController : MonoBehaviour
    {
        public void ShowAllPlayersLocal()
        {
            var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            LogInfo($"[VisualsController] Found {players.Length} PlayerController(s)");

            PlayerController localPlayerController = GetLocalPlayer(players);

            // 1. Activate all players and their visuals
            foreach (var pc in players)
            {
                ActivatePlayerVisuals(pc);
            }

            // 2. Handle local player camera
            if (localPlayerController != null)
            {
                SetupLocalPlayerCamera(localPlayerController);
            }
            else
            {
                LogError("[VisualsController] Local player NOT FOUND! Attempting last resort camera activation.");
                ActivateLastResortCamera();
            }

            // 3. UI and Bootstrap Environment
            DisableBootstrapCamera();
            EnableGameHUD();

            LogInfo("[VisualsController] All players and visuals initialized");
        }

        private PlayerController GetLocalPlayer(PlayerController[] players)
        {
            if (NetworkClient.localPlayer != null)
                return NetworkClient.localPlayer.GetComponent<PlayerController>();

            foreach (var p in players)
                if (p.isLocalPlayer) return p;

            if (NetworkClient.connection?.identity != null)
                return NetworkClient.connection.identity.GetComponent<PlayerController>();

            return null;
        }

        private void ActivatePlayerVisuals(PlayerController pc)
        {
            GameObject player = pc.gameObject;
            if (!player.activeSelf) player.SetActive(true);

            // Enable renderers and colliders
            foreach (var r in player.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            foreach (var c in player.GetComponentsInChildren<Collider>(true)) c.enabled = true;

            // Activate current weapon
            Transform weaponHolder = pc.transform.Find("WeaponHolder") ?? pc.transform.Find("PlayerVisual/WeaponHolder");
            if (weaponHolder != null)
            {
                Transform currentWeapon = weaponHolder.Find("CurrentWeapon");
                if (currentWeapon != null) currentWeapon.gameObject.SetActive(true);
            }
        }

        private void SetupLocalPlayerCamera(PlayerController pc)
        {
            var fps = pc.GetComponent<FPSController>();
            if (fps == null) return;

            // Ensure all parents are active
            Transform curr = pc.transform;
            while (curr.parent != null)
            {
                if (!curr.parent.gameObject.activeSelf) curr.parent.gameObject.SetActive(true);
                curr = curr.parent;
            }

            if (fps.playerCamera == null)
            {
                fps.SendMessage("SetupCamera", SendMessageOptions.DontRequireReceiver);
                StartCoroutine(SetupCameraAndActivateCoroutine(fps, pc.name));
            }
            else
            {
                ActivatePlayerCamera(fps, pc.name);
                StartCoroutine(EnsureCameraActiveDelayed(fps, pc.name));
            }

            fps.SendMessage("OnStartLocalPlayer", SendMessageOptions.DontRequireReceiver);
        }

        private IEnumerator SetupCameraAndActivateCoroutine(FPSController fps, string playerName)
        {
            yield return null; // Wait for SetupCamera
            ActivatePlayerCamera(fps, playerName);
        }

        private void ActivatePlayerCamera(FPSController fps, string playerName)
        {
            if (fps == null || fps.playerCamera == null) return;

            fps.playerCamera.gameObject.SetActive(true);
            fps.playerCamera.enabled = true;
            LogInfo($"[VisualsController] Camera activated for {playerName}");
        }

        private IEnumerator EnsureCameraActiveDelayed(FPSController fps, string playerName)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(0.5f);
                if (fps != null && fps.playerCamera != null && (!fps.playerCamera.gameObject.activeSelf || !fps.playerCamera.enabled))
                {
                    ActivatePlayerCamera(fps, playerName);
                    LogInfo($"[VisualsController] Retrying camera activation for {playerName} (Attempt {i+1})");
                }
            }
        }

        private void ActivateLastResortCamera()
        {
            var allFPS = FindObjectsByType<FPSController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fps in allFPS)
            {
                if (!fps.gameObject.activeSelf) fps.gameObject.SetActive(true);
                if (fps.playerCamera != null)
                {
                    ActivatePlayerCamera(fps, fps.name);
                    break;
                }
            }
        }

        private void DisableBootstrapCamera()
        {
            var obj = GameObject.Find("BootstrapCamera");
            if (obj != null && obj.TryGetComponent<Camera>(out var cam)) cam.enabled = false;
        }

        private void EnableGameHUD()
        {
            var hud = FindFirstObjectByType<GameHUD>(FindObjectsInactive.Include);
            if (hud != null) hud.gameObject.SetActive(true);
        }
    }
}
