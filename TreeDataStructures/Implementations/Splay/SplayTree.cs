using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private void Splay(BstNode<TKey, TValue> cur)
    {
        if (cur == null) return;
        BstNode<TKey, TValue> par = cur.Parent;
        while (cur != null && cur.Parent != null)
        {
            par = cur.Parent;
            if (par == Root)
            {
                if (cur.IsLeftChild) RotateRight(cur);
                else RotateLeft(cur);
                break;
            }
            else if (par.IsLeftChild && cur.IsLeftChild)
            {
                RotateRight(par);
                RotateRight(cur);
            }
            else if (par.IsRightChild && cur.IsRightChild)
            {
                RotateLeft(par);
                RotateLeft(cur);
            }
            else if (cur.IsLeftChild && par.IsRightChild)
            {
                RotateBigLeft(cur);
            }
            else if (cur.IsRightChild && par.IsLeftChild)
            {
                RotateBigRight(cur);
            }
            cur = cur.Parent;
        }
    }

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        BstNode<TKey, TValue>? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            Splay(node);
            return true;
        }
        value = default;
        return false;
    }
}
