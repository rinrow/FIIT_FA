using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return (null, null);
        int cmp = Comparer.Compare(key, root.Key);
        if(cmp > 0)
        {
            (var a, var b) = Split(root.Right, key);
            root.Right = a;
            a?.Parent = root;
            return (root, b);
        }
        else
        {
            (var a, var b) = Split(root.Left, key);
            root.Left = b;
            b?.Parent = root;
            return (a, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if(left == null) return right;
        if (right == null) return left;
        if(left.Priority > right.Priority)
        {
            left.Right = Merge(left.Right, right);
            left.Right?.Parent = left;
            return left;
        } 
        else
        {
            right.Left = Merge(left, right.Left);
            right.Left?.Parent = right;
            return right;
        }
    }
    
    private void Init(TreapNode<TKey, TValue>? x)
    {
        if (x == null) return;
        x.Left?.Parent = x;
        x.Right?.Parent = x;
    }

    public override void Add(TKey key, TValue value)
    {
        var tmp = FindNode(key);
        if (tmp != null)
        {
            tmp.Value = value;
            return;
        }
        var k = CreateNode(key, value);
        (var t1, var t2) = Split(Root, key);
        Root = Merge(Merge(t1, k), t2);
        Root?.Parent = null;
        Count++;
        OnNodeAdded(k);
    }

    private TreapNode<TKey, TValue>? Erase(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return null;
        int cmp = Comparer.Compare(key, root.Key);
        if(cmp == 0) return Merge(root.Left, root.Right);
        if(cmp < 0) root.Left = Erase(root.Left, key);
        else root.Right = Erase(root.Right, key);
        Init(root);
        return root;
    }

    public override bool Remove(TKey key)
    {
        if (!ContainsKey(key)) return false;
        Root = Erase(Root, key);
        Root?.Parent = null;
        Count--;
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);
}