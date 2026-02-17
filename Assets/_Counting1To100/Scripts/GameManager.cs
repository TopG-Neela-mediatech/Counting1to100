using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Counting1To100
{
    public class GameManager : GenericSingleton<GameManager>
    {
        // Events
        public static event Action OnGameStarted;
        //public static event Action<Scene, LoadSceneMode> OnSceneLoaded;
        public static event Action OnSceneLoaded;
        public static event Action OnLevelComplete;
        public static event Action OnBonusRoundStart;
    
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
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
    
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameManager] Scene Loaded: {scene.name}");
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
                Debug.Log($"[GameManager] Match! {_currentMatches + 1}/{_matchesNeeded}");
                _currentMatches++;
                
                if (_currentMatches >= _matchesNeeded)
                {
                    CompleteLevel();
                }
            }
            else
            {
                Debug.Log($"[GameManager] No Match.");
                // Optional: Penalty?
            }
        }
    
        public void CompleteLevel()
        {
            Debug.Log("[GameManager] Level Complete");
            OnLevelComplete?.Invoke();
            
            // Prepare Next Level
            Invoke(nameof(StartNextLevel), 2f); // Short delay before next batch handling
        }
    
        private void StartNextLevel()
        {
            // 1-10 -> 11-20
            CurrentLevelMin += 10;
            CurrentLevelMax += 10;
            _currentMatches = 0;
            
            Debug.Log($"Starting Next Level: {CurrentLevelMin}-{CurrentLevelMax}");
            OnGameStarted?.Invoke(); // Re-trigger level start for subscribed spawners
            
            if (JarManager.Instance != null)
            {
                JarManager.Instance.UpdateJarNumbers();
            }
        }
    
        public void StartBonusRound()
        {
            Debug.Log("[GameManager] Bonus Round Started");
            OnBonusRoundStart?.Invoke();
        }
    }
}
