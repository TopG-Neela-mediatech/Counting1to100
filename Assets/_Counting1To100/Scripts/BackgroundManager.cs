using UnityEngine;
using UnityEngine.UI;

namespace Counting1To100
{
    public class BackgroundManager : MonoBehaviour
    {
        [Header("Background Components")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private SpriteRenderer _backgroundSpriteRenderer;

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
