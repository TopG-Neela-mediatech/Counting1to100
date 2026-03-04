using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Counting1To100.DragAndDropMode
{
    [System.Serializable]
    public struct BugSpriteData
    {
        public SpriteRenderer Renderer;
        public int OriginalOrder;
    }

    public class BugController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _numberText;

        [Header("Upper Wings")]
        [SerializeField] private Transform _upperLeftWing;
        [SerializeField] private Transform _upperRightWing;
        [SerializeField] private float _upperWingAngle = 45f;
        [SerializeField] private float _upperFlutterSpeed = 20f;

        [Header("Lower Wings")]
        [SerializeField] private Transform _lowerLeftWing;
        [SerializeField] private Transform _lowerRightWing;
        [SerializeField] private float _lowerWingAngle = 30f;
        [SerializeField] private float _lowerFlutterSpeed = 15f;

        [Header("Antennas")]
        [SerializeField] private Transform _leftAntenna;
        [SerializeField] private Transform _rightAntenna;
        [SerializeField] private float _antennaTwitchSpeed = 10f;
        [SerializeField] private float _antennaTwitchX = 0f;
        [SerializeField] private float _antennaTwitchY = 0f;
        [SerializeField] private float _antennaTwitchZ = 5f;

        [Header("Animation Settings")]
        [SerializeField] private float _dropScale = 0.35f;
        [SerializeField] private float _noiseFrequency = 1.0f;

        [Header("Wandering Settings (Near Flower)")]
        [SerializeField] private float _jarMinX = -0.6f;
        [SerializeField] private float _jarMaxX = 0.6f;
        [SerializeField] private float _jarMinY = -0.25f;
        [SerializeField] private float _jarMaxY = 1.25f;
        [SerializeField] private float _wanderSpeed = 0.5f;
        [SerializeField] private float _wanderChangeInterval = 2f;
        [SerializeField] private float _jumpHeight = 5.0f; // Increased default power

        [Header("Visuals")]
        [SerializeField] private System.Collections.Generic.List<BugSpriteData> _bugSprites;
        [SerializeField] private Canvas _textCanvas;
        [SerializeField] private int _baseSortingOrderBonus = 15; // Above jars
        [SerializeField] private int _dragSortingOrderBonus = 50;

        public int Number { get; private set; }
        public event System.Action<BugController> OnDespawn;

        private bool _isDropped = false;
        private Rigidbody2D _rb;
        private Coroutine _moveCoroutine;
        private Coroutine _wanderCoroutine;
        
        private int _originalTextSortingOrder;
        private Camera _mainCamera;
        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        
        private Quaternion _startRotUL, _startRotUR, _startRotLL, _startRotLR, _startRotLA, _startRotRA;
        
        // Flight data
        private Vector3 _flightEndPosition;
        private float _flightSpeed;
        private float _flightDuration;
        private float _flightElapsed;
        private Vector3 _flightStartPosition;
        private Vector3 _dragStartPosition;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;

            if (_upperLeftWing) _startRotUL = _upperLeftWing.localRotation;
            if (_upperRightWing) _startRotUR = _upperRightWing.localRotation;
            if (_lowerLeftWing) _startRotLL = _lowerLeftWing.localRotation;
            if (_lowerRightWing) _startRotLR = _lowerRightWing.localRotation;
            if (_leftAntenna) _startRotLA = _leftAntenna.localRotation;
            if (_rightAntenna) _startRotRA = _rightAntenna.localRotation;

            for (int i = 0; i < _bugSprites.Count; i++)
            {
                var data = _bugSprites[i];
                if (data.Renderer != null)
                {
                    data.OriginalOrder = data.Renderer.sortingOrder;
                    _bugSprites[i] = data;
                }
            }

            if (_textCanvas != null)
            {
                _originalTextSortingOrder = _textCanvas.sortingOrder;
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

        private void OnDisable()
        {
            StopAllCoroutines();
            _moveCoroutine = null;
            _wanderCoroutine = null;
        }

        // --- SPAWNING & FLIGHT FLIGHT ---
        
        public void InitializeFlight(Vector3 endPosition, float speed, Camera mainCamera)
        {
            _mainCamera = mainCamera;
            _isDropped = false;
            
            // Apply base sorting order to appear above jars
            SetSortingOrder(_baseSortingOrderBonus);
            
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            
            _flightStartPosition = transform.position;
            _flightEndPosition = endPosition;
            _flightSpeed = speed > 0 ? speed : 1f;
            float distance = Vector3.Distance(_flightStartPosition, _flightEndPosition);
            _flightDuration = distance / _flightSpeed;
            _flightElapsed = 0f;

            _moveCoroutine = StartCoroutine(CrossScreenFlightRoutine());
        }

        private System.Collections.IEnumerator CrossScreenFlightRoutine()
        {
            float noiseSeed = Random.Range(0f, 100f);

            while (_flightElapsed < _flightDuration)
            {
                if (_isDropped) yield break; // Paused by drag

                if (GameManager.Instance != null && GameManager.Instance.IsTutorialActive)
                {
                    yield return null;
                    continue;
                }

                _flightElapsed += Time.deltaTime;
                float t = _flightElapsed / _flightDuration;
                
                Vector3 currentBasePos = Vector3.Lerp(_flightStartPosition, _flightEndPosition, t);
                
                // Add slight bobbing to flight path
                float noise = Mathf.PerlinNoise(Time.time * _noiseFrequency + noiseSeed, 0f) * 0.5f - 0.25f; 
                transform.position = currentBasePos + new Vector3(0, noise, 0);

                yield return null;
            }

            // Exited screen without being grabbed
            if (!_isDropped)
            {
                Despawn();
            }
        }

        // --- DRAG INTERACTION ---

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isDropped) return;

            // Pause flight
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            
            _dragStartPosition = transform.position;

            // Fetch canvas for coordinate conversion if needed
            if (_parentCanvas == null) _parentCanvas = GetComponentInParent<Canvas>();

            // Visual bump so it appears "held"
            SetSortingOrder(_baseSortingOrderBonus + _dragSortingOrderBonus);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isDropped) return;
            if (_mainCamera == null) return; // Wait for camera to be assigned via InitializeFlight or similar

            if (_rectTransform != null && _parentCanvas != null && _parentCanvas.renderMode != RenderMode.WorldSpace)
            {
                // UI / Canvas space movement
                RectTransformUtility.ScreenPointToWorldPointInRectangle(_rectTransform, eventData.position, _mainCamera, out Vector3 worldPos);
                transform.position = worldPos;
            }
            else
            {
                // World space / Camera space movement
                Vector3 screenPos = eventData.position;
                screenPos.z = Mathf.Abs(_mainCamera.transform.position.z - transform.position.z);
                Vector3 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);
                worldPos.z = 0f;
                transform.position = worldPos;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isDropped) return;

            SetSortingOrder(_baseSortingOrderBonus);

            IDragContainer container = null;
            if (ContainerManager.Instance != null)
            {
                // Find a flower within a certain drop radius
                container = ContainerManager.Instance.GetValidDropContainer(transform.position, 2f); 
            }

            if (container != null)
            {
                // We dropped it close enough to a flower! Let the flower handle it.
                _isDropped = true;
                container.ReceiveDroppedBug(this);
            }
            else
            {
                // Dropped in empty space. Resume crossing the screen from current spot.
                _flightStartPosition = transform.position;
                float remainingDist = Vector3.Distance(_flightStartPosition, _flightEndPosition);
                _flightDuration = remainingDist / _flightSpeed;
                _flightElapsed = 0f;
                
                _moveCoroutine = StartCoroutine(CrossScreenFlightRoutine());
            }
        }

        public void RejectFlight()
        {
            _isDropped = true; // Prevent dragging while it zooms away
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            // Revert parent back to canvas if it was parented to the flower momentarily
            if (_parentCanvas != null) transform.SetParent(_parentCanvas.transform);
            
            _moveCoroutine = StartCoroutine(RejectZoomOutRoutine());
        }

        private System.Collections.IEnumerator RejectZoomOutRoutine()
        {
            float duration = 0.75f;
            float elapsed = 0f;
            Vector3 startP = transform.position;
            
            // Pick a random direction
            float angle = Random.Range(0f, 360f);
            Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f).normalized;
            
            // Move it far enough to definitely be off-screen
            Vector3 targetP = startP + (direction * 30f);

            // Boost sorting order so it doesn't clip behind flowers while rejecting
            SetSortingOrder(_baseSortingOrderBonus + 10);
                
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Ease in for a 'zoom away' effect
                float easeInT = t * t * t; 
                transform.position = Vector3.Lerp(startP, targetP, easeInT);
                yield return null;
            }
            
            Despawn();
        }

        // --- FLOWER BEHAVIOR ---

        public void BecomeDecoration()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            
            // Smoothly move to center and scale down after being parented
            _moveCoroutine = StartCoroutine(SmoothCenterRoutine());
        }

        private System.Collections.IEnumerator SmoothCenterRoutine()
        {
            Vector3 startLocal = transform.localPosition;
            Vector3 startScale = transform.localScale;
            float duration = 0.6f; 
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 1. Core Lerp for horizontal position and scale
                // Linear t for position, we will override Y with the jump
                Vector3 basePos = Vector3.Lerp(startLocal, Vector3.zero, t);
                float scale = Mathf.Lerp(startScale.x, _dropScale, t);

                // 2. Parabolic Jump Calculation
                // offset = height * (4 * t * (1 - t)) -> creates an arc that peaks at t=0.5
                float yOffset = _jumpHeight * (4 * t * (1 - t));
                
                transform.localPosition = new Vector3(basePos.x, basePos.y + yOffset, 0);
                transform.localScale = Vector3.one * scale;

                yield return null;
            }

            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one * _dropScale;

            // Begin revolving/wandering around the newly parented flower head
            if (_wanderCoroutine != null) StopCoroutine(_wanderCoroutine);
            _wanderCoroutine = StartCoroutine(FlowerWanderRoutine());
        }

        private System.Collections.IEnumerator FlowerWanderRoutine()
        {
            Vector3 targetLocalPos = GetRandomFlowerPosition();
            Vector3 velocity = Vector3.zero;

            while (true)
            {
                // SmoothDamp provides a very natural, organic ease-in/ease-out movement
                transform.localPosition = Vector3.SmoothDamp(
                    transform.localPosition, 
                    targetLocalPos, 
                    ref velocity, 
                    _wanderSpeed // Represents "smooth time" here, lower is faster
                );

                // Optional: Rotate slightly towards movement direction
                if (velocity.sqrMagnitude > 0.01f)
                {
                    float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                    Quaternion targetRot = Quaternion.Euler(0, 0, angle - 90f); // Assuming bug faces 'up' visually
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * 5f);
                }

                // If we are close enough to the target, wait and pick a new one
                if (Vector3.Distance(transform.localPosition, targetLocalPos) < 0.05f)
                {
                    yield return new WaitForSeconds(Random.Range(0.5f, _wanderChangeInterval));
                    targetLocalPos = GetRandomFlowerPosition();
                }

                yield return null;
            }
        }

        private Vector3 GetRandomFlowerPosition()
        {
            float x = Random.Range(_jarMinX, _jarMaxX);
            float y = Random.Range(_jarMinY, _jarMaxY);
            return new Vector3(x, y, 0f);
        }

        // --- UTILS ---

        public void SetNumber(int number)
        {
            Number = number;
            if (_numberText != null) _numberText.text = number.ToString();
        }

        public void Despawn()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            if (_wanderCoroutine != null) StopCoroutine(_wanderCoroutine);
            ResetSortingOrders();
            OnDespawn?.Invoke(this);
        }

        private void ResetSortingOrders()
        {
            SetSortingOrder(0); // 0 bonus = original orders
        }

        private void SetSortingOrder(int bonus)
        {
            foreach (var data in _bugSprites)
            {
                if (data.Renderer != null) 
                    data.Renderer.sortingOrder = data.OriginalOrder + bonus;
            }
            if (_textCanvas != null) 
                _textCanvas.sortingOrder = _originalTextSortingOrder + bonus;
        }

        // --- Visual Animation Helpers ---

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
