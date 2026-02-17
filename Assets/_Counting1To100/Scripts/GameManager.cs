using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : GenericSingleton<GameManager>
{
    // Events
    public static event Action OnGameStarted;
    //public static event Action<Scene, LoadSceneMode> OnSceneLoaded;
    public static event Action OnSceneLoaded;
    public static event Action OnLevelComplete;
    public static event Action OnBonusRoundStart;

    // Game State
    public int CurrentTargetNumber { get; private set; } = 1;
    public int MaxLevelNumber { get; private set; } = 10; // For Level 1

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
        CurrentTargetNumber = 1; // Reset for Level 1
        OnGameStarted?.Invoke();
    }

    public void CheckDrop(int number)
    {
        if (number == CurrentTargetNumber)
        {
            Debug.Log($"[GameManager] Correct! {number} collected.");
            CurrentTargetNumber++;
            
            // Trigger visual update event if needed? 
            // For now, checking win condition
            if (CurrentTargetNumber > MaxLevelNumber)
            {
                CompleteLevel();
            }
        }
        else
        {
            Debug.Log($"[GameManager] Wrong! Needed {CurrentTargetNumber}, got {number}.");
            // Optional: Play 'Wrong' sound
        }
    }

    public void CompleteLevel()
    {
        Debug.Log("[GameManager] Level Complete");
        OnLevelComplete?.Invoke();
    }

    public void StartBonusRound()
    {
        Debug.Log("[GameManager] Bonus Round Started");
        OnBonusRoundStart?.Invoke();
    }
}
