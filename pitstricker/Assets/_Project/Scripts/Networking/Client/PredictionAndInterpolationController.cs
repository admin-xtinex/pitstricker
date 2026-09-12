using System;
using System.Collections.Generic;
using UnityEngine;
using PitStriker.Networking.Shared;
using PitStriker.Physics;

namespace PitStriker.Networking.Client
{
    /// <summary>
    /// Phase 5 Prediction, Interpolation, and Reconciliation Controller:
    /// - Local Player: Zero-latency client prediction + smooth server reconciliation error decay.
    /// - Remote Player: Snapshot buffering + smooth interpolation with jitter compensation.
    /// </summary>
    public class PredictionAndInterpolationController : MonoBehaviour
    {
        public static PredictionAndInterpolationController Instance { get; private set; }

        [Header("Interpolation Settings")]
        [Tooltip("Interpolation delay in seconds to absorb network jitter and packet delay variance.")]
        [SerializeField] private float _interpolationDelaySeconds = 0.075f; // 75ms buffer

        [Header("Reconciliation Settings")]
        [Tooltip("Maximum allowed position error before hard snapping is forced.")]
        [SerializeField] private float _maxAcceptableErrorMeters = 1.5f;

        [Tooltip("Smooth error decay rate per second.")]
        [SerializeField] private float _errorDecaySpeed = 12f;

        private struct TimestampedSnapshot
        {
            public double ReceivedClientTime;
            public WorldSnapshotData Snapshot;

            public TimestampedSnapshot(double time, WorldSnapshotData snapshot)
            {
                ReceivedClientTime = time;
                Snapshot = snapshot;
            }
        }

        private readonly List<TimestampedSnapshot> _snapshotBuffer = new List<TimestampedSnapshot>(32);
        private const int MaxBufferSize = 30;

        // Local reconciliation error offset
        private Vector3 _reconciliationErrorOffset = Vector3.zero;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void PushSnapshot(WorldSnapshotData snapshot)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            _snapshotBuffer.Add(new TimestampedSnapshot(now, snapshot));

            if (_snapshotBuffer.Count > MaxBufferSize)
            {
                _snapshotBuffer.RemoveAt(0);
            }
        }

        public void Clear()
        {
            _snapshotBuffer.Clear();
            _reconciliationErrorOffset = Vector3.zero;
        }

        /// <summary>
        /// Smoothly interpolates the remote marble's transform between buffered snapshots.
        /// </summary>
        public void UpdateRemoteMarble(MarbleController remoteMarble, int remotePlayerIndex)
        {
            if (remoteMarble == null || _snapshotBuffer.Count == 0) return;

            // If only 1 snapshot, snap directly
            if (_snapshotBuffer.Count == 1)
            {
                CompactMarbleState state = remotePlayerIndex == 0 ? _snapshotBuffer[0].Snapshot.Marble0 : _snapshotBuffer[0].Snapshot.Marble1;
                remoteMarble.transform.position = state.Position;
                return;
            }

            double renderTime = Time.realtimeSinceStartupAsDouble - _interpolationDelaySeconds;

            // Find surrounding snapshots
            TimestampedSnapshot s0 = _snapshotBuffer[0];
            TimestampedSnapshot s1 = _snapshotBuffer[_snapshotBuffer.Count - 1];

            // If renderTime is newer than newest snapshot, extrapolate smoothly
            if (renderTime >= s1.ReceivedClientTime)
            {
                CompactMarbleState latest = remotePlayerIndex == 0 ? s1.Snapshot.Marble0 : s1.Snapshot.Marble1;
                if (latest.IsMoving)
                {
                    float dt = (float)(renderTime - s1.ReceivedClientTime);
                    Vector3 extrapolatedPos = (Vector3)latest.Position + (Vector3)latest.Velocity * MathF.Min(dt, 0.10f);
                    remoteMarble.transform.position = Vector3.Lerp(remoteMarble.transform.position, extrapolatedPos, Time.deltaTime * 15f);
                }
                else
                {
                    remoteMarble.transform.position = latest.Position;
                    remoteMarble.Halt();
                }
                return;
            }

            // Find the two snapshots enclosing renderTime
            bool found = false;
            for (int i = 0; i < _snapshotBuffer.Count - 1; i++)
            {
                if (_snapshotBuffer[i].ReceivedClientTime <= renderTime && _snapshotBuffer[i + 1].ReceivedClientTime >= renderTime)
                {
                    s0 = _snapshotBuffer[i];
                    s1 = _snapshotBuffer[i + 1];
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                s0 = _snapshotBuffer[_snapshotBuffer.Count - 2];
                s1 = _snapshotBuffer[_snapshotBuffer.Count - 1];
            }

            double duration = s1.ReceivedClientTime - s0.ReceivedClientTime;
            float t = duration > 0.0001 ? (float)((renderTime - s0.ReceivedClientTime) / duration) : 0f;
            t = Mathf.Clamp01(t);

            CompactMarbleState m0 = remotePlayerIndex == 0 ? s0.Snapshot.Marble0 : s0.Snapshot.Marble1;
            CompactMarbleState m1 = remotePlayerIndex == 0 ? s1.Snapshot.Marble0 : s1.Snapshot.Marble1;

            if (!m1.IsMoving && !m0.IsMoving)
            {
                // Settled at rest
                remoteMarble.transform.position = m1.Position;
                remoteMarble.Halt();
            }
            else
            {
                // Interpolate position and velocity
                Vector3 interpPos = Vector3.Lerp(m0.Position, m1.Position, t);
                Vector3 interpVel = Vector3.Lerp(m0.Velocity, m1.Velocity, t);

                remoteMarble.transform.position = interpPos;
                if (remoteMarble.Rigidbody != null && !remoteMarble.Rigidbody.isKinematic)
                {
                    remoteMarble.Rigidbody.linearVelocity = interpVel;
                }
            }
        }

        /// <summary>
        /// Reconciles local predicted marble state against server authoritative snapshot using smooth exponential error decay.
        /// </summary>
        public void ReconcileLocalMarble(MarbleController localMarble, CompactMarbleState serverState)
        {
            if (localMarble == null) return;

            Vector3 serverPos = serverState.Position;
            Vector3 currentPos = localMarble.transform.position;

            if (!serverState.IsMoving)
            {
                // Authoritative settle: snap to final rest position
                localMarble.transform.position = serverPos;
                localMarble.Halt();
                _reconciliationErrorOffset = Vector3.zero;
                return;
            }

            // Calculate discrepancy
            Vector3 error = serverPos - currentPos;
            float errorDist = error.magnitude;

            if (errorDist > _maxAcceptableErrorMeters)
            {
                // Discrepancy is too large (e.g. client missed a collision event): hard snap
                localMarble.transform.position = serverPos;
                _reconciliationErrorOffset = Vector3.zero;
            }
            else if (errorDist > 0.04f)
            {
                // Smoothly decay error offset over time so camera and visuals don't hitch
                float blend = 1f - Mathf.Exp(-_errorDecaySpeed * Time.deltaTime);
                localMarble.transform.position = Vector3.Lerp(currentPos, serverPos, blend);
            }
        }
    }
}
