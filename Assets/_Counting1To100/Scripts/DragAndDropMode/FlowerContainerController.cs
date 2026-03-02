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

        public Transform ContainerTransform => _flowerHeadTransform != null ? _flowerHeadTransform : transform;
        public int TargetNumber => _targetNumber;
        
        // This assumes each flower takes one bug. If multiple, change the logic
        public bool IsCompleted => ContainerTransform.childCount > 0;

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

            if (bug.Number == _targetNumber)
            {
                if (GameManager.Instance != null)
                {
                    // Pass true for valid match
                    GameManager.Instance.CheckDrop(bug.Number, true);
                }

                bug.transform.SetParent(ContainerTransform);
                bug.BecomeDecoration();
            }
            else
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckDrop(bug.Number, false);
                }
                
                // We will add rejection to BugController soon
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
    }
}
