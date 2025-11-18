#if UNITASK_INSTALLED
using static ClosureBT.BT;

namespace ClosureBT
{
    public readonly struct ChangeReactivityStruct
    {
        public readonly bool reactive;

        public ChangeReactivityStruct(bool reactive)
        {
            this.reactive = reactive;
        }

        public static Node operator +(ChangeReactivityStruct reactivity, Node node)
        {
            node.IsReactive = reactivity.reactive;
            return node;
        }

        public static Node operator *(ChangeReactivityStruct reactivity, Node node)
        {
            node.IsReactive = reactivity.reactive;
            return node;
        }

        public static CompositeNode operator +(ChangeReactivityStruct reactivity, CompositeNode node)
        {
            node.IsReactive = reactivity.reactive;
            return node;
        }

        public static CompositeNode operator *(ChangeReactivityStruct reactivity, CompositeNode node)
        {
            node.IsReactive = reactivity.reactive;
            return node;
        }

        public static LeafNode operator +(ChangeReactivityStruct reactivity, LeafNode node)
        {
            node.IsReactive = reactivity.reactive;
            return node;
        }

        public static LeafNode operator *(ChangeReactivityStruct reactivity, LeafNode node)
        {
            node.IsReactive = reactivity.reactive;
            return node;
        }
    }

    public static partial class BT
    {
        public static ChangeReactivityStruct Reactive => new(true);
        public static ChangeReactivityStruct NonReactive => new(false);

        /// <summary>
        /// Sets the current node as a reactive node
        /// </summary>
        public static void MarkAsReactive()
        {
            if (CurrentNode != null)
                CurrentNode.IsReactive = true;
        }
    }
}

#endif
