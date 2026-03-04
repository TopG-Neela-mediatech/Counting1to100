using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Counting1To100.DragAndDropMode;

namespace Counting1To100
{
    public class LevelTutorialManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _darkOverlay;
        [SerializeField] private Transform _handPointer;
        [SerializeField] private Transform _tutorialElementsParent; // Parent for dummy bug and hand to sit above overlay

        [Header("Settings")]
        [SerializeField] private float _animationDuration = 2f;
        [SerializeField] private float _tutorialWaitStart = 0.5f;
        [SerializeField] private int _loops = 2; // How many times to show the drag animation
        
        private Coroutine _tutorialCoroutine;
        private BugController _dummyBug;

        private void OnEnable()
        {
            GameManager.OnGameStarted += HandleGameStarted;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= HandleGameStarted;
        }

        private void HandleGameStarted()
        {
            var levelData = GameManager.Instance.CurrentLevelData;
            if (levelData != null && levelData.ShowTutorial)
            {
                if (_tutorialCoroutine != null) StopCoroutine(_tutorialCoroutine);
                _tutorialCoroutine = StartCoroutine(TutorialRoutine());
            }
        }

        private IEnumerator TutorialRoutine()
        {
            // 1. Pause game logic
            GameManager.Instance.StartTutorial();

            // 2. Setup visual overlay
            if (_darkOverlay != null) _darkOverlay.SetActive(true);
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);

            yield return new WaitForSecondsRealtime(_tutorialWaitStart);

            // 3. Identify targets
            var availableNumbers = ContainerManager.Instance.GetAvailableTargetNumbers();
            if (availableNumbers.Count == 0)
            {
                EndTutorial();
                yield break;
            }

            int targetNumber = availableNumbers[0];
            IDragContainer targetContainer = ContainerManager.Instance.GetContainerByNumber(targetNumber);
            if (targetContainer == null)
            {
                EndTutorial();
                yield break;
            }

            // Highlight Container (could use sorting layer boost, but let's assume overlay ignores it or we boost it)
            // If target container is a canvas element, we need a Canvas override. If it's a sprite, we need a SortingGroup. 
            // For now, depending on the project structure, it might naturally sit behind the overlay if overlay is high sorting order. 
            // The absolute best way to fake it is drawing another canvas or sorting group, but moving it is risky. 
            // Let's assume the user handles highlighting via Canvas Sorting overrides manually, or we just put the dummy bug over it.

            // 4. Instantiate Dummy Bug for Tutorial
            var levelData = GameManager.Instance.CurrentLevelData;
            Vector3 bugStartPos = Camera.main != null ? Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.2f, 10f)) : new Vector3(0, -3f, 0);
            bugStartPos.z = 0;

            _dummyBug = Instantiate(levelData.BugPrefab, _tutorialElementsParent != null ? _tutorialElementsParent : transform);
            _dummyBug.transform.position = bugStartPos;
            _dummyBug.SetNumber(targetNumber);
            
            // "Disable" normal flight logic by flying it to its own exact position instantly
            _dummyBug.InitializeFlight(bugStartPos, 100f, Camera.main); 

            // 5. Play Drag Animation loops
            if (_handPointer != null)
            {
                _handPointer.gameObject.SetActive(true);
                
                for (int i = 0; i < _loops; i++)
                {
                    _handPointer.position = bugStartPos;
                    float elapsed = 0f;
                    
                    Vector3 targetPos = targetContainer.ContainerTransform.position;
                    
                    while (elapsed < _animationDuration)
                    {
                        elapsed += Time.unscaledDeltaTime; // Unscaled in case timeScale = 0 is used elsewhere
                        float t = elapsed / _animationDuration;
                        
                        // simple ease out
                        t = Mathf.Sin(t * Mathf.PI * 0.5f);
                        
                        _handPointer.position = Vector3.Lerp(bugStartPos, targetPos, t);
                        yield return null;
                    }
                    
                    yield return new WaitForSecondsRealtime(0.5f);
                }
                
                _handPointer.gameObject.SetActive(false);
            }

            // 6. Cleanup & Unpause
            EndTutorial();
        }

        private void EndTutorial()
        {
            if (_darkOverlay != null) _darkOverlay.SetActive(false);
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);
            
            if (_dummyBug != null)
            {
                Destroy(_dummyBug.gameObject);
            }

            if (GameManager.Instance != null && GameManager.Instance.IsTutorialActive)
            {
                GameManager.Instance.EndTutorial();
            }
        }
    }
}
