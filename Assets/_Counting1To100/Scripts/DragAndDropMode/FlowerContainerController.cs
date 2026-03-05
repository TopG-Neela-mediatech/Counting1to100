using UnityEngine;
using TMPro;

namespace Counting1To100.DragAndDropMode
{
    public class FlowerContainerController : MonoBehaviour, IDragContainer
    {
        [Header("Settings")]
        [SerializeField] private int _targetNumber;
        [SerializeField] private TextMeshProUGUI _numberText;
        [SerializeField] private Transform _flowerHeadTransform; // Replaces 'DropTarget'
        [SerializeField] private SpriteRenderer _flowerSR;
        [SerializeField, Range(1f,2f)] private float _pulseFactor;
        public Transform ContainerTransform => _flowerHeadTransform != null ? _flowerHeadTransform : transform;
        public int TargetNumber => _targetNumber;
        
        // This assumes each flower takes one bug. If multiple, change the logic
        public bool IsCompleted => ContainerTransform.childCount > 0;

        private Coroutine _pulseCoroutine;

        private void Start()
        {
            if (ContainerManager.Instance != null)
            {
                ContainerManager.Instance.RegisterContainer(this);
            }
        }

        private void OnDestroy()
        {
            if (ContainerManager.Instance != null)
            {
                ContainerManager.Instance.UnregisterContainer(this);
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

        public void ReceiveDroppedBug(BugController bug)
        {
            if (bug == null) return;

            // PREVENT DOUBLE DROP: Check if we already have a bug here
            if (bug.Number == _targetNumber && !IsCompleted)
            {
                if (GameManager.Instance != null)
                {
                    // Pass true for valid match
                    GameManager.Instance.CheckDrop(bug.Number, true);
                }

                bug.transform.SetParent(ContainerTransform);
                bug.BecomeDecoration();

                // Trigger Pulse feedback
                if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = StartCoroutine(PulseRoutine());
            }
            else
            {
                // Wrong number OR the flower is already occupied
                if (GameManager.Instance != null && !IsCompleted)
                {
                    // Only penalize if it was a wrong number attempt, not just overlapping icons
                    GameManager.Instance.CheckDrop(bug.Number, false);
                }
                
                bug.RejectFlight();
            }
        }
        
        public void ClearContent()
        {
             System.Collections.Generic.List<BugController> bugs = new System.Collections.Generic.List<BugController>();
             foreach (Transform child in ContainerTransform)
             {
                 if (child.TryGetComponent(out BugController bug))
                 {
                     bugs.Add(bug);
                 }
                 else
                 {
                     Destroy(child.gameObject);
                 }
             }

             foreach (var b in bugs)
             {
                 b.Despawn();
             }
        }

        private System.Collections.IEnumerator PulseRoutine()
        {
            float duration = 0.3f;
            float halfDuration = duration / 2f;
            float elapsed = 0f;
            
            Vector3 baseScale = transform.localScale;
            
            // Fallback in case the inspector value hasn't been set yet and is evaluating to 0
            float factor = _pulseFactor > 0.1f ? _pulseFactor : 1.25f;
            
            Vector3 maxScale = baseScale * factor; 

            // Scale Up
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(baseScale, maxScale, elapsed / halfDuration);
                yield return null;
            }

            // Scale Down
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(maxScale, baseScale, elapsed / halfDuration);
                yield return null;
            }

            transform.localScale = baseScale;
        }


        public void ChangeSprite(Sprite flowerSprite)
        {
            if(flowerSprite != null)
            {
                _flowerSR.sprite = flowerSprite;
            }
        }
    }
}
