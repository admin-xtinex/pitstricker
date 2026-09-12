using System;
using UnityEngine;

namespace PitStriker.Networking.Client
{
    public enum NetworkQualityGrade
    {
        Excellent, // RTT < 50ms
        Good,      // RTT 50-100ms
        Acceptable,// RTT 100-200ms
        Degraded,  // RTT 200-300ms
        Poor       // RTT > 300ms
    }

    /// <summary>
    /// Phase 6 Adaptive Network Optimizer:
    /// Dynamically tracks connection health, RTT, jitter, and adapts the interpolation delay
    /// and snapshot processing to ensure smooth performance on low-end Android hardware.
    /// </summary>
    public class AdaptiveNetworkOptimizer : MonoBehaviour
    {
        public static AdaptiveNetworkOptimizer Instance { get; private set; }

        [Header("Telemetry")]
        [SerializeField] private NetworkQualityGrade _currentGrade = NetworkQualityGrade.Good;
        [SerializeField] private float _smoothedRttMs = 50f;
        [SerializeField] private float _rttJitterMs = 0f;
        [SerializeField] private int _packetsReceivedPerSec = 0;

        public NetworkQualityGrade CurrentGrade => _currentGrade;
        public float SmoothedRttMs => _smoothedRttMs;
        public float RttJitterMs => _rttJitterMs;
        public int PacketsPerSecond => _packetsReceivedPerSec;

        private float _lastRtt = 50f;
        private int _packetCounter = 0;
        private float _packetRateTimer = 0f;

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

        private void Update()
        {
            if (CloudNetworkClient.Instance == null || !CloudNetworkClient.Instance.IsConnected) return;

            float rawRtt = CloudNetworkClient.Instance.RttMs;
            if (rawRtt > 0f)
            {
                // Exponential moving average for smoothed RTT
                _smoothedRttMs = Mathf.Lerp(_smoothedRttMs, rawRtt, Time.unscaledDeltaTime * 2f);
                _rttJitterMs = Mathf.Lerp(_rttJitterMs, Mathf.Abs(rawRtt - _lastRtt), Time.unscaledDeltaTime * 2f);
                _lastRtt = rawRtt;

                UpdateQualityGrade();
            }

            // Measure packet rate
            _packetRateTimer += Time.unscaledDeltaTime;
            if (_packetRateTimer >= 1.0f)
            {
                _packetsReceivedPerSec = _packetCounter;
                _packetCounter = 0;
                _packetRateTimer = 0f;
            }
        }

        public void RecordPacketReceived()
        {
            _packetCounter++;
        }

        private void UpdateQualityGrade()
        {
            if (_smoothedRttMs < 50f)
            {
                _currentGrade = NetworkQualityGrade.Excellent;
            }
            else if (_smoothedRttMs < 100f)
            {
                _currentGrade = NetworkQualityGrade.Good;
            }
            else if (_smoothedRttMs < 200f)
            {
                _currentGrade = NetworkQualityGrade.Acceptable;
            }
            else if (_smoothedRttMs < 300f)
            {
                _currentGrade = NetworkQualityGrade.Degraded;
            }
            else
            {
                _currentGrade = NetworkQualityGrade.Poor;
            }
        }
    }
}
