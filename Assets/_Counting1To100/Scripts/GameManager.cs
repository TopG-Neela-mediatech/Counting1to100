using System;
using UnityEngine;
using System.Collections;

namespace Counting1To100
{
    public class GameManager : GenericSingleton<GameManager>
    {
        [Header("Level Configurations")]
        [SerializeField] private System.Collections.Generic.List<LevelData> _levels;
        
        [SerializeField, Range(0,10)] private int _currentLevelIndex = 0;
        public int CurrentLevelIndex => _currentLevelIndex;
        public LevelData CurrentLevelData => 
            (_levels != null && _currentLevelIndex >= 0 && _currentLevelIndex < _levels.Count) ? _levels[_currentLevelIndex] : null;

        // Events
        public static event Action OnSceneLoaded;
        public static event Action OnGameStarted;
        public static event Action OnLevelComplete;
        public static event Action OnNextLevel;
        public static event Action OnGameEnded;
        
        public static event Action OnTutorialStarted;
        public static event Action OnTutorialEnded;

        // Tutorial State
        public bool IsTutorialActive { get; private set; } = false;

        // Game State
        public int CurrentLevelMin => CurrentLevelData != null ? CurrentLevelData.LevelMin : 1;
        public int CurrentLevelMax => CurrentLevelData != null ? CurrentLevelData.LevelMax : 10;
        
        private int _matchesNeeded = 10;
        private int _currentMatches = 0;
    
        protected override void Awake()
        {
            base.Awake();
        }
    
        private void Start()
        {
            Debug.Log("[GameManager] Scene Ready. Invoking OnSceneLoaded.");
            OnSceneLoaded?.Invoke();

            // Initialize Playschool Win/Lose Panel
            if (PlayschoolCommon.Instance != null)
            {
                PlayschoolCommon.Instance.SpawnplayschoolWinLosePanel();
            }
        }
    
        public void StartGame()
        {
            Debug.Log("[GameManager] Game Started");
            //_currentLevelIndex = 0;
            _matchesNeeded = 10; // This could also be logic based like (CurrentLevelMax - CurrentLevelMin + 1)
            _currentMatches = 0;
            
            OnGameStarted?.Invoke();
        }
    
        public void CheckDrop(int number, bool matchCorrect)
        {
            if (matchCorrect)
            {
                _currentMatches++;
                
                if (_currentMatches >= _matchesNeeded)
                {
                    CompleteLevel();
                }
            }
        }

        public void EndGame()
        {
            Debug.Log("[GameManager] Game Ended");
            OnGameEnded?.Invoke();
        }
    
        public void CompleteLevel()
        {
            Debug.Log("[GameManager] Level Complete");
            OnLevelComplete?.Invoke();
            
            // Show Manual Win/Lose Panel after a short delay
            StartCoroutine(EnableWinPanelAfterDelay());
        }

        private IEnumerator EnableWinPanelAfterDelay()
        {
            yield return new WaitForSeconds(0.25f);
            
            if (WinLosePanelScript.Instance != null)
            {
                WinLosePanelScript.Instance.ShowNextLevelPopUp(LoadNextLevel);
            }
            else
            {
                Debug.LogWarning("[GameManager] WinLosePanelScript Instance not found! Defaulting to auto-load.");
                LoadNextLevel();
            }
        }

        public void LoadNextLevel()
        {
            _currentLevelIndex++;
            
            if (_levels != null && _currentLevelIndex >= _levels.Count)
            {
                // No more levels
                EndGame();
                return;
            }

            _currentMatches = 0;
            
            Debug.Log($"Starting Next Level: {CurrentLevelMin}-{CurrentLevelMax}");
            OnNextLevel?.Invoke();
            OnGameStarted?.Invoke(); 
        }

        public void StartTutorial()
        {
            IsTutorialActive = true;
            Debug.Log("[GameManager] Tutorial Started");
            OnTutorialStarted?.Invoke();
        }

        public void EndTutorial()
        {
            IsTutorialActive = false;
            Debug.Log("[GameManager] Tutorial Ended");
            OnTutorialEnded?.Invoke();
        }
    }
}
