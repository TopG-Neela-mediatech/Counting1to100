using UnityEngine;

namespace TMKOC.Counting100.DragAndDropMode
{
    public interface IDragContainer
    {
        Transform ContainerTransform { get; }
        int TargetNumber { get; }
        bool IsCompleted { get; }
        void ReceiveDroppedBug(BugController bug);
    }
}
