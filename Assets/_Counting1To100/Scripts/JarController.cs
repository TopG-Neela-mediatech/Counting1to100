using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Counting1To100
{
    public class JarController : MonoBehaviour, IDropTarget
    {
        [SerializeField] private int _targetNumber;
        [SerializeField] private TextMeshProUGUI _numberText;
        [SerializeField] private Transform _dropTarget; // Where the bee should aim for

        public int Number => _targetNumber;
        public Transform DropTarget => _dropTarget != null ? _dropTarget : transform;

        // Static list removed in favor of JarManager
        
        private void Start()
        {
            // Register self
            if (JarManager.Instance != null)
            {
                JarManager.Instance.RegisterJar(this);
            }
        }

        private void OnDestroy()
        {
            if (JarManager.Instance != null)
            {
                JarManager.Instance.UnregisterJar(this);
            }
        }

        public void SetTargetNumber(int number)
        {
            _targetNumber = number;
            if (_numberText != null)
            {
                _numberText.text = number.ToString();
            }
        }

        public void ReceiveDrop(BeeController bee)
        {
            if (bee == null) return;
            
            // Check Matching Logic (Flexible)
            if (bee.Number == _targetNumber)
            {
                Debug.Log($"Correct! Firefly {bee.Number} -> Jar {_targetNumber}");
                if (GameManager.Instance != null)
                {
                    // Pass true for valid match
                    GameManager.Instance.CheckDrop(bee.Number, true);
                }
            }
            else
            {
                Debug.Log($"Wrong! Firefly {bee.Number} != Jar {_targetNumber}");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckDrop(bee.Number, false);
                }
            }

            bee.Despawn(); // Return to pool
        }

        // Deprecated: Collider not strictly needed with Tween/Callback logic, 
        // but kept empty just in case we switch back to physics later.
        // private void OnTriggerEnter2D(Collider2D other) { ... }
    }
}
