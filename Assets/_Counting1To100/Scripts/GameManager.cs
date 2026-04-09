using System;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace TMKOC.Counting100
{
    public class GameManager : GenericSingleton<GameManager>
    {
        [Header("Level Configurations")]
        [SerializeField] private System.Collections.Generic.List<LevelData> _levels;

        [SerializeField, Range(0, 10)] private int _currentLevelIndex = 0;

        [SerializeField] private float _levelCompletePopupDelay = 2f;

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

            // Audio Cue: Intro
            AudioManager.Instance.PlayIntro();

            HelperGameCategoryDataSaver.Init(_levels.Count); // Add The Max level

            _currentLevelIndex = HelperGameCategoryDataSaver.GetStartLevel(); // Get Current Level
            Debug.Log($"Current level index: {_currentLevelIndex}");

            if (_currentLevelIndex > _levels.Count)
            {
                _currentLevelIndex = 0;
            }

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
            _matchesNeeded = 10;
            _currentMatches = 0;

            AudioManager.Instance.PlayLevelStart(_currentLevelIndex);

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


        public void CompleteLevel()
        {
            Debug.Log("[GameManager] Level Complete");
            OnLevelComplete?.Invoke();

            // Show Manual Win/Lose Panel after a short delay
            StartCoroutine(EnableWinPanelAfterDelay());
        }

        private IEnumerator EnableWinPanelAfterDelay()
        {
            yield return new WaitForSeconds(_levelCompletePopupDelay);

            HelperGameCategoryDataSaver.LevelCompleted(_currentLevelIndex + 1); // Save Current Level

            // Check if this was the last level
            if (_levels != null && _currentLevelIndex >= _levels.Count - 1)
            {
                HandleLastLevelCompletion();
                yield break;
            }

            AudioManager.Instance.PlayLevelComplete();


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

        private void HandleLastLevelCompletion()
        {
            Debug.Log("[GameManager] Game Ended");

            AudioManager.Instance.PlayGameEnd();
            OnGameEnded?.Invoke();

#if PLAYSCHOOL_MAIN
        if (EffectParticleControll.Instance != null && EffectParticleControll.Instance.spawnEndpanelGameObject == null){
                    EffectParticleControll.Instance.SpawnGameEndPanel();
                    GameOverEndPanel.Instance.AddTheListnerRetryGame(()=> GameManager.Instance.GameRestart());
        }
#else
            //Your testing End panel
            Debug.Log("Game completed, Test Panel comes here");
#endif
        }

        public void OnBackButtonPressed()
        {
            SceneManager.LoadSceneAsync(TMKOCPlaySchoolConstants.TMKOCPlayMainMenu);
        }

        public void LoadNextLevel()
        {
            _currentLevelIndex++;

            AudioManager.Instance.PlayLevelStart(_currentLevelIndex);

            if (_levels != null && _currentLevelIndex >= _levels.Count)
            {
                // No more levels
                //EndGame();
                Debug.Log("Reaching code block which should not be possible to reach");
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
