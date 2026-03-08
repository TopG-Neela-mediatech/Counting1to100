using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

namespace Counting1To100
{
    /// <summary>
    /// Manages per-level ambient decoration prefabs (fish, birds, etc.).
    /// Spawns them from ObjectPools and moves them across the screen
    /// using camera bounds, similar to DirectionalBugSpawner.
    /// </summary>
    public class DecorationManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform _decorationParent;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private float _offscreenOffset = 2f; // How far past screen edge to spawn/despawn

        private float _halfWidth;
        private float _halfHeight;
        private bool _isSpawning = false;
        private Coroutine _spawnCoroutine;
        private List<ObjectPool<GameObject>> _decorationPools;
        private List<GameObject> _activeDecorations = new List<GameObject>();
        private int _nextPrefabIndex = 0;

        // Per-level settings (read from LevelData each level)
        private float _spawnInterval;
        private int _maxActiveDecorations;
        private float _moveSpeed;
        private bool _horizontalOnly;

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            if (_mainCamera != null && _mainCamera.orthographic)
            {
                _halfHeight = _mainCamera.orthographicSize;
                _halfWidth = _mainCamera.orthographicSize * _mainCamera.aspect;
            }
            else
            {
                _halfHeight = 10f;
                _halfWidth = 15f;
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStarted += StartSpawning;
            GameManager.OnLevelComplete += StopAndClear;
            GameManager.OnGameEnded += StopAndClear;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= StartSpawning;
            GameManager.OnLevelComplete -= StopAndClear;
            GameManager.OnGameEnded -= StopAndClear;
        }

        private void StartSpawning()
        {
            StopAndClear();

            if (GameManager.Instance == null || GameManager.Instance.CurrentLevelData == null) return;

            var levelData = GameManager.Instance.CurrentLevelData;
            var prefabs = levelData.DecorationPrefabs;
            if (prefabs == null || prefabs.Length == 0) return;

            // Read per-level settings from SO
            _spawnInterval = levelData.DecorationSpawnInterval;
            _maxActiveDecorations = levelData.DecorationMaxActive;
            _moveSpeed = levelData.DecorationMoveSpeed;
            _horizontalOnly = levelData.DecorationHorizontalOnly;

            // Build pools for this level's decoration prefabs
            _decorationPools = new List<ObjectPool<GameObject>>();
            Transform parent = _decorationParent != null ? _decorationParent : transform;

            foreach (var prefab in prefabs)
            {
                if (prefab == null) continue;
                var capturedPrefab = prefab;
                _decorationPools.Add(new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(capturedPrefab, parent),
                    actionOnGet: (obj) => obj.SetActive(true),
                    actionOnRelease: (obj) => obj.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj),
                    collectionCheck: true,
                    defaultCapacity: 5,
                    maxSize: 20
                ));
            }

            _nextPrefabIndex = 0;
            _isSpawning = true;
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private void StopAndClear()
        {
            _isSpawning = false;
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            // Stop all movement coroutines and release or destroy active decorations
            StopAllCoroutines();

            foreach (var obj in _activeDecorations)
            {
                if (obj != null) Destroy(obj);
            }
            _activeDecorations.Clear();

            if (_decorationPools != null)
            {
                foreach (var pool in _decorationPools)
                {
                    pool.Clear();
                }
                _decorationPools = null;
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (_isSpawning)
            {
                if (_activeDecorations.Count < _maxActiveDecorations)
                {
                    SpawnDecoration();
                }

                yield return new WaitForSeconds(_spawnInterval);
            }
        }

        private void SpawnDecoration()
        {
            if (_decorationPools == null || _decorationPools.Count == 0) return;

            var pool = _decorationPools[_nextPrefabIndex];
            _nextPrefabIndex = (_nextPrefabIndex + 1) % _decorationPools.Count;

            GameObject obj = pool.Get();
            _activeDecorations.Add(obj);

            GetCrossScreenPath(out Vector3 startPos, out Vector3 endPos);

            obj.transform.position = startPos;

            // Flip to face movement direction (prefab faces right by default)
            bool movingLeft = endPos.x < startPos.x;
            Vector3 scale = obj.transform.localScale;
            obj.transform.localScale = new Vector3(movingLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x), scale.y, scale.z);

            StartCoroutine(MoveDecoration(obj, pool, startPos, endPos));
        }

        private IEnumerator MoveDecoration(GameObject obj, ObjectPool<GameObject> pool, Vector3 start, Vector3 end)
        {
            float distance = Vector3.Distance(start, end);
            float duration = distance / _moveSpeed;
            float elapsed = 0f;
            float noiseSeed = Random.Range(0f, 100f);

            while (elapsed < duration)
            {
                if (obj == null) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                Vector3 basePos = Vector3.Lerp(start, end, t);

                // Slight vertical bobbing for natural movement
                float bob = Mathf.Sin(Time.time * 2f + noiseSeed) * 0.15f;
                obj.transform.position = basePos + new Vector3(0, bob, 0);

                yield return null;
            }

            // Reached end — release back to pool
            if (obj != null)
            {
                _activeDecorations.Remove(obj);
                pool.Release(obj);
            }
        }

        private void GetCrossScreenPath(out Vector3 startPos, out Vector3 endPos)
        {
            if (_horizontalOnly)
            {
                // Fish-style: left to right or right to left
                bool leftToRight = Random.value > 0.5f;
                float y = Random.Range(-_halfHeight * 0.6f, _halfHeight * 0.6f); // Visible screen height

                float spawnX = _halfWidth + _offscreenOffset; // Spawn/despawn past screen edge
                if (leftToRight)
                {
                    startPos = new Vector3(-spawnX, y, 0);
                    endPos = new Vector3(spawnX, y, 0);
                }
                else
                {
                    startPos = new Vector3(spawnX, y, 0);
                    endPos = new Vector3(-spawnX, y, 0);
                }
            }
            else
            {
                // Random edge-to-edge (for butterflies etc. — future use)
                int edge = Random.Range(0, 4);
                startPos = Vector3.zero;
                endPos = Vector3.zero;

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
        }
    }
}
