using UnityEngine;

namespace PitStriker.Core
{
    /// <summary>
    /// Phase 1 Educational Demo Script:
    /// Demonstrates the MonoBehaviour lifecycle methods and their execution order.
    /// Attach this component to any GameObject (like a Sphere) and press Play.
    /// </summary>
    public class LifecycleDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [Tooltip("Type a custom name to identify this object in the Console.")]
        [SerializeField] private string _objectLabel = "Test Sphere";

        private int _updateCount = 0;
        private int _fixedUpdateCount = 0;

        /// <summary>
        /// 1. Awake is called FIRST when the GameObject is initialized, before Start.
        /// Use Awake to initialize variables or cache component references (e.g. GetComponent).
        /// </summary>
        private void Awake()
        {
            Debug.Log($"[LIFECYCLE 1] Awake() called on '{_objectLabel}'. Memory initialized.");
        }

        /// <summary>
        /// 2. Start is called on the frame when a script is enabled, just before any Update methods.
        /// Use Start to set up initial game state or coordinate with other GameObjects.
        /// </summary>
        private void Start()
        {
            Debug.Log($"[LIFECYCLE 2] Start() called on '{_objectLabel}'. Ready for action!");
        }

        /// <summary>
        /// 3. FixedUpdate is called on a FIXED time interval (e.g. 50 or 60 times per second).
        /// CRITICAL RULE: All physics calculations and Rigidbody forces belong HERE.
        /// </summary>
        private void FixedUpdate()
        {
            _fixedUpdateCount++;
            
            // Only log the first few ticks to avoid flooding the console
            if (_fixedUpdateCount <= 3)
            {
                Debug.Log($"[LIFECYCLE 3] FixedUpdate() tick #{_fixedUpdateCount} - Physics simulation step.");
            }
        }

        /// <summary>
        /// 4. Update is called ONCE PER FRAME.
        /// Frame rate can vary (e.g. 60 FPS, 120 FPS).
        /// Use Update for user input (screen touches, keyboard) and visual movement.
        /// </summary>
        private void Update()
        {
            _updateCount++;

            // Only log the first few frames to avoid flooding the console
            if (_updateCount <= 3)
            {
                Debug.Log($"[LIFECYCLE 4] Update() frame #{_updateCount} - Rendering frame.");
            }
        }

        /// <summary>
        /// 5. OnDisable is called when the GameObject or component is deactivated.
        /// Always unsubscribe from C# events here to avoid memory leaks.
        /// </summary>
        private void OnDisable()
        {
            Debug.Log($"[LIFECYCLE 5] OnDisable() called on '{_objectLabel}'.");
        }

        /// <summary>
        /// 6. OnDestroy is called when the GameObject is removed from the scene.
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log($"[LIFECYCLE 6] OnDestroy() called on '{_objectLabel}'. Cleanup complete.");
        }
    }
}
