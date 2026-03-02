using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace Counting1To100.DragAndDropMode
{
    public class DirectionalBugSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private List<BugController> _bugPrefabs;
        
        [Header("Spawning Settings")]
        [SerializeField] private float _spawnInterval = 3f;
        [SerializeField] private float _bugFlightSpeed = 100f; // Screen units per second crossing
        
        [Header("Screen Edge Spawning")]
        // These bounds should correspond to your camera's world extents
        [SerializeField] private float _cameraWorldWidth = 15f; 
        [SerializeField] private float _cameraWorldHeight = 10f; 

        private bool _isSpawning = false;
        private Coroutine _spawnCoroutine;
        private ObjectPool<BugController> _bugPool;

        private void Awake()
        {
            if (_bugPrefabs != null && _bugPrefabs.Count > 0)
            {
                _bugPool = new ObjectPool<BugController>(
                    createFunc: () => Instantiate(_bugPrefabs[Random.Range(0, _bugPrefabs.Count)], transform),
                    actionOnGet: (bug) => { bug.gameObject.SetActive(true); bug.OnDespawn += ReleaseBug; },
                    actionOnRelease: (bug) => { bug.OnDespawn -= ReleaseBug; bug.gameObject.SetActive(false); },
                    actionOnDestroy: (bug) => Destroy(bug.gameObject),
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: 50
                );
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStarted += StartSpawning;
            GameManager.OnLevelComplete += StopSpawning;
            GameManager.OnGameEnded += StopSpawning;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= StartSpawning;
            GameManager.OnLevelComplete -= StopSpawning;
            GameManager.OnGameEnded -= StopSpawning;
        }

        private void StartSpawning()
        {
            StopSpawning();
            _isSpawning = true;
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private void StopSpawning()
        {
            _isSpawning = false;
            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        }

        private System.Collections.IEnumerator SpawnRoutine()
        {
            while (_isSpawning)
            {
                yield return new WaitForSeconds(_spawnInterval);
                SpawnBug();
            }
        }

        private void SpawnBug()
        {
            if (_bugPool == null) return;
            BugController bug = _bugPool.Get();

            int number = -1;
            if (ContainerManager.Instance != null)
            {
                var avail = ContainerManager.Instance.GetAvailableTargetNumbers();
                if (avail.Count > 0) number = avail[Random.Range(0, avail.Count)];
            }
            if (number == -1) number = Random.Range(1, 10);
            bug.SetNumber(number);

            CalculateCrossScreenPath(out Vector3 startPos, out Vector3 endPos);

            bug.transform.position = startPos;
            bug.InitializeFlight(endPos, _bugFlightSpeed);
        }

        private void CalculateCrossScreenPath(out Vector3 startPos, out Vector3 endPos)
        {
            float halfW = _cameraWorldWidth / 2f;
            float halfH = _cameraWorldHeight / 2f;

            startPos = Vector3.zero;
            endPos = Vector3.zero;

            // Pick a random edge to spawn on (0=Top, 1=Right, 2=Bottom, 3=Left)
            int edge = Random.Range(0, 4);
            
            // Generate a random position along that edge and its opposite edge
            switch (edge)
            {
                case 0: // Top to Bottom
                    startPos = new Vector3(Random.Range(-halfW, halfW), halfH, 0);
                    endPos = new Vector3(Random.Range(-halfW, halfW), -halfH, 0);
                    break;
                case 1: // Right to Left
                    startPos = new Vector3(halfW, Random.Range(-halfH, halfH), 0);
                    endPos = new Vector3(-halfW, Random.Range(-halfH, halfH), 0);
                    break;
                case 2: // Bottom to Top
                    startPos = new Vector3(Random.Range(-halfW, halfW), -halfH, 0);
                    endPos = new Vector3(Random.Range(-halfW, halfW), halfH, 0);
                    break;
                case 3: // Left to Right
                    startPos = new Vector3(-halfW, Random.Range(-halfH, halfH), 0);
                    endPos = new Vector3(halfW, Random.Range(-halfH, halfH), 0);
                    break;
            }
        }

        private void ReleaseBug(BugController bug) { _bugPool.Release(bug); }
    }
}
