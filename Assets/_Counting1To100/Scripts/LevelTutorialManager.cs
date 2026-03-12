using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Counting1To100.DragAndDropMode;

namespace Counting1To100
{
    public class LevelTutorialManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _darkOverlay;
        [SerializeField] private Transform _handPointer;

        [Header("Settings")]
        [SerializeField] private float _animationDuration = 2f;
        [SerializeField] private float _idleWaitTime = 5f;
        [SerializeField] private float _initialTutorialDelay = 1.5f;
        [SerializeField] private int _highlightSortingOrder = 1000;
        
        private Coroutine _handAnimCoroutine;
        private BugController _highlightedBug;
        private FlowerContainerController _highlightedContainer;
        
        private bool _isTutorialDone = false;
        private bool _isShowingHint = false;
        private bool _isIdleHint = false;
        private float _timeSinceLastInteraction = 0f;

        private void Awake()
        {
            if (_darkOverlay != null) _darkOverlay.SetActive(false);
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameManager.OnGameStarted += HandleGameStarted;
            GameManager.OnNextLevel += HandleNextLevel;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= HandleGameStarted;
            GameManager.OnNextLevel -= HandleNextLevel;
        }

        private void HandleGameStarted()
        {
            _isTutorialDone = false;
            _timeSinceLastInteraction = 0f;

            var levelData = GameManager.Instance.CurrentLevelData;
            if (levelData != null && levelData.ShowTutorial)
            {
                StartCoroutine(InitialTutorialRoutine());
            }
            else
            {
                // If this level doesn't need the initial tutorial, mark it done to allow idle hints to begin
                _isTutorialDone = true;
            }
        }

        private void HandleNextLevel()
        {
            // Reset for next level
            EndCurrentHint();
            _isTutorialDone = false;
            _timeSinceLastInteraction = 0f;
        }

        private void Update()
        {
            // Reset idle timer on any screen touch/click
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                _timeSinceLastInteraction = 0f;
                
                // Only instantly kill the hint if it was an IDLE hint. 
                // The main tutorial hint requires successfully completing the drag to exit.
                if (_isShowingHint && _isIdleHint)
                {
                    EndCurrentHint();
                }
            }

            // Only run idle tracking if the initial tutorial is completely finished
            if (!_isTutorialDone || GameManager.Instance == null || GameManager.Instance.IsTutorialActive) return;

            // Increment idle timer and check threshold
            if (!_isShowingHint)
            {
                _timeSinceLastInteraction += Time.unscaledDeltaTime;
                if (_timeSinceLastInteraction >= _idleWaitTime)
                {
                    TriggerIdleHint();
                }
            }
        }

        private IEnumerator InitialTutorialRoutine()
        {
            // Wait for bugs to actually spawn
            BugController validBug = null;
            while (validBug == null)
            {
                yield return new WaitForSeconds(0.5f);
                validBug = FindValidBug();
            }

            // Wait a little bit extra so the bug flies towards the center of the screen
            yield return new WaitForSeconds(_initialTutorialDelay);

            // Fetch the bug again just in case it was caught/despawned during the delay
            if (validBug == null || !validBug.gameObject.activeInHierarchy || validBug.Number <= 0)
            {
                validBug = FindValidBug();
            }

            if (validBug != null)
            {
                // A valid bug is on screen and well within view! Start the main tutorial.
                _isIdleHint = false;
                ShowHint(validBug, true);
            }
            else
            {
                // Edge case: if bugs vanished, retry the routine
                StartCoroutine(InitialTutorialRoutine());
            }
        }

        private void TriggerIdleHint()
        {
            BugController validBug = FindValidBug();
            if (validBug != null)
            {
                _isIdleHint = true;
                ShowHint(validBug, false);
            }
        }

        private BugController FindValidBug()
        {
            if (DirectionalBugSpawner.Instance == null || DirectionalBugSpawner.Instance.ActiveBugs.Count == 0) return null;

            var activeBugs = DirectionalBugSpawner.Instance.ActiveBugs;
            int count = activeBugs.Count;
            int startIndex = Random.Range(0, count);

            for (int i = 0; i < count; i++)
            {
                int index = (startIndex + i) % count;
                BugController bug = activeBugs[index];

                if (bug != null && bug.gameObject.activeInHierarchy && bug.Number > 0)
                {
                    IDragContainer container = ContainerManager.Instance.GetContainerByNumber(bug.Number);
                    if (container != null && !container.IsCompleted)
                    {
                        return bug; // Found a valid pair
                    }
                }
            }
            return null;
        }

        private void ShowHint(BugController bug, bool isInitialTutorial)
        {
            if (_isShowingHint) return;
            _isShowingHint = true;

            _highlightedBug = bug;
            _highlightedContainer = ContainerManager.Instance.GetContainerByNumber(bug.Number) as FlowerContainerController;

            if (_highlightedContainer == null)
            {
                EndCurrentHint();
                return;
            }

            // Always pause and show overlay now, as per user request for unified tutorial experience
            GameManager.Instance.StartTutorial();
            if (_darkOverlay != null) _darkOverlay.SetActive(true);

            // Boost Sorting
            _highlightedBug.HighlightForTutorial(_highlightSortingOrder);
            _highlightedContainer.HighlightForTutorial(_highlightSortingOrder - 1); // slightly behind bug

            // Subscribe to the success event so we know to end the tutorial!
            _highlightedBug.OnSuccessfulDrop += HandleSuccessfulDrop;
            _highlightedBug.OnDragStarted += HandleDragStarted;
            _highlightedBug.OnDragEnded += HandleDragEnded;

            if (_handPointer != null)
            {
                var handCanvas = _handPointer.GetComponent<Canvas>();
                if (handCanvas != null)
                {
                    handCanvas.sortingOrder = _highlightSortingOrder + 1;
                }
                
                _handPointer.gameObject.SetActive(true);
            }

            if (_handAnimCoroutine != null) StopCoroutine(_handAnimCoroutine);
            _handAnimCoroutine = StartCoroutine(AnimateHandRoutine());
        }

        private void HandleSuccessfulDrop(BugController bug)
        {
            if (bug == _highlightedBug)
            {
                // The user successfully dropped the tutorial bug!
                if (!_isIdleHint)
                {
                    _isTutorialDone = true;
                }
                EndCurrentHint();
            }
        }

        private void HandleDragStarted(BugController bug)
        {
            if (_handAnimCoroutine != null) StopCoroutine(_handAnimCoroutine);
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);
        }

        private void HandleDragEnded(BugController bug)
        {
            // Only restart if the hint is still active
            if (_isShowingHint && _highlightedBug != null && _highlightedContainer != null)
            {
                if (_handPointer != null) _handPointer.gameObject.SetActive(true);
                if (_handAnimCoroutine != null) StopCoroutine(_handAnimCoroutine);
                _handAnimCoroutine = StartCoroutine(AnimateHandRoutine());
            }
        }

        private IEnumerator AnimateHandRoutine()
        {
            while (_isShowingHint && _highlightedBug != null && _highlightedContainer != null)
            {
                float elapsed = 0f;

                while (elapsed < _animationDuration && _isShowingHint)
                {
                    if (_highlightedBug == null || _highlightedBug.gameObject == null || 
                        _highlightedContainer == null || _highlightedContainer.gameObject == null ||
                        _handPointer == null || _handPointer.gameObject == null) 
                    {
                        break;
                    }

                    Vector3 bugWorldPos = _highlightedBug.transform.position;
                    Vector3 containerWorldPos = _highlightedContainer.ContainerTransform.position;

                    // Ensure Z is visible to camera
                    bugWorldPos.z = _handPointer.position.z;
                    containerWorldPos.z = _handPointer.position.z;

                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / _animationDuration;
                    t = Mathf.Sin(t * Mathf.PI * 0.5f); // Sine ease

                    // Simple, robust world space lerp.
                    // This requires _handPointer to be either a SpriteRenderer or standard Transform,
                    // or a RectTransform within a World Space Canvas.
                    _handPointer.position = Vector3.Lerp(bugWorldPos, containerWorldPos, t);
                    
                    yield return null;
                }

                if (_isShowingHint)
                {
                    // Brief pause before repeating the hand animation
                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }

            if (_isShowingHint) EndCurrentHint();
        }

        private void EndCurrentHint()
        {
            if (!_isShowingHint) return;

            _isShowingHint = false;
            _timeSinceLastInteraction = 0f;

            if (_handAnimCoroutine != null) StopCoroutine(_handAnimCoroutine);

            if (_darkOverlay != null) _darkOverlay.SetActive(false);
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);

            if (_highlightedBug != null)
            {
                _highlightedBug.ClearHighlight();
                _highlightedBug.OnSuccessfulDrop -= HandleSuccessfulDrop;
                _highlightedBug.OnDragStarted -= HandleDragStarted;
                _highlightedBug.OnDragEnded -= HandleDragEnded;
                _highlightedBug = null;
            }

            if (_highlightedContainer != null)
            {
                _highlightedContainer.ClearHighlight();
                _highlightedContainer = null;
            }

            if (GameManager.Instance != null && GameManager.Instance.IsTutorialActive)
            {
                GameManager.Instance.EndTutorial();
            }
        }
    }
}
