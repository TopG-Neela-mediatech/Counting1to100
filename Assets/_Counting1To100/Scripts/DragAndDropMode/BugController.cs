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

        [Header("Animation Settings")]
        [SerializeField] private float _dropScale = 0.35f;
        [SerializeField] private float _noiseFrequency = 1.0f;

        [Header("Wandering Settings (Near Flower)")]
        [SerializeField] private float _wanderRadius = 1.5f;
        [SerializeField] private float _wanderSpeed = 0.5f;
        [SerializeField] private float _wanderChangeInterval = 2f;

        [Header("Visuals")]
        [SerializeField] private System.Collections.Generic.List<BugSpriteData> _bugSprites;
        [SerializeField] private Canvas _textCanvas;
        [SerializeField] private int _dragSortingOrderBonus = 50;

        public int Number { get; private set; }
        public event System.Action<BugController> OnDespawn;

        private bool _isDropped = false;
        private Rigidbody2D _rb;
        private Coroutine _moveCoroutine;
        private Coroutine _wanderCoroutine;
        
        private int _originalTextSortingOrder;
        private Camera _mainCamera;
        
        // Flight data
        private Vector3 _flightEndPosition;
        private float _flightSpeed;
        private float _flightDuration;
        private float _flightElapsed;
        private Vector3 _flightStartPosition;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;

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

        private void OnDisable()
        {
            StopAllCoroutines();
            _moveCoroutine = null;
            _wanderCoroutine = null;
        }

        // --- SPAWNING & FLIGHT FLIGHT ---
        
        public void InitializeFlight(Vector3 endPosition, float speed)
        {
            _isDropped = false;
            ResetSortingOrders();
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

            // Visual bump so it appears "held"
            foreach (var data in _bugSprites)
                if (data.Renderer != null) data.Renderer.sortingOrder = data.OriginalOrder + _dragSortingOrderBonus;
            
            if (_textCanvas != null) _textCanvas.sortingOrder = _originalTextSortingOrder + _dragSortingOrderBonus;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isDropped) return;

            // Follow finger/mouse
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(eventData.position);
            worldPos.z = 0f;
            transform.position = worldPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isDropped) return;

            ResetSortingOrders();

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
            // Bug was wrong number. Fly upwards and fade out or just despawn after a bit
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(RejectRoutine());
        }

        private System.Collections.IEnumerator RejectRoutine()
        {
            // Simple upward float away
            float elapsed = 0;
            Vector3 startP = transform.position;
            Vector3 endP = startP + new Vector3(0, 3f, 0);

            // Boost sorting order so it doesn't clip behind flowers while rejecting
            foreach (var data in _bugSprites)
                if (data.Renderer != null) data.Renderer.sortingOrder = data.OriginalOrder + 10;
                
            while(elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startP, endP, elapsed / 1f);
                yield return null;
            }
            Despawn();
        }

        // --- FLOWER BEHAVIOR ---

        public void BecomeDecoration()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            
            // Shrink down visually to fit on flower
            transform.localScale = Vector3.one * _dropScale;
            transform.localPosition = Vector3.zero;

            // Begin revolving/wandering around the newly parented flower head
            if (_wanderCoroutine != null) StopCoroutine(_wanderCoroutine);
            _wanderCoroutine = StartCoroutine(FlowerWanderRoutine());
        }

        private System.Collections.IEnumerator FlowerWanderRoutine()
        {
            Vector3 targetLocalPos = GetRandomFlowerPosition();

            while (true)
            {
                while (Vector3.Distance(transform.localPosition, targetLocalPos) > 0.05f)
                {
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetLocalPos, _wanderSpeed * Time.deltaTime);
                    yield return null;
                }

                yield return new WaitForSeconds(Random.Range(0.5f, _wanderChangeInterval));

                targetLocalPos = GetRandomFlowerPosition();
            }
        }

        private Vector3 GetRandomFlowerPosition()
        {
            // Instead of a box bounds, you might want simple random points in a circle (radius) around the flower head (0,0,0)
            Vector2 randomCircle = Random.insideUnitCircle * _wanderRadius;
            return new Vector3(randomCircle.x, randomCircle.y, 0f);
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
            foreach (var data in _bugSprites)
            {
                if (data.Renderer != null) data.Renderer.sortingOrder = data.OriginalOrder;
            }
            if (_textCanvas != null) _textCanvas.sortingOrder = _originalTextSortingOrder;
        }
    }
}
