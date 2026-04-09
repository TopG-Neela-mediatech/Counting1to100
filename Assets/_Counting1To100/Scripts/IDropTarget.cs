using UnityEngine;

namespace TMKOC.Counting100
{
    public interface IDropTarget
    {
        Transform DropTarget { get; }
        int Number { get; }
        bool IsCompleted { get; }
        void ReceiveDrop(BeeController bee);
    }
}
