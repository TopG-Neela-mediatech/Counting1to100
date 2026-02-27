using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Counting1To100
{
    public class GameManager : GenericSingleton<GameManager>
    {
        // Events
        public static event Action OnGameStarted, OnGameEnded;
        //public static event Action<Scene, LoadSceneMode> OnSceneLoaded;
        public static event Action OnSceneLoaded;
        public static event Action OnLevelComplete, OnNextLevel;
        //public static event Action OnBonusRoundStart;

        // Game State
        public int CurrentLevelMin { get; private set; } = 1;
        public int CurrentLevelMax { get; private set; } = 10;
        
        private int _matchesNeeded = 10;
        private int _currentMatches = 0;
    
        protected override void Awake()
        {
            base.Awake();
        }
    
        private void OnEnable()
        {
            // Subscriptions to other events if needed
        }
    
        private void OnDisable()
        {
            // Unsubscriptions
        }

        private void Start()
        {
            Debug.Log("[GameManager] Scene Ready. Invoking OnSceneLoaded.");
            OnSceneLoaded?.Invoke();
        }
    
        public void StartGame()
        {
            Debug.Log("[GameManager] Game Started");
            // Reset Level 1
            CurrentLevelMin = 1;
            CurrentLevelMax = 10;
            _matchesNeeded = 10;
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
            else
            {
                // Optional: Game Over condition?
                // EndGame();
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
            
            // Prepare Next Level
            StartCoroutine(NextLevelRoutine());
        }

        private IEnumerator NextLevelRoutine()
        {
            yield return new WaitForSeconds(2f);
            StartNextLevel();
        }
    
        private void StartNextLevel()
        {
            // 1-10 -> 11-20
            CurrentLevelMin += 10;
            CurrentLevelMax += 10;
            _currentMatches = 0;
            
            Debug.Log($"Starting Next Level: {CurrentLevelMin}-{CurrentLevelMax}");
            OnNextLevel?.Invoke();
            OnGameStarted?.Invoke(); // Re-trigger level start for subscribed spawners
            
            if (JarManager.Instance != null)
            {
                JarManager.Instance.UpdateJarNumbers();
            }
        }
    
        //public void StartBonusRound()
        //{
        //    Debug.Log("[GameManager] Bonus Round Started");
        //    OnBonusRoundStart?.Invoke();
        //}
    }
}
