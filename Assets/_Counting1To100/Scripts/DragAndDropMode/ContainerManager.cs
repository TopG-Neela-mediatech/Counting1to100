using UnityEngine;

namespace Counting1To100.DragAndDropMode
{
    public class ContainerManager : MonoBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private Transform _spawnParent;

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
            GameManager.OnGameStarted += SetupContainersForLevel;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= SetupContainersForLevel;
        }

        private void SetupContainersForLevel()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentLevelData == null) return;
            
            var levelData = GameManager.Instance.CurrentLevelData;
            
            // Clear existing
            foreach (var container in _containers)
            {
                if (container is MonoBehaviour mb && mb != null)
                {
                    Destroy(mb.gameObject);
                }
            }
            _containers.Clear();

            if (levelData.ContainerPrefab == null || _spawnParent == null) return;

            int count = levelData.LevelMax - levelData.LevelMin + 1;
            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(levelData.ContainerPrefab, _spawnParent);
                obj.transform.localScale = Vector3.one;
                obj.transform.localPosition = Vector3.zero;
                
                IDragContainer comp = obj.GetComponent<IDragContainer>();
                if (comp != null && !_containers.Contains(comp))
                {
                    _containers.Add(comp);
                }
            }

            // Number assignment
            UpdateContainerNumbers();
        }

        public void RegisterContainer(IDragContainer container)
        {
            if (!_containers.Contains(container))
            {
                _containers.Add(container);
            }
        }

        private void SortContainers()
        {
            // Sort by X position for visual consistency (1-10 left to right)
            _containers.Sort((a, b) => a.ContainerTransform.position.x.CompareTo(b.ContainerTransform.position.x));
        }

        public void UpdateContainerNumbers()
        {
            if (GameManager.Instance == null || _containers == null || _containers.Count == 0) return;
            
            // Sort containers left to right to assign numbers sequentially
            SortContainers();

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
                // Only find non-completed containers for dragging
                if (container.IsCompleted) continue;

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

        public IDragContainer GetContainerByNumber(int number)
        {
            foreach (var container in _containers)
            {
                if (container.TargetNumber == number) return container;
            }
            return null;
        }
    }
}
