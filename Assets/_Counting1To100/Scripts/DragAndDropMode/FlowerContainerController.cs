using UnityEngine;
using TMPro;

namespace TMKOC.Counting100.DragAndDropMode
{
    public class FlowerContainerController : MonoBehaviour, IDragContainer
    {
        [Header("Settings")]
        [SerializeField] private int _targetNumber;
        [SerializeField] private TextMeshProUGUI _numberText;
        [SerializeField] private Transform _flowerHeadTransform;
        [SerializeField] private SpriteRenderer _flowerSR;
        [SerializeField, Range(1f,2f)] private float _pulseFactor;
        [SerializeField] private GameObject _afterDropEffect;

        [SerializeField] private Sprite _afterDropSpriteChange;

        public Transform ContainerTransform => _flowerHeadTransform != null ? _flowerHeadTransform : transform;
        public int TargetNumber => _targetNumber;
        
        // This assumes each flower takes one bug. If multiple, change the logic
        public bool IsCompleted => ContainerTransform.childCount > 0;

        private Coroutine _pulseCoroutine;
        private Sprite _originalSprite;
        private UnityEngine.Rendering.SortingGroup _tutorialSortingGroup;
        private int _originalSortingOrder = 0;
        
        private Canvas _numberTextCanvas;
        private int _originalCanvasSortingOrder;

        private void Start()
        {
            if (_flowerSR != null) _originalSprite = _flowerSR.sprite;

            if (ContainerManager.Instance != null)
            {
                ContainerManager.Instance.RegisterContainer(this);
            }
        }

        private void OnDestroy()
        {
            if (ContainerManager.Instance != null)
            {
                ContainerManager.Instance.UnregisterContainer(this);
            }
        }

        public void SetTargetNumber(int number)
        {
            _targetNumber = number;
            if (_numberText != null)
            {
                _numberText.text = number.ToString();
                _numberText.gameObject.SetActive(true);
            }
        }

        public void ReceiveDroppedBug(BugController bug)
        {
            if (bug == null) return;

            // PREVENT DOUBLE DROP: Check if we already have a bug here
            if (bug.Number == _targetNumber && !IsCompleted)
            {
                AudioManager.Instance.PlayCorrect();

                if (GameManager.Instance != null)
                {
                    // Pass true for valid match
                    GameManager.Instance.CheckDrop(bug.Number, true);
                }

                bug.transform.SetParent(ContainerTransform);
                // Level 5 Snowball / After Drop logic - Trigger only after landing is complete
                if (_afterDropEffect != null || _afterDropSpriteChange != null)
                {
                    System.Action<BugController> successHandler = null;
                    successHandler = (b) => 
                    {
                        if (this != null)
                        {
                            if (_afterDropEffect != null) _afterDropEffect.SetActive(true);
                            if (_afterDropSpriteChange != null && _flowerSR != null) _flowerSR.sprite = _afterDropSpriteChange;
                        }
                        b.OnSuccessfulDrop -= successHandler;
                    };
                    bug.OnSuccessfulDrop += successHandler;
                }

                // If the bug hides its number on land, the container should KEEP showing its number.
                // If the bug KEEPS its number visible, the container should HIDE its number to avoid double digits.
                if (_numberText != null)
                {
                    _numberText.gameObject.SetActive(bug.HideNumberOnLand);
                }

                // Trigger Pulse feedback
                if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = StartCoroutine(PulseRoutine());

                bug.BecomeDecoration();
            }
            else
            {
                AudioManager.Instance.PlayIncorrect();

                // Wrong number OR the flower is already occupied
                if (GameManager.Instance != null && !IsCompleted)
                {
                    // Only penalize if it was a wrong number attempt, not just overlapping icons
                    GameManager.Instance.CheckDrop(bug.Number, false);
                }
                
                bug.RejectFlight();
            }
        }
        
        public void ClearContent()
        {
             System.Collections.Generic.List<BugController> bugs = new System.Collections.Generic.List<BugController>();
             foreach (Transform child in ContainerTransform)
             {
                 if (child.TryGetComponent(out BugController bug))
                 {
                     bugs.Add(bug);
                 }
                 else
                 {
                     Destroy(child.gameObject);
                 }
             }

             foreach (var b in bugs)
             {
                 b.Despawn();
             }

             if (_numberText != null) _numberText.gameObject.SetActive(true);
             if (_afterDropEffect != null) _afterDropEffect.SetActive(false);
             if (_flowerSR != null && _originalSprite != null) _flowerSR.sprite = _originalSprite;
        }

        private System.Collections.IEnumerator PulseRoutine()
        {
            float duration = 0.3f;
            float halfDuration = duration / 2f;
            float elapsed = 0f;
            
            Vector3 baseScale = transform.localScale;
            
            // Fallback in case the inspector value hasn't been set yet and is evaluating to 0
            float factor = _pulseFactor > 0.1f ? _pulseFactor : 1.25f;
            
            Vector3 maxScale = baseScale * factor; 

            // Scale Up
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(baseScale, maxScale, elapsed / halfDuration);
                yield return null;
            }

            // Scale Down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(maxScale, baseScale, elapsed / halfDuration);
                yield return null;
            }

            transform.localScale = baseScale;
        }


        public void ChangeSprite(Sprite flowerSprite)
        {
            if(flowerSprite != null)
            {
                _flowerSR.sprite = flowerSprite;
                _originalSprite = flowerSprite; // Update original so we revert to the correct assigned variant
            }
        }

        // --- Tutorial Interactions ---

        public void HighlightForTutorial(int highlightSortingOrder)
        {
            if (_tutorialSortingGroup == null)
            {
                _tutorialSortingGroup = gameObject.GetComponent<UnityEngine.Rendering.SortingGroup>();
                if (_tutorialSortingGroup == null)
                {
                    _tutorialSortingGroup = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                }
            }
            
            _originalSortingOrder = _tutorialSortingGroup.sortingOrder;
            _tutorialSortingGroup.sortingOrder = highlightSortingOrder;

            // Legitimize the UI Text Canvas sorting fix
            if (_numberText != null)
            {
                if (_numberTextCanvas == null) _numberTextCanvas = _numberText.GetComponentInParent<Canvas>();
                if (_numberTextCanvas != null)
                {
                    _originalCanvasSortingOrder = _numberTextCanvas.sortingOrder;
                    _numberTextCanvas.sortingOrder = highlightSortingOrder;
                }
            }
        }

        public void ClearHighlight()
        {
            if (_tutorialSortingGroup != null)
            {
                _tutorialSortingGroup.sortingOrder = _originalSortingOrder;
            }

            if (_numberTextCanvas != null)
            {
                _numberTextCanvas.sortingOrder = _originalCanvasSortingOrder;
            }
        }
    }
}
