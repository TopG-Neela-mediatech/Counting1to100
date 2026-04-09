using UnityEngine;
using System.Collections.Generic;

namespace TMKOC.Counting100
{
    public class ParticleEffectController : MonoBehaviour
    {
        [Header("Level Specific Effects")]
        [Tooltip("Assign GameObjects here. Index 0 = Level 1, Index 1 = Level 2, etc.")]
        [SerializeField] private List<GameObject> _levelEffects;

        private void OnEnable()
        {
            GameManager.OnGameStarted += HandleGameStarted;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= HandleGameStarted;
        }

        private void HandleGameStarted()
        {
            if (GameManager.Instance == null) return;

            int currentLevelIndex = GameManager.Instance.CurrentLevelIndex;
            UpdateActiveEffect(currentLevelIndex);
        }

        private void UpdateActiveEffect(int activeIndex)
        {
            if (_levelEffects == null) return;

            for (int i = 0; i < _levelEffects.Count; i++)
            {
                if (_levelEffects[i] == null) continue;

                // Enable if it matches current level index, otherwise disable
                bool shouldBeActive = (i == activeIndex);
                
                // Only change state if necessary to avoid redundant particle resets
                if (_levelEffects[i].activeSelf != shouldBeActive)
                {
                    _levelEffects[i].SetActive(shouldBeActive);
                }
            }
        }
    }
}
