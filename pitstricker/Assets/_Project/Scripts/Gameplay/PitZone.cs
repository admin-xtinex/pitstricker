using System;
using UnityEngine;
using PitStriker.Physics;

namespace PitStriker.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class PitZone : MonoBehaviour
    {
        [Header("Pit Identity")]
        [SerializeField] private int _pitNumber = 1;

        [Header("Capture Thresholds")]
        [SerializeField] private float _maxCaptureSpeed = 0.90f;
        [SerializeField, Min(0.1f)] private float _sizeScale = 1f;
        public int PitNumber => _pitNumber;
        public bool IsSunk { get; private set; }

        private MarbleController _capturedMarble = null;
        public static event Action<PitZone, MarbleController> OnMarbleSunk;

        public void SetPitNumber(int number)
        {
            _pitNumber = number;
        }

        private void Awake()
        {
            if (gameObject.name.Contains("2") || gameObject.name.Contains("02"))
                _pitNumber = 2;
            else if (gameObject.name.Contains("3") || gameObject.name.Contains("03"))
                _pitNumber = 3;
            else if (gameObject.name.Contains("1") || gameObject.name.Contains("01"))
                _pitNumber = 1;

            SphereCollider sphereTrigger = GetComponent<SphereCollider>();
            if (sphereTrigger != null)
            {
                sphereTrigger.isTrigger = true;
                sphereTrigger.radius = 1.50f * _sizeScale;
                sphereTrigger.center = Vector3.zero;
            }
        }

        public bool IsMarbleInsidePit(MarbleController marble)
        {
            if (marble == null || marble.IsRetired) return false;
            Vector2 marbleXZ = new Vector2(marble.transform.position.x, marble.transform.position.z);
            Vector2 pitXZ = new Vector2(transform.position.x, transform.position.z);
            float horizontalDist = Vector2.Distance(marbleXZ, pitXZ);
            float relativeY = marble.transform.position.y - transform.position.y;
            float marbleRadius = marble.WorldRadius;
            return horizontalDist <= .52f * _sizeScale - marbleRadius * .25f
                && relativeY < marbleRadius * .85f;
        }

        public void MarkSunk(MarbleController marble, bool fireEvent = true)
        {
            if (marble == null || marble.IsRetired) return;
            _capturedMarble = marble;
            IsSunk = true;
            marble.Halt();
            if (PitStriker.Audio.AudioManager.Instance != null)
                PitStriker.Audio.AudioManager.Instance.PlayPitSink();
            if (PitStriker.VFX.VFXManager.Instance != null)
            {
                PitStriker.VFX.VFXManager.Instance.PlayPitCelebration(transform.position);
                PitStriker.VFX.VFXManager.Instance.PlayScoreBurst(transform.position);
            }
            if (PitStriker.CameraSystem.SmoothFollowCamera.Instance != null)
                PitStriker.CameraSystem.SmoothFollowCamera.Instance.TriggerScorePunch();
            Debug.Log($"<color=#00FFAA><b>[GOAL!]</b> {marble.name} settled and SUNK into Pit #{_pitNumber}!</color>");
            if (fireEvent)
                OnMarbleSunk?.Invoke(this, marble);
        }

        private void OnTriggerStay(Collider other)
        {
            MarbleController marble = other.GetComponent<MarbleController>();
            if (marble == null || marble.IsRetired) return;
            if (!IsMarbleInsidePit(marble)) return;
            if (marble.CurrentSpeed > _maxCaptureSpeed) return;
            if (marble == _capturedMarble)
            {
                marble.Halt();
                return;
            }
            MarkSunk(marble);
        }

        private void OnTriggerExit(Collider other)
        {
            MarbleController marble = other.GetComponent<MarbleController>();
            if (marble != null && marble == _capturedMarble)
            {
                IsSunk = false;
                _capturedMarble = null;
            }
        }

        public void ResetPit()
        {
            IsSunk = false;
            _capturedMarble = null;
        }
    }
}
