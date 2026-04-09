using UnityEngine;
using TMKOC.Counting100.DragAndDropMode;

namespace TMKOC.Counting100
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "TMKOC.Counting100/LevelData", order = 1)]
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
        public Sprite SecondaryBackgroundSprite;
        public Sprite[] ContainerSprites;
        public Sprite[] BugColorVariants; // Optional: random color sprites for single-prefab levels (e.g., dino eggs)
        public float BugDropScale = 0.35f;
        public float ContainerYOffset = 0f;

        [Header("Level Decorations")]
        public GameObject[] DecorationPrefabs;
        public float DecorationSpawnInterval = 4f;
        public int DecorationMaxActive = 5;
        public float DecorationMoveSpeed = 2f;
        public bool DecorationHorizontalOnly = true;

        [Header("Tutorial Settings")]
        public bool ShowTutorial = true;
    }
}
