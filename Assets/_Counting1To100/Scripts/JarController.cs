using UnityEngine;

namespace Counting1To100
{
    public class JarController : MonoBehaviour
    {
        [SerializeField] private int _targetNumber;

        public void SetTargetNumber(int number)
        {
            _targetNumber = number;
            // TODO: Update Visual Label on Jar
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            BeeController bee = other.GetComponent<BeeController>();
            if (bee != null)
            {
                // Delegate logic to GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckDrop(bee.Number);
                }

                bee.Despawn(); // Return to pool instead of Destroy
            }
        }
    }
}
