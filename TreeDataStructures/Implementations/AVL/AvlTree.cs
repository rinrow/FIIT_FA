using System.Runtime.InteropServices.Marshalling;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;


public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    private int Height(AvlNode<TKey, TValue> ?node) => node == null ? 0 : node.Height;

    private int BFactor(AvlNode<TKey, TValue> node) => node == null ? 0 : Height(node.Right) - Height(node.Left);

    private void UpdHeight(AvlNode<TKey, TValue> node)
    {
        if (node == null) return;
        node.Height = Math.Max(Height(node.Left), Height(node.Right)) + 1; 
    }

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        AvlNode<TKey, TValue> cur = newNode;
        while (cur != null && cur.Parent != null)
        {
            AvlNode < TKey, TValue > par = cur.Parent;
            int parBFactor = BFactor(cur.Parent);
            if (parBFactor == -2)
            {
                if (BFactor(cur) < 0) RotateRight(cur);
                else if (BFactor(cur) > 0) RotateBigRight(cur.Right);

            }
            else if (parBFactor == 2)
            {
                if (BFactor(cur) > 0) RotateLeft(cur);
                else if (BFactor(cur) < 0) RotateBigLeft(cur.Left);
            }
            else if (parBFactor == 0) break;
            UpdHeight(par);
            UpdHeight(cur);
            UpdHeight(cur.Parent);
            cur = cur.Parent;
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        if (parent == null) return;
        AvlNode<TKey, TValue > cur = parent;
        while(cur != null && cur.Parent != null)
        {
            int bf = BFactor(cur);
            if (bf == 2)
            {
                if (BFactor(cur.Right) > 0) RotateLeft(cur.Right);
                else if (BFactor(cur.Left) < 0) RotateBigLeft(cur.Right.Left);
            }
            else if (bf == -2)
            {
                if (BFactor(cur.Left) < 0) RotateRight(cur.Left);
                else if (BFactor(cur.Left) > 0) RotateBigRight(cur.Left.Right);
            }
            else if (bf == -1 || bf == 1) break;
            UpdHeight(cur);
            UpdHeight(cur.Parent);
            if (cur.Parent != null)
            {
                UpdHeight(cur.Parent.Left);
                UpdHeight(cur.Parent.Right);
            }
            cur = cur.Parent;
            cur = cur.Parent;
        }
    }
}