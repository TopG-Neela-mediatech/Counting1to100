using UnityEngine;
using UnityEngine.Pool;

namespace Counting1To100
{
    public class BeeSpawner : MonoBehaviour
    {
        [SerializeField] private BeeController _beePrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _endPoint; // Where the bee flies to
        [SerializeField] private float _spawnInterval = 2f; 
        [SerializeField] private float _moveSpeed = 100f; // Single fixed speed
        [Header("Bobbing Settings")]
        [SerializeField] private float _minBobFrequency = 1.5f;
        [SerializeField] private float _maxBobFrequency = 2.5f;
        [SerializeField] private float _minBobAmplitude = 40f;
        [SerializeField] private float _maxBobAmplitude = 60f;
        
        [SerializeField] private int _minNumber = 1;
        [SerializeField] private int _maxNumber = 10;

        private bool _isSpawning = false;
        private ObjectPool<BeeController> _beePool;

        private void Awake()
        {
            if (_beePrefab != null && _spawnPoint != null)
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
            BeeController bee = Instantiate(_beePrefab, _spawnPoint);
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
            Debug.Log("Starting Spawning");
            _isSpawning = true;
            StartCoroutine(SpawnRoutine());
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
            if (_beePrefab == null || _spawnPoint == null || _endPoint == null) return;
            if (_beePool == null) return;

            // 1. Get from Pool
            BeeController beeObj = _beePool.Get();

            // 2. Spawn Position & Reset
            Vector3 spawnPos = _spawnPoint.position;
            beeObj.transform.position = spawnPos;
            beeObj.transform.rotation = Quaternion.identity;
            beeObj.transform.localScale = Vector3.one;

            // 3. Setup Number
            int randomNumber = Random.Range(_minNumber, _maxNumber + 1);
            beeObj.SetNumber(randomNumber);
            
            // 4. Calculate End Position
            Vector3 endPos = _endPoint.position;

            // 5. Random Bobbing
            float randomFreq = Random.Range(_minBobFrequency, _maxBobFrequency);
            float randomAmp = Random.Range(_minBobAmplitude, _maxBobAmplitude);

            // 6. Start Movement (Single Speed, Random Bobbing)
            beeObj.Initialize(endPos, _moveSpeed, randomFreq, randomAmp);
        }

        private void ReturnBeeToPool(BeeController bee)
        {
            _beePool.Release(bee);
        }
    }
}
