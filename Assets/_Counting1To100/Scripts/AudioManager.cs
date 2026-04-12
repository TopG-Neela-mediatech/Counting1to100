using UnityEngine;
using System.Collections;

namespace TMKOC.Counting100
{
    public class AudioManager : GenericSingleton<AudioManager>
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Background Music")]
        [SerializeField] private AudioClip _backgroundMusic;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (_bgSource != null && _backgroundMusic != null)
            {
                _bgSource.clip = _backgroundMusic;
                _bgSource.loop = true;
                _bgSource.Play();
            }
        }

        public void PlayIntro()
        {
            if (RuntimeAudioLoader.Instance != null)
                RuntimeAudioLoader.Instance.PlayRuntimeAudio("OnSelectionScene");
        }

        public void PlayLevelStart(int levelIndex)
        {
            // levelIndex is 0-based, titles like "OnLevelStart1" are 1-based
            string key = $"OnLevelStart{levelIndex}";
            Debug.Log($"Level Index Audio Key: {key}");
            if (RuntimeAudioLoader.Instance != null)
                RuntimeAudioLoader.Instance.PlayRuntimeAudio(key);
        }

        public void PlayLevelComplete()
        {
            if (RuntimeAudioLoader.Instance != null)
                RuntimeAudioLoader.Instance.PlayNextLevelAudioClip();
        }

        public void PlayGameEnd()
        {
            if (RuntimeAudioLoader.Instance != null)
                RuntimeAudioLoader.Instance.PlayRuntimeAudio("OnGameEnd");
        }

        public void PlayNumber(int number)
        {
            if (RuntimeAudioLoader.Instance == null) return;

            if (number <= 20)
            {
                // Uses common number clips (1.0, 2.0, etc.)
                RuntimeAudioLoader.Instance.PlayNumberClip(number);
            }
            else
            {
                // Uses runtime loaded clips from the specific category
                string num = number + ".0";
                RuntimeAudioLoader.Instance.PlayRuntimeAudio(num);
            }
        }

        public void PlayCorrect()
        {
            if (RuntimeAudioLoader.Instance != null)
                RuntimeAudioLoader.Instance.PlayCorrectAudioClip();
        }

        public void PlayIncorrect()
        {
            if (RuntimeAudioLoader.Instance != null)
                RuntimeAudioLoader.Instance.PlayIncorrectAudioClip();
        }
    }
}
