using UnityEngine;
using Counting1To100.DragAndDropMode;

namespace Counting1To100
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "Counting1To100/LevelData", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("Number Range")]
        public int LevelMin = 1;
        public int LevelMax = 10;
        
        [Header("Spawn Prefabs")]
        public GameObject ContainerPrefab;
        public System.Collections.Generic.List<BugController> BugPrefabs;

        [Header("Visual Assets")]
        public Sprite BackgroundSprite;

        [Header("Tutorial Settings")]
        public bool ShowTutorial = true;
    }
}
