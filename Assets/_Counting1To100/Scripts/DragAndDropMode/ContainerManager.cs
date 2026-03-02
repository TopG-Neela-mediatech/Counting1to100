using UnityEngine;

namespace Counting1To100.DragAndDropMode
{
    public class ContainerManager : MonoBehaviour
    {
        public static ContainerManager Instance { get; private set; }

        private System.Collections.Generic.List<IDragContainer> _containers = new System.Collections.Generic.List<IDragContainer>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStarted += UpdateContainerNumbers;
            GameManager.OnNextLevel += UpdateContainerNumbers;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= UpdateContainerNumbers;
            GameManager.OnNextLevel -= UpdateContainerNumbers;
        }

        public void RegisterContainer(IDragContainer container)
        {
            if (!_containers.Contains(container))
            {
                _containers.Add(container);
            }
        }

        private void ShuffleContainers()
        {
            // Fisher-Yates shuffle to randomize the order of containers
            for (int i = _containers.Count - 1; i > 0; i--)
            {
                int rnd = Random.Range(0, i + 1);
                IDragContainer temp = _containers[i];
                _containers[i] = _containers[rnd];
                _containers[rnd] = temp;
            }
        }

        public void UpdateContainerNumbers()
        {
            if (GameManager.Instance == null) return;
            
            // Randomize which container gets which number
            ShuffleContainers();

            int startNum = GameManager.Instance.CurrentLevelMin;
            for (int i = 0; i < _containers.Count; i++)
            {
                if (_containers[i] is FlowerContainerController flower)
                {
                    flower.ClearContent();
                    flower.SetTargetNumber(startNum + i);
                }
            }
        }

        public void UnregisterContainer(IDragContainer container)
        {
            if (_containers.Contains(container))
            {
                _containers.Remove(container);
            }
        }

        /// <summary>
        /// Finds the closest container to the current drag position to drop into.
        /// We use a distance check within a defined "drop radius".
        /// </summary>
        public IDragContainer GetValidDropContainer(Vector2 position, float radius = 2f)
        {
            IDragContainer closest = null;
            float minDistance = radius; 

            foreach (var container in _containers)
            {
                // Ensure we don't drop into an already completed container if that's the rule
                // Assuming it's ok to check distance on X and Y since bug is dragged freely
                float dist = Vector2.Distance(position, container.ContainerTransform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = container;
                }
            }

            return closest;
        }

        public System.Collections.Generic.List<int> GetAvailableTargetNumbers()
        {
            var available = new System.Collections.Generic.List<int>();
            foreach (var container in _containers)
            {
                if (!container.IsCompleted)
                {
                    available.Add(container.TargetNumber);
                }
            }
            return available;
        }
    }
}
