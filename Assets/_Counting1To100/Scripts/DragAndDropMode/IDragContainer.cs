using UnityEngine;

namespace Counting1To100.DragAndDropMode
{
    public interface IDragContainer
    {
        Transform ContainerTransform { get; }
        int TargetNumber { get; }
        bool IsCompleted { get; }
        void ReceiveDroppedBug(BugController bug);
    }
}
