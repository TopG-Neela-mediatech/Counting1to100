using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace Counting1To100.DragAndDropMode
{
    public class DirectionalBugSpawner : MonoBehaviour
    {
        [Header("Spawning Settings")]
        [SerializeField] private float _spawnInterval = 3f;
        [SerializeField] private float _initialStartDelay = 1f;
        [SerializeField] private float _bugFlightSpeed = 100f; // Screen units per second crossing
        [SerializeField] private Transform _spawnParent;
        [SerializeField] private Camera _mainCamera;

        [Header("Screen Edge Spawning")]
        [SerializeField] private float _offscreenOffset = 2f; 

        private float _halfWidth;
        private float _halfHeight;
        private bool _isSpawning = false;
        private Coroutine _spawnCoroutine;
        private System.Collections.Generic.List<ObjectPool<BugController>> _bugPools;
        private int _nextPrefabIndex = 0; // Round-robin index

        // Active bugs tracking for tutorial
        private System.Collections.Generic.List<BugController> _activeBugs = new System.Collections.Generic.List<BugController>();
        public System.Collections.Generic.IReadOnlyList<BugController> ActiveBugs => _activeBugs;

        public static DirectionalBugSpawner Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

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
            if (_bugPools != null) 
            {
                foreach (var pool in _bugPools)
                {
                    pool.Clear();
                }
                var existingBugs = FindObjectsByType<BugController>(FindObjectsSortMode.None);
                foreach (var b in existingBugs) 
                {
                    if (b != null) Destroy(b.gameObject);
                }
            }
            _activeBugs.Clear();

            var levelData = GameManager.Instance?.CurrentLevelData;
            if (levelData != null && levelData.BugPrefabs != null && levelData.BugPrefabs.Count > 0)
            {
                _bugPools = new System.Collections.Generic.List<ObjectPool<BugController>>();
                foreach (var prefab in levelData.BugPrefabs)
                {
                    if (prefab == null) continue;
                    var capturedPrefab = prefab; // capture for closure
                    _bugPools.Add(new ObjectPool<BugController>(
                        createFunc: () => Instantiate(capturedPrefab, transform),
                        actionOnGet: (bug) => { bug.gameObject.SetActive(true); _activeBugs.Add(bug); bug.OnDespawn += ReleaseBug; },
                        actionOnRelease: (bug) => { bug.OnDespawn -= ReleaseBug; _activeBugs.Remove(bug); bug.gameObject.SetActive(false); },
                        actionOnDestroy: (bug) => Destroy(bug.gameObject),
                        collectionCheck: true,
                        defaultCapacity: 10,
                        maxSize: 50
                    ));
                }
                _nextPrefabIndex = 0;
            }
            else
            {
                Debug.LogWarning("[DirectionalBugSpawner] No BugPrefabs found in current LevelData!");
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
            // Wait before first spawn (lets UI panel exit, containers set up, etc.)
            yield return new WaitForSeconds(_initialStartDelay);

            while (_isSpawning)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsTutorialActive)
                {
                    yield return null;
                    continue;
                }

                // Spawn first, then wait
                SpawnBug();

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
            }
        }

        private void SpawnBug()
        {
            if (_bugPools == null || _bugPools.Count == 0) return;

            // Round-robin: cycle through all prefab pools so every color gets used
            var pool = _bugPools[_nextPrefabIndex];
            _nextPrefabIndex = (_nextPrefabIndex + 1) % _bugPools.Count;

            BugController bug = pool.Get();

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
                pool.Release(bug);
                return;
            }

            bug.SetNumber(number);

            // Apply random color variant if the level defines any (e.g., dino eggs)
            var levelData = GameManager.Instance?.CurrentLevelData;
            if (levelData != null && levelData.BugColorVariants != null && levelData.BugColorVariants.Length > 0)
            {
                Sprite variant = levelData.BugColorVariants[Random.Range(0, levelData.BugColorVariants.Length)];
                bug.SetBodySprite(variant);
            }

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
            // If _bugPools is ever used improperly before init, safely exist
            if (_bugPools == null || _bugPools.Count == 0) return;
            // Find the matching pool and release back to it
            foreach (var pool in _bugPools)
            {
                try { pool.Release(bug); return; } catch { }
            }
        }
    }
}
