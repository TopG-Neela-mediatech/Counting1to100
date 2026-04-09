using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TMKOC.Counting100
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _playButton;
        [SerializeField] private GameObject _startPanel;
        [SerializeField] private GameObject _gamePanel;
        [SerializeField] private Button _backButton;

        [Header("Tween Settings")]
        [SerializeField] private float _exitSlideDuration = 0.5f;

        private void Start()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClicked);
            }
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackBtnClicked);
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

        public void OnBackBtnClicked()
        {
            if(GameManager.Instance != null)
            {
                GameManager.Instance.OnBackButtonPressed();
                return;
            }
        }

        private void HandleGameStarted()
        {
            if (_startPanel != null && _startPanel.transform.childCount > 0)
            {
                RectTransform child = _startPanel.transform.GetChild(0) as RectTransform;
                if (child != null)
                {
                    StartCoroutine(SlideUpRoutine(child));
                }
            }
            if (_gamePanel != null) _gamePanel.SetActive(true);
        }

        private IEnumerator SlideUpRoutine(RectTransform target)
        {
            Vector2 startPos = target.anchoredPosition;
            Vector2 endPos = new Vector2(startPos.x, startPos.y + target.rect.height + 250f);
            float elapsed = 0f;

            while (elapsed < _exitSlideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _exitSlideDuration);
                // Ease out (decelerate) â€” starts fast, slows down at end
                // t = 1f - Mathf.Pow(1f - t, 3f);

                // Ease in (accelerate) â€” starts slow, speeds up at end
                t = Mathf.Pow(t, 3f);
                target.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            target.anchoredPosition = endPos;
        }
    }
}
