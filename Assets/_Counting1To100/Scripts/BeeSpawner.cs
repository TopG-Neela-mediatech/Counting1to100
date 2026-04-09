using UnityEngine;
using UnityEngine.Pool;

namespace TMKOC.Counting100
{
    public class BeeSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private System.Collections.Generic.List<BeeController> _beePrefabs;
        
        [Header("Spawn Positions")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _endPoint; // Where the bee flies to

        [Header("Movement Settings")]
        [SerializeField] private float _spawnInterval = 2f; 
        [SerializeField] private float _moveSpeed = 100f; 
        [SerializeField] private float _minBobFrequency = 1.5f;
        [SerializeField] private float _maxBobFrequency = 2.5f;
        [SerializeField] private float _minBobAmplitude = 0.25f;
        [SerializeField] private float _maxBobAmplitude = 0.5f;
        
        [SerializeField] private int _minNumber = 1;
        [SerializeField] private int _maxNumber = 10;
        
        [Header("Spawning Logic")]
        [SerializeField] private bool _useSmartSpawning = true;

        private bool _isSpawning = false;
        private ObjectPool<BeeController> _beePool;

        private void Awake()
        {
            if (_beePrefabs != null && _beePrefabs.Count > 0 && _spawnPoint != null)
            {
                _beePool = new ObjectPool<BeeController>(
                    createFunc: CreateBee,
                    actionOnGet: OnGetBee,
                    actionOnRelease: OnReleaseBee,
                    actionOnDestroy: OnDestroyBee,
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: 50
                );
            }
        }

        #region Pooling Methods
        private BeeController CreateBee()
        {
            // Pick a random visual variant from the list to instantiate
            BeeController prefab = _beePrefabs[Random.Range(0, _beePrefabs.Count)];
            BeeController bee = Instantiate(prefab, _spawnPoint);
            return bee;
        }

        private void OnGetBee(BeeController bee)
        {
            bee.gameObject.SetActive(true);
            bee.OnDespawn += ReturnBeeToPool;
        }

        private void OnReleaseBee(BeeController bee)
        {
            bee.OnDespawn -= ReturnBeeToPool;
            bee.gameObject.SetActive(false);
        }

        private void OnDestroyBee(BeeController bee)
        {
            Destroy(bee.gameObject);
        }
        #endregion

        private Coroutine _spawnCoroutine;

        private void OnEnable()
        {
            GameManager.OnGameStarted += HandleGameStarted;
            GameManager.OnLevelComplete += HandleLevelComplete;
            GameManager.OnGameEnded += HandleGameEnded;
            GameManager.OnNextLevel += HandleNextLevel;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= HandleGameStarted;
            GameManager.OnLevelComplete -= HandleLevelComplete;
            GameManager.OnGameEnded -= HandleGameEnded;
            GameManager.OnNextLevel -= HandleNextLevel;
        }

        private void HandleGameStarted()
        {
            Debug.Log("[BeeSpawner] Received OnGameStarted: Spawning activated.");
            StopSpawning(); // Ensure clean start
            _isSpawning = true;
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private void HandleLevelComplete()
        {
            Debug.Log("[BeeSpawner] Received OnLevelComplete: Spawning halted.");
            StopSpawning();
        }

        private void HandleGameEnded()
        {
            Debug.Log("[BeeSpawner] Received OnGameEnded: Spawning terminated.");
            StopSpawning();
        }

        private void HandleNextLevel()
        {
            // Prepare for next level if needed
        }

        private void StopSpawning()
        {
            _isSpawning = false;
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }

        private System.Collections.IEnumerator SpawnRoutine()
        {
            while (_isSpawning)
            {
                yield return new WaitForSeconds(_spawnInterval);
                SpawnBee();
            }
        }

        private void SpawnBee()
        {
            if (_beePrefabs == null || _beePrefabs.Count == 0 || _spawnPoint == null || _endPoint == null) return;
            if (_beePool == null) return;

            // 1. Get from Pool (The pool will have various visual types inside it)
            BeeController beeObj = _beePool.Get();

            // 2. Spawn Position & Reset
            Vector3 spawnPos = _spawnPoint.position;
            beeObj.transform.position = spawnPos;
            beeObj.transform.rotation = Quaternion.identity;
            beeObj.transform.localScale = Vector3.one;

            // 3. Setup Number (Dynamic from GameManager/JarManager)
            int randomNumber = -1;

            if (_useSmartSpawning && JarManager.Instance != null)
            {
                var availableNumbers = JarManager.Instance.GetAvailableTargetNumbers();
                if (availableNumbers.Count > 0)
                {
                    randomNumber = availableNumbers[Random.Range(0, availableNumbers.Count)];
                }
            }

            if (randomNumber == -1)
            {
                int min = 1;
                int max = 10;
                if (GameManager.Instance != null)
                {
                    min = GameManager.Instance.CurrentLevelMin;
                    max = GameManager.Instance.CurrentLevelMax;
                }
                randomNumber = Random.Range(min, max + 1);
            }
            
            beeObj.SetNumber(randomNumber);
            
            // 4. Calculate End Position
            Vector3 endPos = _endPoint.position;

            // 5. Random Bobbing
            float randomFreq = Random.Range(_minBobFrequency, _maxBobFrequency);
            
            // 6. Start Movement
            beeObj.Initialize(endPos, _moveSpeed, randomFreq, _minBobAmplitude, _maxBobAmplitude);
        }

        private void ReturnBeeToPool(BeeController bee)
        {
            _beePool.Release(bee);
        }
    }
}
