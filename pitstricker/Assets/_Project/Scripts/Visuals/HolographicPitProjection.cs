using UnityEngine;

namespace PitStriker.Visuals
{
    /// <summary>
    /// In-Pit Number Identifier:
    /// Displays bold, crisp pit numbers ("1", "2", "3") directly inside the bottom basin floor
    /// of each deep circular pit. Eliminates all floating flags/holograms in mid-air,
    /// providing 100% unobstructed visibility across the entire fairway.
    /// </summary>
    public class HolographicPitProjection : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int _pitNumber = 1;

        [Header("In-Pit Basin Marker (Zero Air Obstruction)")]
        [SerializeField] private Color _textColor = new Color(1.0f, 0.94f, 0.65f, 0.95f); // Crisp golden-white
        [SerializeField] private float _floorOffset = -0.185f; // Rests cleanly on the -0.20m deep saucer floor

        private Transform _inPitRoot;
        private TextMesh _numberText;

        public int PitNumber
        {
            get => _pitNumber;
            set
            {
                _pitNumber = value;
                if (_numberText != null) _numberText.text = _pitNumber.ToString();
            }
        }

        private void Awake()
        {
            BuildInPitMarker();
        }

        public void BuildInPitMarker()
        {
            // 1. Destroy any obsolete mid-air floating holograms
            Transform oldVisualRoot = transform.Find("Hologram_VisualRoot");
            if (oldVisualRoot != null) DestroyImmediate(oldVisualRoot.gameObject);

            Transform oldBeam = transform.Find("Hologram_EmitterBeam");
            if (oldBeam != null) DestroyImmediate(oldBeam.gameObject);

            // 2. Create or find In-Pit Floor Marker root
            Transform existingFloorRoot = transform.Find("InPit_Floor_Marker");
            GameObject root = existingFloorRoot != null ? existingFloorRoot.gameObject : new GameObject("InPit_Floor_Marker");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, _floorOffset, 0f);
            root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Laid completely flat on the basin floor facing up (+Y)
            _inPitRoot = root.transform;

            // 3. Crisp bold 3D Text numeral inside the cavity floor
            Transform textTrans = root.transform.Find("InPit_NumberText");
            GameObject textObj = textTrans != null ? textTrans.gameObject : new GameObject("InPit_NumberText");
            textObj.transform.SetParent(root.transform, false);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localRotation = Quaternion.identity;

            _numberText = textObj.GetComponent<TextMesh>();
            if (_numberText == null) _numberText = textObj.AddComponent<TextMesh>();

            _numberText.text = _pitNumber.ToString();
            _numberText.fontSize = 100;
            _numberText.characterSize = 0.075f;
            _numberText.alignment = TextAlignment.Center;
            _numberText.anchor = TextAnchor.MiddleCenter;
            _numberText.color = _textColor;
            _numberText.fontStyle = FontStyle.Bold;

            // Ensure unlit clear rendering inside dark red bowl
            MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }
    }
}