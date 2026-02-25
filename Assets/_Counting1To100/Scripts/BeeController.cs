using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

namespace Counting1To100
{
    /// <summary>
    /// Controls the visual and movement behavior of the Bee.
    /// Handles wing rotation, antenna twitch, and horizontal flight with bobbing.
    /// </summary>
    [System.Serializable]
    public struct BeeSpriteData
    {
        public SpriteRenderer Renderer;
        public int OriginalOrder;
    }

    public class BeeController : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _numberText;

        [Header("Upper Wings")]
        [SerializeField] private Transform _upperLeftWing;
        [SerializeField] private Transform _upperRightWing;
        [Tooltip("Maximum rotation angle for upper wings.")]
        [SerializeField] private float _upperWingAngle = 45f;
        [Tooltip("Flutter speed for upper wings.")]
        [SerializeField] private float _upperFlutterSpeed = 20f;

        [Header("Lower Wings")]
        [SerializeField] private Transform _lowerLeftWing;
        [SerializeField] private Transform _lowerRightWing;
        [Tooltip("Maximum rotation angle for lower wings.")]
        [SerializeField] private float _lowerWingAngle = 30f;
        [Tooltip("Flutter speed for lower wings.")]
        [SerializeField] private float _lowerFlutterSpeed = 15f;

        [Header("Antennas")]
        [SerializeField] private Transform _leftAntenna;
        [SerializeField] private Transform _rightAntenna;
        [Tooltip("Speed of the antenna twitch.")]
        [SerializeField] private float _antennaTwitchSpeed = 10f;
        [Tooltip("Twitch strength on X axis.")]
        [SerializeField] private float _antennaTwitchX = 0f;
        [Tooltip("Twitch strength on Y axis.")]
        [SerializeField] private float _antennaTwitchY = 0f;
        [Tooltip("Twitch strength on Z axis.")]
        [SerializeField] private float _antennaTwitchZ = 5f;

        [Header("Movement")]
        // Controlled by Spawner now
        private float _bobFrequency;
        private float _bobAmplitude;

        [Header("Visuals & Rejection")]
        [SerializeField] private System.Collections.Generic.List<BeeSpriteData> _beeSprites;
        [SerializeField] private float _rejectYOffset = -2f;
        [SerializeField] private int _rejectSortingOrderBase = 12;

        [Header("Game Logic")]
        public int Number { get; private set; }
        private bool _isDropped = false;
        private Rigidbody2D _rb;
        private RectTransform _rectTransform;
        private Coroutine _moveCoroutine;
        
        // Event for Pooled Despawning
        public event System.Action<BeeController> OnDespawn;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic; // Fly initially

            if (_upperLeftWing) _startRotUL = _upperLeftWing.localRotation;
            if (_upperRightWing) _startRotUR = _upperRightWing.localRotation;
            if (_lowerLeftWing) _startRotLL = _lowerLeftWing.localRotation;
            if (_lowerRightWing) _startRotLR = _lowerRightWing.localRotation;
            if (_leftAntenna) _startRotLA = _leftAntenna.localRotation;
            if (_rightAntenna) _startRotRA = _rightAntenna.localRotation;

            // Initialize/Cache original sorting orders into the struct list if they weren't set in Inspector
            for (int i = 0; i < _beeSprites.Count; i++)
            {
                var data = _beeSprites[i];
                if (data.Renderer != null)
                {
                    data.OriginalOrder = data.Renderer.sortingOrder;
                    _beeSprites[i] = data; // Assign back to list (struct)
                }
            }
        }

        private void OnEnable()
        {
            StartCoroutine(AnimationRoutine());
        }

        private System.Collections.IEnumerator AnimationRoutine()
        {
            while (true)
            {
                HandleWingFlutter();
                HandleAntennaTwitch();
                yield return null;
            }
        }

        public void Initialize(Vector3 endPosition, float speed, float bobFrequency, float minBobAmplitude, float maxBobAmplitude)
        {
            // Reset State for Pooling
            _isDropped = false;
            ResetSortingOrders();
            transform.localScale = Vector3.one; 
            
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.linearVelocity = Vector2.zero; // Unity 6 / 2023+ uses linearVelocity, else velocity
                _rb.angularVelocity = 0f;
                // Reset rotation to neutral
                transform.rotation = Quaternion.identity; 
            }

            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            
            // Calculate duration based on passed Speed and Distance
            Vector3 startPos = _rectTransform != null ? _rectTransform.anchoredPosition3D : transform.position;
            float distance = Vector3.Distance(startPos, endPosition);
            
            // Avoid divide by zero, ensure min speed
            float validSpeed = speed > 0 ? speed : 1f; 
            float duration = distance / validSpeed;

            _moveCoroutine = StartCoroutine(MoveRoutine(endPosition, duration, bobFrequency, minBobAmplitude, maxBobAmplitude));
        }

        private System.Collections.IEnumerator MoveRoutine(Vector3 endPosition, float duration, float bobFrequency, float minBobAmplitude, float maxBobAmplitude)
        {
            float elapsed = 0f;
            Vector3 startPosition = _rectTransform != null ? _rectTransform.anchoredPosition3D : transform.position;
            
            // Use a random phase offset so all bees don't bob in perfect sync
            float bobPhase = Random.Range(0f, 2f * Mathf.PI);
            float noiseSeed = Random.Range(0f, 100f);

            while (elapsed < duration)
            {
                if (_isDropped) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Linear Lerp for consistent horizontal speed
                Vector3 currentBasePos = Vector3.Lerp(startPosition, endPosition, t);
                
                // Calculate Dynamic Amplitude using Perlin Noise
                // Frequency of 1.0f means smooth variation over ~1 second scale
                float noise = Mathf.PerlinNoise(Time.time * 1.0f + noiseSeed, 0f); 
                float currentAmplitude = Mathf.Lerp(minBobAmplitude, maxBobAmplitude, noise);

                // Add Bobbing
                float yOffset = Mathf.Sin((Time.time * bobFrequency) + bobPhase) * currentAmplitude;
                Vector3 bobbedPos = currentBasePos + new Vector3(0, yOffset, 0);

                if (_rectTransform != null)
                {
                    transform.position = bobbedPos; 
                }
                else
                {
                    transform.position = bobbedPos;
                }

                yield return null;
            }

            // Reached End
            if (!_isDropped)
            {
                Despawn(); // Return to pool
            }
        }

        public void Despawn()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            ResetSortingOrders();
            OnDespawn?.Invoke(this);
        }

        private void ResetSortingOrders()
        {
            foreach (var data in _beeSprites)
            {
                if (data.Renderer != null)
                {
                    data.Renderer.sortingOrder = data.OriginalOrder;
                }
            }
        }

        public void SetNumber(int number)
        {
            Number = number;
            if (_numberText != null)
            {
                _numberText.text = number.ToString();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Required for OnPointerClick to work in some input modules
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isDropped)
            {
                IDropTarget closestTarget = GetClosestTarget();
                if (closestTarget != null)
                {
                    DropTo(closestTarget);
                }
                else
                {
                    Debug.LogWarning("No Drop Targets found!");
                }
            }
        }

        private IDropTarget GetClosestTarget()
        {
            if (JarManager.Instance != null)
            {
                return JarManager.Instance.GetClosestDropTarget(transform.position);
            }
            return null;
        }

        public void DropTo(IDropTarget target)
        {
            _isDropped = true;
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            
            // Start Tween to Target
            _moveCoroutine = StartCoroutine(DropRoutine(target));
        }

        public void RejectFromJar()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(RejectRoutine());
        }

        private System.Collections.IEnumerator RejectRoutine()
        {
            // 1. Change sorting orders to be above the jar (11)
            // Determine minimum current order to maintain relative offsets
            int minOrder = int.MaxValue;
            foreach (var data in _beeSprites)
            {
                if (data.Renderer != null && data.Renderer.sortingOrder < minOrder)
                    minOrder = data.Renderer.sortingOrder;
            }

            int offset = _rejectSortingOrderBase - minOrder;
            foreach (var data in _beeSprites)
            {
                if (data.Renderer != null) data.Renderer.sortingOrder += offset;
            }

            // 2. Move away (down) from current position (the jar)
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + new Vector3(0, _rejectYOffset, 0);
            
            float duration = 0.5f; // Fast rejection
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            Despawn();
        }

        private System.Collections.IEnumerator DropRoutine(IDropTarget target)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = target.DropTarget.position;
            // Get speed from Manager or default
            float speed = JarManager.Instance != null ? JarManager.Instance.DropSpeed : 500f;
            
            float distance = Vector3.Distance(startPos, endPos);
            float duration = distance / speed; 
            float elapsed = 0f;

            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 1. Move Linear
                transform.position = Vector3.Lerp(startPos, endPos, t);

                // 2. Scale Down (Shrink into Jar) - Trigger in last 30%
                if (t > 0.7f) 
                {
                     float scaleT = (t - 0.7f) / 0.3f; // Normalize 0 to 1 over the last 30%
                     transform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.35f, scaleT);
                }

                yield return null;
            }

            // Ensure Scale is Target and Position is Target
            transform.position = endPos;
            transform.localScale = Vector3.one * 0.35f;
            
            // Manual Callback to Interaction
            target.ReceiveDrop(this);
        }

        public void BecomeDecoration()
        {
            // Stop logic
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            if (_rb) _rb.simulated = false;
            if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;
            
            // Keep script active for animations
        }

        // --- Visual Animation Helpers ---

        private Quaternion _startRotUL, _startRotUR, _startRotLL, _startRotLR, _startRotLA, _startRotRA;

        private void HandleAntennaTwitch()
        {
            float sin = Mathf.Sin(Time.time * _antennaTwitchSpeed);
            float x = sin * _antennaTwitchX;
            float y = sin * _antennaTwitchY;
            float z = sin * _antennaTwitchZ;

            if (_leftAntenna) _leftAntenna.localRotation = _startRotLA * Quaternion.Euler(x, y, z);
            if (_rightAntenna) _rightAntenna.localRotation = _startRotRA * Quaternion.Euler(x, -y, -z);
        }

        private void HandleWingFlutter()
        {
            float upperAngle = Mathf.Sin(Time.time * _upperFlutterSpeed) * _upperWingAngle;
            float lowerAngle = Mathf.Sin(Time.time * _lowerFlutterSpeed) * _lowerWingAngle;

            if (_upperLeftWing) _upperLeftWing.localRotation = _startRotUL * Quaternion.Euler(0, 0, upperAngle);
            if (_upperRightWing) _upperRightWing.localRotation = _startRotUR * Quaternion.Euler(0, 0, -upperAngle);
            if (_lowerLeftWing) _lowerLeftWing.localRotation = _startRotLL * Quaternion.Euler(0, 0, lowerAngle);
            if (_lowerRightWing) _lowerRightWing.localRotation = _startRotLR * Quaternion.Euler(0, 0, -lowerAngle);
        }
    }
}
