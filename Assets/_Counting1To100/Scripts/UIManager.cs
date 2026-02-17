using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Counting1To100
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _playButton;
        [SerializeField] private GameObject _startPanel;
        [SerializeField] private GameObject _gamePanel;

        private void Start()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClicked);
            }
        }

        private void OnEnable()
        {
            GameManager.OnSceneLoaded += OnSceneLoad;
            GameManager.OnGameStarted += HandleGameStarted;
        }

        private void OnSceneLoad()
        {
            if (_startPanel) _startPanel.SetActive(true);
        }

        private void OnDisable()
        {
            GameManager.OnSceneLoaded -= OnSceneLoad;
            GameManager.OnGameStarted -= HandleGameStarted;
        }

        public void OnPlayClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
            else
            {
                Debug.LogError("[UIManager] GameManager Instance is null!");
            }
        }

        private void HandleGameStarted()
        {
            if (_startPanel != null) _startPanel.SetActive(false);
            if (_gamePanel != null) _gamePanel.SetActive(true);
        }
    }
}
