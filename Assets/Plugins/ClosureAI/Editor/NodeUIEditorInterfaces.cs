#if UNITASK_INSTALLED
using UnityEngine.UIElements;
using static ClosureAI.AI;

namespace ClosureAI.Editor.UI
{
    public interface IOnNodeVisualElementClicked
    {
        void OnNodeVisualElementClicked(Node node, ClickEvent e);
    }

    public interface IVisualElementController : IOnNodeVisualElementClicked
    {
    }
}

#endif