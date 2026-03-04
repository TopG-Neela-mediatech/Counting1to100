using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace Counting1To100.DragAndDropMode
{
    public class DirectionalBugSpawner : MonoBehaviour
    {
        [Header("Spawning Settings")]
        [SerializeField] private float _spawnInterval = 3f;
        [SerializeField] private float _bugFlightSpeed = 100f; // Screen units per second crossing
        [SerializeField] private Transform _spawnParent;
        [SerializeField] private Camera _mainCamera;

        [Header("Screen Edge Spawning")]
        [SerializeField] private float _offscreenOffset = 2f; 

        private float _halfWidth;
        private float _halfHeight;
        private bool _isSpawning = false;
        private Coroutine _spawnCoroutine;
        private ObjectPool<BugController> _bugPool;

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            
            // Calculate world-space bounds from camera
            if (_mainCamera != null && _mainCamera.orthographic)
            {
                _halfHeight = _mainCamera.orthographicSize + _offscreenOffset;
                _halfWidth = (_mainCamera.orthographicSize * _mainCamera.aspect) + _offscreenOffset;
            }
            else
            {
                // Fallback for non-orthographic or missing camera
                _halfHeight = 10f; 
                _halfWidth = 15f;
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
            
            // Clean up old bugs and pool before switching levels
            if (_bugPool != null) 
            {
                _bugPool.Clear();
                var existingBugs = FindObjectsByType<BugController>(FindObjectsSortMode.None);
                foreach (var b in existingBugs) 
                {
                    if (b != null) Destroy(b.gameObject);
                }
            }

            var levelData = GameManager.Instance?.CurrentLevelData;
            if (levelData != null && levelData.BugPrefab != null)
            {
                _bugPool = new ObjectPool<BugController>(
                    createFunc: () => Instantiate(levelData.BugPrefab, transform),
                    actionOnGet: (bug) => { bug.gameObject.SetActive(true); bug.OnDespawn += ReleaseBug; },
                    actionOnRelease: (bug) => { bug.OnDespawn -= ReleaseBug; bug.gameObject.SetActive(false); },
                    actionOnDestroy: (bug) => Destroy(bug.gameObject),
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: 50
                );
            }
            else
            {
                Debug.LogWarning("[DirectionalBugSpawner] No BugPrefab found in current LevelData!");
                return;
            }

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
                if (GameManager.Instance != null && GameManager.Instance.IsTutorialActive)
                {
                    yield return null;
                    continue;
                }

                // Custom wait to allow interruption by tutorial
                float timer = 0f;
                while (timer < _spawnInterval)
                {
                    if (GameManager.Instance != null && GameManager.Instance.IsTutorialActive)
                    {
                        yield return null;
                    }
                    else
                    {
                        timer += Time.deltaTime;
                        yield return null;
                    }
                }

                if (_isSpawning && (GameManager.Instance == null || !GameManager.Instance.IsTutorialActive))
                {
                    SpawnBug();
                }
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
                if (avail.Count > 0) 
                {
                    number = avail[Random.Range(0, avail.Count)];
                }
            }
            
            if (number == -1) 
            {
                // If no valid target numbers are left, recycle bug implicitly
                _bugPool.Release(bug);
                return;
            }

            bug.SetNumber(number);

            CalculateCrossScreenPath(out Vector3 startPos, out Vector3 endPos);
            
            // Parent first so coordinates are relative to the canvas if intended
            if (_spawnParent != null)
            {
                bug.transform.SetParent(_spawnParent);
            }

            // Set positions - using transform.position ensures world space consistency initially
            bug.transform.position = startPos;
            bug.InitializeFlight(endPos, _bugFlightSpeed, _mainCamera);
        }

        private void CalculateCrossScreenPath(out Vector3 startPos, out Vector3 endPos)
        {
            startPos = Vector3.zero;
            endPos = Vector3.zero;

            // Pick a random edge to spawn on (0=Top, 1=Right, 2=Bottom, 3=Left)
            int edge = Random.Range(0, 4);
            
            // Generate a random position along that edge and its opposite edge
            switch (edge)
            {
                case 0: // Top to Bottom
                    startPos = new Vector3(Random.Range(-_halfWidth, _halfWidth), _halfHeight, 0);
                    endPos = new Vector3(Random.Range(-_halfWidth, _halfWidth), -_halfHeight, 0);
                    break;
                case 1: // Right to Left
                    startPos = new Vector3(_halfWidth, Random.Range(-_halfHeight, _halfHeight), 0);
                    endPos = new Vector3(-_halfWidth, Random.Range(-_halfHeight, _halfHeight), 0);
                    break;
                case 2: // Bottom to Top
                    startPos = new Vector3(Random.Range(-_halfWidth, _halfWidth), -_halfHeight, 0);
                    endPos = new Vector3(Random.Range(-_halfWidth, _halfWidth), _halfHeight, 0);
                    break;
                case 3: // Left to Right
                    startPos = new Vector3(-_halfWidth, Random.Range(-_halfHeight, _halfHeight), 0);
                    endPos = new Vector3(_halfWidth, Random.Range(-_halfHeight, _halfHeight), 0);
                    break;
            }
        }

        private void ReleaseBug(BugController bug) 
        { 
            _bugPool.Release(bug); 
        }
    }
}
