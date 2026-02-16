using UnityEngine;

namespace Counting1To100
{
    /// <summary>
    /// Controls the visual and movement behavior of the Bee.
    /// Handles wing rotation and horizontal flight.
    /// </summary>
    public class BeeController : MonoBehaviour
    {
        [Header("Wing Settings")]
        [Tooltip("Assign the 4 wing transforms here.")]
        [SerializeField] private Transform[] _wings;

        [Tooltip("Speed of the wing flutter animation.")]
        [SerializeField] private float _flutterSpeed = 20f;

        [Tooltip("Maximum angle of wing rotation.")]
        [SerializeField] private float _flutterAngle = 15f;

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 2f;

        private Quaternion[] _initialRotations;

        private void Start()
        {
            if (_wings != null && _wings.Length > 0)
            {
                _initialRotations = new Quaternion[_wings.Length];
                for (int i = 0; i < _wings.Length; i++)
                {
                    if (_wings[i] != null)
                    {
                        _initialRotations[i] = _wings[i].localRotation;
                    }
                }
            }
        }

        private void Update()
        {
            HandleWingFlutter();
        }

        /// <summary>
        /// Rotates the wings back and forth to simulate fluttering.
        /// </summary>
        private void HandleWingFlutter()
        {
            if (_wings == null) return;

            float angle = Mathf.Sin(Time.time * _flutterSpeed) * _flutterAngle;

            for (int i = 0; i < _wings.Length; i++)
            {
                if (_wings[i] != null)
                {
                    // Rotate around the Z axis (assuming 2D sprite setup)
                    // Adjust axis as needed based on prefab orientation
                    Quaternion flutterRotation = Quaternion.Euler(0, 0, angle);
                    _wings[i].localRotation = _initialRotations[i] * flutterRotation;
                }
            }
        }

        /// <summary>
        /// Moves the bee horizontally.
        /// </summary>
        public void Move()
        {
            transform.Translate(Vector3.right * _moveSpeed * Time.deltaTime);
        }
    }
}
