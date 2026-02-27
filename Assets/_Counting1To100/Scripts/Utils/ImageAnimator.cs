using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Counting1To100
{
    /// <summary>
    /// Animates a sequence of sprites on a Unity UI Image component.
    /// Supports speed control, looping, and playback management.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ImageAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("List of sprites to animate.")]
        [SerializeField] private List<Sprite> _sprites = new List<Sprite>();

        [Tooltip("Frames per second.")]
        [SerializeField] private float _frameRate = 12f;

        [Tooltip("Should the animation loop automatically?")]
        [SerializeField] private bool _loop = true;

        [Tooltip("Should the animation start playing immediately?")]
        [SerializeField] private bool _playOnAwake = true;

        [Header("Events")]
        public UnityEvent OnAnimationComplete;

        [Header("Runtime Info")]
        [SerializeField] private bool _isPlaying;

        private Image _image;
        private int _currentFrame;
        private Coroutine _animationCoroutine;

        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Start()
        {
            if (_playOnAwake && _sprites.Count > 0)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        private IEnumerator AnimationLoop()
        {
            while (_isPlaying && _sprites != null && _sprites.Count > 0)
            {
                UpdateSprite();
                
                float frameDuration = 1f / Mathf.Max(0.001f, _frameRate);
                yield return new WaitForSeconds(frameDuration);

                _currentFrame++;

                if (_currentFrame >= _sprites.Count)
                {
                    if (_loop)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        _currentFrame = _sprites.Count - 1;
                        _isPlaying = false;
                        UpdateSprite();
                        OnAnimationComplete?.Invoke();
                        yield break;
                    }
                }
            }
        }

        private void UpdateSprite()
        {
            if (_image != null && _sprites != null && _currentFrame < _sprites.Count)
            {
                _image.sprite = _sprites[_currentFrame];
            }
        }

        /// <summary>
        /// Starts or restarts the animation from the beginning.
        /// </summary>
        public void Play()
        {
            if (_sprites == null || _sprites.Count == 0)
            {
                Debug.LogWarning("[ImageAnimator] No sprites assigned to play.");
                return;
            }

            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            
            _isPlaying = true;
            _currentFrame = 0;
            _animationCoroutine = StartCoroutine(AnimationLoop());
        }

        /// <summary>
        /// Stops the animation and resets to the first frame.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
            _currentFrame = 0;
            UpdateSprite();
        }

        /// <summary>
        /// Pauses the animation at the current frame.
        /// </summary>
        public void Pause()
        {
            _isPlaying = false;
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        /// <summary>
        /// Resumes the animation from the current frame.
        /// </summary>
        public void Resume()
        {
            if (_sprites != null && _sprites.Count > 0 && !_isPlaying)
            {
                _isPlaying = true;
                if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
                _animationCoroutine = StartCoroutine(AnimationLoop());
            }
        }

        /// <summary>
        /// Sets a new list of sprites for the animation.
        /// </summary>
        public void SetSprites(List<Sprite> newSprites, bool restart = true)
        {
            _sprites = newSprites;
            if (restart)
            {
                Play();
            }
        }

        /// <summary>
        /// Updates the frame rate (FPS).
        /// </summary>
        public void SetFrameRate(float newFrameRate)
        {
            _frameRate = newFrameRate;
        }

        /// <summary>
        /// Jumps to a specific frame.
        /// </summary>
        public void SetFrame(int frameIndex)
        {
            if (_sprites != null && frameIndex >= 0 && frameIndex < _sprites.Count)
            {
                _currentFrame = frameIndex;
                UpdateSprite();
            }
        }
    }
}
