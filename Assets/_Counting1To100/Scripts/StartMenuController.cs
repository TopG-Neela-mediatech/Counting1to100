using UnityEngine;

namespace Counting1To100
{
    /// <summary>
    /// Handles interactions in the Start Menu scene.
    /// </summary>
    public class StartMenuController : MonoBehaviour
    {
        /// <summary>
        /// Called by the Start Button to begin the game.
        /// </summary>
        public void OnStartButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
            else
            {
                Debug.LogError("[StartMenuController] GameManager Instance is null! Ensure it exists in the scene.");
            }
        }
    }
}
