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
        public bool IsCompleted => DropTarget.childCount > 0;

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
                if (GameManager.Instance != null)
                {
                    // Pass true for valid match
                    GameManager.Instance.CheckDrop(bee.Number, true);
                }

                // Visual Retention
                bee.transform.SetParent(DropTarget);
                bee.BecomeDecoration();
                
                // Randomize position inside jar slightly to avoid stacking
                Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0);
                bee.transform.localPosition = Vector3.zero + randomOffset;
            }
            else
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckDrop(bee.Number, false);
                }
                
                // Trigger the rejection animation (moves out/down with higher sorting order)
                bee.RejectFromJar(DropTarget.transform.position);
            }
        }
        
        public void ClearContent()
        {
            // Destroy all child objects (Previous bees)
            foreach (Transform child in DropTarget)
            {
                Destroy(child.gameObject);
            }
        }

        // Deprecated: Collider not strictly needed with Tween/Callback logic, 
        // but kept empty just in case we switch back to physics later.
        // private void OnTriggerEnter2D(Collider2D other) { ... }
    }
}
