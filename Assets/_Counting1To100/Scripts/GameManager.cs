using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : GenericSingleton<GameManager>
{
    // Events
    public static event Action OnGameStarted;
    public static event Action<Scene, LoadSceneMode> OnSceneLoaded;
    public static event Action OnLevelComplete;
    public static event Action OnBonusRoundStart;

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
        OnSceneLoaded?.Invoke(scene, mode);
    }

    public void StartGame()
    {
        Debug.Log("[GameManager] Game Started");
        OnGameStarted?.Invoke();
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
