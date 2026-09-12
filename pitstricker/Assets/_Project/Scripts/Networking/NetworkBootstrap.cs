using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace PitStriker.Networking
{
    /// <summary>
    /// Phase 1: Core Networking Bootstrap
    /// Initializes Unity Gaming Services (UGS) and handles anonymous player authentication.
    /// Provides robust error handling for mobile network drops or offline operation.
    /// </summary>
    public static class NetworkBootstrap
    {
        public static bool IsInitialized => UnityServices.State == ServicesInitializationState.Initialized;
        public static bool IsSignedIn
        {
            get
            {
                try
                {
                    return IsInitialized && AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static string PlayerId
        {
            get
            {
                try
                {
                    return IsSignedIn ? AuthenticationService.Instance.PlayerId : string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public static event Action OnInitialized;
        public static event Action<string> OnSignedIn;
        public static event Action<string> OnInitializationFailed;

        private static bool _isInitializing = false;

        /// <summary>
        /// Initializes Unity Services and authenticates the player anonymously.
        /// Safe to call multiple times; returns immediately if already signed in.
        /// </summary>
        public static async Task<bool> InitializeAndSignInAsync()
        {
            if (IsSignedIn)
            {
                return true;
            }

            if (_isInitializing)
            {
                Debug.LogWarning("[NETWORK BOOTSTRAP] Initialization already in progress. Awaiting completion...");
                while (_isInitializing)
                {
                    await Task.Yield();
                }
                return IsSignedIn;
            }

            _isInitializing = true;

            try
            {
                // 1. Initialize Unity Services
                if (!IsInitialized)
                {
                    Debug.Log("<color=#00FFAA><b>[NETWORK BOOTSTRAP]</b> Initializing Unity Gaming Services...</color>");
                    var options = new InitializationOptions();
                    
                    // Profile isolation: In Unity Editor, use a unique profile if testing clone instances
#if UNITY_EDITOR
                    int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                    options.SetProfile($"Editor_{processId}");
#endif
                    await UnityServices.InitializeAsync(options);
                    OnInitialized?.Invoke();
                    Debug.Log("<color=#00FFAA><b>[NETWORK BOOTSTRAP]</b> Unity Services initialized successfully.</color>");
                }

                // 2. Sign In Anonymously
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("<color=#00FFAA><b>[NETWORK BOOTSTRAP]</b> Signing in anonymously...</color>");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    
                    string playerId = AuthenticationService.Instance.PlayerId;
                    Debug.Log($"<color=#00FF88><b>[NETWORK BOOTSTRAP]</b> Signed in successfully! Player ID: {playerId}</color>");
                    OnSignedIn?.Invoke(playerId);
                }

                _isInitializing = false;
                return true;
            }
            catch (AuthenticationException authEx)
            {
                _isInitializing = false;
                string error = $"Authentication error (Code {authEx.ErrorCode}): {authEx.Message}";
                Debug.LogError($"[NETWORK BOOTSTRAP] {error}");
                OnInitializationFailed?.Invoke(error);
                return false;
            }
            catch (RequestFailedException reqEx)
            {
                _isInitializing = false;
                string error = $"UGS network request failed: {reqEx.Message}";
                Debug.LogWarning($"[NETWORK BOOTSTRAP] {error}");
                OnInitializationFailed?.Invoke(error);
                return false;
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                string error = $"Unexpected bootstrap failure: {ex.Message}";
                Debug.LogError($"[NETWORK BOOTSTRAP] {error}");
                OnInitializationFailed?.Invoke(error);
                return false;
            }
        }

        /// <summary>
        /// Signs out the current player session if signed in.
        /// </summary>
        public static void SignOut()
        {
            if (IsSignedIn)
            {
                try
                {
                    AuthenticationService.Instance.SignOut();
                    Debug.Log("[NETWORK BOOTSTRAP] Signed out.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NETWORK BOOTSTRAP] Error during sign out: {ex.Message}");
                }
            }
        }
    }
}
