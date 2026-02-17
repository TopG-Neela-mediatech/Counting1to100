using UnityEngine;
using System.Collections.Generic;

namespace Counting1To100
{
    public class JarManager : GenericSingleton<JarManager>
    {
        private List<JarController> _jars = new List<JarController>();

        protected override void Awake()
        {
            base.Awake();
        }

        private void OnEnable()
        {
            GameManager.OnGameStarted += UpdateJarNumbers;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= UpdateJarNumbers;
        }

        public void RegisterJar(JarController jar)
        {
            if (!_jars.Contains(jar))
            {
                _jars.Add(jar);
                // Sort jars by X position so they are sequential from left to right?
                // Or user manually sets them? 
                // For "Counting", usually left-most is #1.
                // We can auto-sort here or trust the user setup. 
                // Let's sort by X to be safe and ensure functionality matches visual order.
                _jars.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            }
        }

        public void UnregisterJar(JarController jar)
        {
            if (_jars.Contains(jar))
            {
                _jars.Remove(jar);
            }
        }

        public IDropTarget GetClosestDropTarget(Vector3 position)
        {
            IDropTarget closest = null;
            float minDistance = float.MaxValue;

            foreach (var jar in _jars)
            {
                float dist = Mathf.Abs(position.x - jar.DropTarget.position.x);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = jar;
                }
            }

            return closest;
        }

        public void UpdateJarNumbers()
        {
            // Sync with GameManager's current level range
            if (GameManager.Instance == null) return;

            int startNum = GameManager.Instance.CurrentLevelMin;
            
            // Re-sort to ensure Left->Right is Min->Max
            _jars.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

            for (int i = 0; i < _jars.Count; i++)
            {
                _jars[i].SetTargetNumber(startNum + i);
            }
        }
    }
}
