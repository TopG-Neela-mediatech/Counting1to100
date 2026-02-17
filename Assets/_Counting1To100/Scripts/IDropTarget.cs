using UnityEngine;

namespace Counting1To100
{
    public interface IDropTarget
    {
        Transform DropTarget { get; }
        int Number { get; }
        void ReceiveDrop(BeeController bee);
    }
}
