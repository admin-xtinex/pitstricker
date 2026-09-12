using System.Collections;
using UnityEngine;
using PitStriker.Gameplay;
using PitStriker.Physics;
using PitStriker.Input;

namespace PitStriker.Visuals
{
    /// <summary>
    /// Responsive 3D stylized character mascot ("Pip" the Striker Caddy).
    /// Features exaggerated cartoon proportions, procedural expressive anatomy,
    /// physics-assisted secondary motion, anticipation, follow-through, and fluid state transitions.
    /// Reacts dynamically to player aiming, launches, collisions, and pit captures.
    /// </summary>
    [ExecuteAlways]
    public class StylizedCharacterMascot : MonoBehaviour
    {
        public enum MascotState
        {
            Idle,
            AimingAnticipation,
            StrikeFollowThrough,
            RollingSuspense,
            PitCelebration,
            NearMissGasp
        }

        [Header("Character Identity")]
        [SerializeField] private string _characterName = "Pip the Striker Caddy";
        public string CharacterName => _characterName;
        [SerializeField] private Color _bodyColor = new Color(0.95f, 0.52f, 0.22f); // Vibrant warm orange
        [SerializeField] private Color _capColor = new Color(0.15f, 0.45f, 0.95f);  // Striker royal blue

        [Header("Current State")]
        [SerializeField] private MascotState _currentState = MascotState.Idle;
        public MascotState CurrentState => _currentState;

        // Hierarchical Skeletal Node Transforms (Procedural Rig)
        private Transform _rootNode;
        private Transform _bodyNode;
        private Transform _headNode;
        private Transform _leftEyeNode;
        private Transform _rightEyeNode;
        private Transform _leftArmNode;
        private Transform _rightArmNode;
        private Transform _capNode;

        // Secondary Motion Physics Springs
        private float _hatSpringOffset = 0f;
        private float _hatSpringVel = 0f;
        private float _bodySquash = 0f;
        private float _bodySquashVel = 0f;
        private float _blinkTimer = 2.5f;

        // State Tracking
        private float _stateTimer = 0f;
        private MarbleController _watchedMarble = null;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;

        private void Awake()
        {
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
            Transform existing = transform.Find("Pip_Rig");
            if (existing != null)
            {
                _rootNode = existing;
                _bodyNode = _rootNode.Find("Body");
                if (_bodyNode != null)
                {
                    _headNode = _bodyNode.Find("Head");
                    if (_headNode != null)
                    {
                        _capNode = _headNode.Find("Cap");
                        _leftEyeNode = _headNode.Find("LeftEye");
                        _rightEyeNode = _headNode.Find("RightEye");
                    }
                    _leftArmNode = _bodyNode.Find("LeftArm");
                    _rightArmNode = _bodyNode.Find("RightArm");
                }
            }
            else
            {
                BuildProceduralCharacter();
            }
        }

        public void InitInEditor()
        {
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
            if (transform.Find("Pip_Rig") == null)
            {
                BuildProceduralCharacter();
            }
        }

        private void OnEnable()
        {
            SwipeLaunchController.OnPowerChanged += HandleAimPowerChanged;
            PitZone.OnMarbleSunk += HandleMarbleSunk;
            TurnManager.OnActivePlayerChanged += HandleActivePlayerChanged;
            TurnManager.OnStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            SwipeLaunchController.OnPowerChanged -= HandleAimPowerChanged;
            PitZone.OnMarbleSunk -= HandleMarbleSunk;
            TurnManager.OnActivePlayerChanged -= HandleActivePlayerChanged;
            TurnManager.OnStateChanged -= HandleGameStateChanged;
        }

        private void BuildProceduralCharacter()
        {
            // Root
            _rootNode = new GameObject("Pip_Rig").transform;
            _rootNode.SetParent(transform, false);

            Shader toonShader = Shader.Find("Pit Striker/Stylized Toon PBR");
            if (toonShader == null) toonShader = Shader.Find("Universal Render Pipeline/Lit");

            Material bodyMat = new Material(toonShader);
            bodyMat.color = _bodyColor;
            bodyMat.SetFloat("_Smoothness", 0.65f);
            if (bodyMat.HasProperty("_RimColor")) bodyMat.SetColor("_RimColor", new Color(1f, 0.9f, 0.7f));

            Material capMat = new Material(toonShader);
            capMat.color = _capColor;
            capMat.SetFloat("_Smoothness", 0.55f);

            Material eyeWhiteMat = new Material(toonShader);
            eyeWhiteMat.color = Color.white;
            eyeWhiteMat.SetFloat("_Smoothness", 0.95f);

            Material pupilMat = new Material(toonShader);
            pupilMat.color = new Color(0.08f, 0.08f, 0.12f);
            pupilMat.SetFloat("_Smoothness", 0.95f);

            // 1. Chunky Body
            GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bodyObj.name = "Body";
            bodyObj.transform.SetParent(_rootNode, false);
            bodyObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            bodyObj.transform.localScale = new Vector3(0.55f, 0.48f, 0.50f);
            StripCollider(bodyObj);
            bodyObj.GetComponent<Renderer>().sharedMaterial = bodyMat;
            _bodyNode = bodyObj.transform;

            // 2. Oversized Expressive Head
            GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headObj.name = "Head";
            headObj.transform.SetParent(_bodyNode, false);
            headObj.transform.localPosition = new Vector3(0f, 0.95f, 0.05f);
            headObj.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
            StripCollider(headObj);
            headObj.GetComponent<Renderer>().sharedMaterial = bodyMat;
            _headNode = headObj.transform;

            // 3. Stylized Striker Cap
            GameObject capObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capObj.name = "Cap";
            capObj.transform.SetParent(_headNode, false);
            capObj.transform.localPosition = new Vector3(0f, 0.52f, -0.05f);
            capObj.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            capObj.transform.localScale = new Vector3(0.85f, 0.16f, 0.85f);
            StripCollider(capObj);
            capObj.GetComponent<Renderer>().sharedMaterial = capMat;
            _capNode = capObj.transform;

            // Cap Visor / Brim
            GameObject brimObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brimObj.name = "Brim";
            brimObj.transform.SetParent(_capNode, false);
            brimObj.transform.localPosition = new Vector3(0f, -0.08f, 0.42f);
            brimObj.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
            brimObj.transform.localScale = new Vector3(0.82f, 0.08f, 0.45f);
            StripCollider(brimObj);
            brimObj.GetComponent<Renderer>().sharedMaterial = capMat;

            // 4. Large Expressive Eyes
            _leftEyeNode = CreateEye("LeftEye", new Vector3(-0.24f, 0.12f, 0.45f), eyeWhiteMat, pupilMat);
            _rightEyeNode = CreateEye("RightEye", new Vector3(0.24f, 0.12f, 0.45f), eyeWhiteMat, pupilMat);

            // 5. Expressive Arms
            _leftArmNode = CreateArm("LeftArm", new Vector3(-0.35f, 0.10f, 0.05f), bodyMat, capMat);
            _rightArmNode = CreateArm("RightArm", new Vector3(0.35f, 0.10f, 0.05f), bodyMat, capMat);
        }

        private static void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        private Transform CreateEye(string name, Vector3 localPos, Material scleraMat, Material pupilMat)
        {
            GameObject eyeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeObj.name = name;
            eyeObj.transform.SetParent(_headNode, false);
            eyeObj.transform.localPosition = localPos;
            eyeObj.transform.localScale = new Vector3(0.26f, 0.32f, 0.22f);
            StripCollider(eyeObj);
            eyeObj.GetComponent<Renderer>().sharedMaterial = scleraMat;

            // Pupil
            GameObject pupilObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pupilObj.name = "Pupil";
            pupilObj.transform.SetParent(eyeObj.transform, false);
            pupilObj.transform.localPosition = new Vector3(0f, 0f, 0.48f);
            pupilObj.transform.localScale = new Vector3(0.55f, 0.55f, 0.35f);
            StripCollider(pupilObj);
            pupilObj.GetComponent<Renderer>().sharedMaterial = pupilMat;

            return eyeObj.transform;
        }

        private Transform CreateArm(string name, Vector3 localPos, Material armMat, Material gloveMat)
        {
            GameObject armObj = new GameObject(name);
            armObj.transform.SetParent(_bodyNode, false);
            armObj.transform.localPosition = localPos;

            // Limb
            GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            limb.name = "Limb";
            limb.transform.SetParent(armObj.transform, false);
            limb.transform.localPosition = new Vector3(0f, -0.18f, 0.08f);
            limb.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            limb.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            StripCollider(limb);
            limb.GetComponent<Renderer>().sharedMaterial = armMat;

            // Chunky Glove / Hand
            GameObject glove = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glove.name = "Glove";
            glove.transform.SetParent(armObj.transform, false);
            glove.transform.localPosition = new Vector3(0f, -0.32f, 0.15f);
            glove.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
            StripCollider(glove);
            glove.GetComponent<Renderer>().sharedMaterial = gloveMat;

            return armObj.transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _stateTimer += dt;

            // Handle Eye Blinking
            _blinkTimer -= dt;
            if (_blinkTimer <= 0f)
            {
                StartCoroutine(BlinkRoutine());
                _blinkTimer = Random.Range(2.5f, 5.0f);
            }

            // Update Hat and Body Springs
            UpdateSecondaryPhysics(dt);

            // Execute State Animation
            switch (_currentState)
            {
                case MascotState.Idle:
                    AnimateIdle(dt);
                    break;
                case MascotState.AimingAnticipation:
                    AnimateAiming(dt);
                    break;
                case MascotState.StrikeFollowThrough:
                    AnimateFollowThrough(dt);
                    break;
                case MascotState.RollingSuspense:
                    AnimateSuspense(dt);
                    break;
                case MascotState.PitCelebration:
                    AnimateCelebration(dt);
                    break;
                case MascotState.NearMissGasp:
                    AnimateGasp(dt);
                    break;
            }
        }

        private void UpdateSecondaryPhysics(float dt)
        {
            // Spring for hat bounce: F = -k*x - c*v
            float hatK = 45f;
            float hatDamp = 6f;
            _hatSpringVel += (-hatK * _hatSpringOffset - hatDamp * _hatSpringVel) * dt;
            _hatSpringOffset += _hatSpringVel * dt;
            if (_capNode != null)
            {
                _capNode.localPosition = new Vector3(0f, 0.52f + _hatSpringOffset, -0.05f);
            }

            // Spring for body squash
            float bodyK = 32f;
            float bodyDamp = 7f;
            _bodySquashVel += (-bodyK * _bodySquash - bodyDamp * _bodySquashVel) * dt;
            _bodySquash += _bodySquashVel * dt;
        }

        private void AnimateIdle(float dt)
        {
            float t = Time.time * 2.2f;
            // Rhythmic breathing squash and stretch
            float breath = Mathf.Sin(t) * 0.05f;
            _bodyNode.localScale = new Vector3(0.55f * (1f - breath * 0.5f), 0.48f * (1f + breath), 0.50f * (1f - breath * 0.5f));

            // Gentle head tilt and look around
            float sway = Mathf.Sin(t * 0.7f) * 6f;
            _headNode.localRotation = Quaternion.Euler(3f + breath * 12f, sway, breath * 8f);

            // Arms resting with subtle breathing sway
            _leftArmNode.localRotation = Quaternion.Euler(15f + breath * 20f, 0f, 10f);
            _rightArmNode.localRotation = Quaternion.Euler(15f + breath * 20f, 0f, -10f);
        }

        private void AnimateAiming(float dt)
        {
            // Anticipation Windup: Lean back against aim direction, squash down like a coiled spring
            _bodyNode.localScale = new Vector3(0.62f, 0.38f, 0.58f); // Crouched squash
            _headNode.localRotation = Quaternion.Euler(-12f, 0f, 0f);  // Eyes up focused

            // Hands forward in intense gripping ready stance
            _leftArmNode.localRotation = Quaternion.Euler(-35f, 25f, 15f);
            _rightArmNode.localRotation = Quaternion.Euler(-35f, -25f, -15f);

            if (_watchedMarble != null)
            {
                LookAtTarget(_watchedMarble.transform.position);
            }
        }

        private void AnimateFollowThrough(float dt)
        {
            // Explosive leap forward, stretching body, arms flung forward!
            float leapT = Mathf.Clamp01(_stateTimer / 0.4f);
            float stretch = Mathf.Sin(leapT * Mathf.PI) * 0.28f;

            _bodyNode.localScale = new Vector3(0.50f * (1f - stretch * 0.5f), 0.48f * (1f + stretch * 1.5f), 0.50f * (1f - stretch * 0.5f));
            _headNode.localRotation = Quaternion.Euler(15f, 0f, 0f);

            _leftArmNode.localRotation = Quaternion.Euler(60f, 0f, 20f);
            _rightArmNode.localRotation = Quaternion.Euler(60f, 0f, -20f);

            if (leapT >= 1f && _watchedMarble != null && _watchedMarble.IsMoving)
            {
                SetState(MascotState.RollingSuspense);
            }
        }

        private void AnimateSuspense(float dt)
        {
            float t = Time.time * 6f;
            // Tiptoe suspense wobble
            float wobble = Mathf.Sin(t) * 0.08f;
            _bodyNode.localScale = new Vector3(0.52f, 0.52f + wobble, 0.48f);

            // Hands clutching face/cheeks
            _leftArmNode.localRotation = Quaternion.Euler(-70f, 35f, 20f);
            _rightArmNode.localRotation = Quaternion.Euler(-70f, -35f, -20f);

            if (_watchedMarble != null)
            {
                LookAtTarget(_watchedMarble.transform.position);
                if (!_watchedMarble.IsMoving)
                {
                    // Check if it stopped safely or sunk
                    SetState(MascotState.Idle);
                }
            }
            else
            {
                SetState(MascotState.Idle);
            }
        }

        private void AnimateCelebration(float dt)
        {
            float t = _stateTimer * 4.5f;
            // High-energy jumping and cheering!
            float jump = Mathf.Abs(Mathf.Sin(t)) * 0.45f;
            transform.position = _homePosition + Vector3.up * jump;

            // Arms pumping in victory!
            float armPump = Mathf.Sin(t * 2f) * 35f;
            _leftArmNode.localRotation = Quaternion.Euler(-110f + armPump, 0f, 15f);
            _rightArmNode.localRotation = Quaternion.Euler(-110f - armPump, 0f, -15f);

            _headNode.localRotation = Quaternion.Euler(-20f, Mathf.Sin(t) * 20f, 0f);

            if (_stateTimer > 3.0f)
            {
                transform.position = _homePosition;
                SetState(MascotState.Idle);
            }
        }

        private void AnimateGasp(float dt)
        {
            // Slumped head clutch
            _bodyNode.localScale = new Vector3(0.60f, 0.40f, 0.55f);
            _headNode.localRotation = Quaternion.Euler(25f, Mathf.Sin(Time.time * 8f) * 12f, 0f);
            _leftArmNode.localRotation = Quaternion.Euler(-65f, 40f, 10f);
            _rightArmNode.localRotation = Quaternion.Euler(-65f, -40f, -10f);

            if (_stateTimer > 2.0f)
            {
                SetState(MascotState.Idle);
            }
        }

        private void LookAtTarget(Vector3 targetPos)
        {
            Vector3 dir = (targetPos - _headNode.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                _headNode.rotation = Quaternion.Slerp(_headNode.rotation, targetRot, Time.deltaTime * 8f);
            }
        }

        private IEnumerator BlinkRoutine()
        {
            if (_leftEyeNode == null || _rightEyeNode == null) yield break;

            Vector3 leftScale = _leftEyeNode.localScale;
            Vector3 rightScale = _rightEyeNode.localScale;

            // Squeeze eyes shut
            _leftEyeNode.localScale = new Vector3(leftScale.x, 0.04f, leftScale.z);
            _rightEyeNode.localScale = new Vector3(rightScale.x, 0.04f, rightScale.z);

            yield return new WaitForSeconds(0.12f);

            if (_leftEyeNode != null) _leftEyeNode.localScale = leftScale;
            if (_rightEyeNode != null) _rightEyeNode.localScale = rightScale;
        }

        public void SetState(MascotState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
            _stateTimer = 0f;

            if (newState == MascotState.StrikeFollowThrough)
            {
                _hatSpringVel = 1.2f; // Cap leaps up with secondary momentum
            }
            else if (newState == MascotState.PitCelebration)
            {
                _hatSpringVel = 2.0f;
            }
        }

        private void HandleAimPowerChanged(float power)
        {
            if (power > 0.05f)
            {
                SetState(MascotState.AimingAnticipation);
            }
            else if (_currentState == MascotState.AimingAnticipation)
            {
                // Released!
                SetState(MascotState.StrikeFollowThrough);
            }
        }

        private void HandleActivePlayerChanged(TurnManager.PlayerData player)
        {
            if (player != null && player.marble != null)
            {
                _watchedMarble = player.marble;
            }
        }

        private void HandleGameStateChanged(TurnManager.GameState state)
        {
            if (state == TurnManager.GameState.Rolling)
            {
                SetState(MascotState.RollingSuspense);
            }
        }

        private void HandleMarbleSunk(PitZone pit, MarbleController marble)
        {
            SetState(MascotState.PitCelebration);
        }
    }
}
