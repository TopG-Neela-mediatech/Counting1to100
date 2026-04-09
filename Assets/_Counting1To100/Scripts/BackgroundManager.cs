using UnityEngine;
using UnityEngine.UI;

namespace TMKOC.Counting100
{
    public class BackgroundManager : MonoBehaviour
    {
        [Header("Background Components")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private SpriteRenderer _backgroundSpriteRenderer;

        [Header("Final Level Settings")]
        [SerializeField] private Image _secondaryBG;
        [SerializeField] private SpriteRenderer _secondarySpriteRenderer;

        private void OnEnable()
        {
            GameManager.OnGameStarted += UpdateBackground;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= UpdateBackground;
        }

        private void UpdateBackground()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentLevelData == null) return;

            // Handle secondary BG activation
            Sprite secondaryBG = GameManager.Instance.CurrentLevelData.SecondaryBackgroundSprite;
            bool hasSecondary = secondaryBG != null;

            if (_secondaryBG != null)
            {
                _secondaryBG.gameObject.SetActive(hasSecondary);
                if (hasSecondary) _secondaryBG.sprite = secondaryBG;
            }

            if (_secondarySpriteRenderer != null)
            {
                _secondarySpriteRenderer.gameObject.SetActive(hasSecondary);
                if (hasSecondary) _secondarySpriteRenderer.sprite = secondaryBG;
            }

            Sprite bgSprite = GameManager.Instance.CurrentLevelData.BackgroundSprite;
            if (bgSprite != null)
            {
                if (_backgroundImage != null)
                {
                    _backgroundImage.sprite = bgSprite;
                }

                if (_backgroundSpriteRenderer != null)
                {
                    _backgroundSpriteRenderer.sprite = bgSprite;
                }
            }
        }
    }
}
