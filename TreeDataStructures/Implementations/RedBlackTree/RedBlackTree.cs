using System.ComponentModel;
using System.Security.Cryptography;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);
    
    private RbNode<TKey, TValue>? Uncle(RbNode<TKey, TValue> node)
    {
        if (node == null || node.Parent == null || node.Parent.Parent == null) return null;
        return node.Parent.IsLeftChild ? node.Parent.Parent.Right : node.Parent.Parent.Left;
    }

    private RbNode<TKey, TValue>? Grandfather(RbNode<TKey, TValue> node)
    {
        return node?.Parent?.Parent;
    }

    private RbNode<TKey, TValue>? Sibling(RbNode<TKey, TValue> parent, RbNode<TKey, TValue>? child)
        => parent.Left == child ? parent.Right : parent.Left;

    private bool IsBlack(RbNode<TKey, TValue>? node) => node == null || node.Color == RbColor.Black;

    private bool IsRed(RbNode<TKey, TValue>? node) => node != null && node.Color == RbColor.Red;


    protected override void OnNodeAdded(RbNode<TKey, TValue> node)
    {
        InsertCase1(node);
    }

    private void InsertCase1(RbNode<TKey, TValue> node)
    {
        if (node.Parent == null) node.Color = RbColor.Black;
        else InsertCase2(node);
    }

    private void InsertCase2(RbNode<TKey, TValue> node)
    {
        if (IsRed(node.Parent)) InsertCase3(node);
    }

    private void InsertCase3(RbNode<TKey, TValue> node)
    {
        var uncle = Uncle(node);
        if(IsRed(uncle))
        {
            node.Parent!.Color = RbColor.Black;
            uncle.Color = RbColor.Black;
            Grandfather(node)!.Color = RbColor.Red;
            InsertCase1(Grandfather(node)!);
        } 
        else InsertCase4(node);
    }

    private void InsertCase4(RbNode<TKey, TValue> node)
    {
        if(node.IsRightChild && node.Parent.IsLeftChild)
        {
            RotateLeft(node);
            node = node.Left;
        } 
        else if(node.IsLeftChild && node.Parent.IsRightChild)
        {
            RotateRight(node);
            node = node.Right;
        }
        InsertCase5(node);
    }

    private void InsertCase5(RbNode<TKey, TValue> node)
    {
        var gp = Grandfather(node);
        node.Parent.Color = RbColor.Black;
        gp.Color = RbColor.Red;
        if (node.IsLeftChild && node.Parent.IsLeftChild) RotateRight(node.Parent);
        else RotateLeft(node.Parent);
    }

    protected override void OnNodeRemovedDeletedInfo(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child, RbNode<TKey, TValue> deleted)
    {
        if (IsBlack(deleted))
        {
            if (IsRed(child)) child.Color = RbColor.Black;
            else DeleteCase1(parent, child);
        }
    }

    private void DeleteCase1(RbNode<TKey, TValue> parent, RbNode<TKey, TValue> node)
    {
        if (parent != null) DeleteCase2(parent, node);
    }

    private void DeleteCase2(RbNode<TKey, TValue> parent, RbNode<TKey, TValue> node)
    {
        var s = Sibling(parent, node);
        if (IsRed(s))
        {
            parent.Color = RbColor.Red;
            s.Color = RbColor.Black;
            if (s.IsLeftChild) RotateRight(s);
            else RotateLeft(s);
        }
        DeleteCase3(parent, node);
    }

    private void DeleteCase3(RbNode<TKey, TValue> parent, RbNode<TKey, TValue> node)
    {
        var s = Sibling(parent, node);
        if (IsBlack(parent) && IsBlack(s) && IsBlack(s.Left)
                && IsBlack(s.Right))
        {
            s.Color = RbColor.Red;
            DeleteCase1(parent.Parent, parent);
        }
        else DeleteCase4(parent, node);
    }

    private void DeleteCase4(RbNode<TKey, TValue> parent, RbNode<TKey, TValue> node)
    {
        var s = Sibling(parent, node);
        if (IsRed(parent) && IsBlack(s) && IsBlack(s.Left) && IsBlack(s.Right))
        {
            s.Color = RbColor.Red;
            parent.Color = RbColor.Black;
        }
        else DeleteCase5(parent, node);
    }

    private void DeleteCase5(RbNode<TKey, TValue> parent, RbNode<TKey, TValue> node)
    {
        var s = Sibling(parent, node);
        if(s.Color == RbColor.Black)
        {
            if(s.IsRightChild && IsRed(s.Left) && IsBlack(s.Right))
            {
                s.Color = RbColor.Red;
                s.Left.Color = RbColor.Black;
                RotateRight(s.Left);
            } else if (s.IsLeftChild && IsRed(s.Right) && IsBlack(s.Left))
            {
                s.Color = RbColor.Red;
                s.Right.Color = RbColor.Black;
                RotateLeft(s.Right);
            }
        }
        DeleteCase6(parent, node);
    }

    private void DeleteCase6(RbNode<TKey, TValue> parent, RbNode<TKey, TValue> node)
    {
        var s = Sibling(parent, node);
        s.Color = parent.Color;
        parent.Color = RbColor.Black;
        if(s.IsRightChild)
        {
            s.Right.Color = RbColor.Black;
            RotateLeft(s);
        } 
        else
        {
            s.Left.Color = RbColor.Black;
            RotateRight(s);
        }
    }
}
