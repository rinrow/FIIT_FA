using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(e => e.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(e => e.Value).ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        TNode? cur = Root, par = null;
        int cmp = 116;
        while (cur != null)
        {
            cmp = Comparer.Compare(key, cur.Key);
            if (cmp == 0) return;
            par = cur;
            cur = cmp > 0 ? cur.Right : cur.Left;
        }
        Count++;
        TNode new_n = CreateNode(key, value);
        if (par == null) Root = new_n;
        else
        {
            if (cmp > 0) par.Right = new_n;
            else par.Left = new_n;
        }
        new_n.Parent = par;
        OnNodeAdded(new_n);
    }

    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }
        RemoveNode(node);
        Count--;
        return true;
    }

    // Implements standard BST delete logic using Transplant helper 
    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null)
        {
            Transplant(node, node.Right);
            OnNodeRemoved(node.Parent, node.Right);
            OnNodeRemovedDeletedInfo(node.Parent, node.Right, node);
        }
        else if (node.Right == null)
        {
            Transplant(node, node.Left);
            OnNodeRemoved(node.Parent, node.Left);
            OnNodeRemovedDeletedInfo(node.Parent, node.Right, node);
        }
        else
        {
            TNode target = node.Right;
            while (target.Left != null) target = target.Left;
            node.Key = target.Key;
            node.Value = target.Value;
            RemoveNode(target);
        }
    }

    public virtual bool ContainsKey(TKey key) => TryGetValue(key, out TValue? val);
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]

    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set
        {
            TNode? node = FindNode(key);
            if(node == null) Add(key, value);
            else node.Value = value;
        }
    }
    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }

    protected virtual void OnNodeRemovedDeletedInfo(TNode? parent, TNode? child, TNode deleted) { }


    #endregion

    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) return current; 
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if(x == null || x.Parent == null) return;   
        TNode? tmp = x.Left;
        x.Left = x.Parent;
        x.Parent.Right = tmp;
        tmp?.Parent = x.Parent;
        Transplant(x.Parent, x);
        x.Left.Parent = x;
    }

    protected void RotateRight(TNode y)
    {
        if(y == null || y.Parent == null) return;
        TNode? tmp = y.Right;   
        y.Right = y.Parent; 
        y.Parent.Left = tmp;
        tmp?.Parent = y.Parent;
        Transplant(y.Parent, y);
        y.Right.Parent = y;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        RotateRight(x);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        RotateLeft(y);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        RotateLeft(x); 
        RotateLeft(x);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        RotateRight(y);
        RotateRight(y);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        // удалить u и вместо него поставить v
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion

    #region Iterators
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);

    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);

    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private TraversalStrategy _strategy;
        private TNode _root;
        private TNode _current;
        private int _depth;
        private bool _flipped;
        private bool _started;
        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public TreeEntry<TKey, TValue> Current
        {
            get
            {
                if (_current == null)
                    throw new InvalidOperationException();

                return new TreeEntry<TKey, TValue>(
                    _current.Key,
                    _current.Value,
                    _depth
                );
            }
        }
        object IEnumerator.Current => Current;

        public TreeIterator(TNode root, TraversalStrategy strategy)
        {
            this._root = root;
            this._strategy = strategy;
            _flipped = strategy == TraversalStrategy.InOrderReverse || 
                        strategy == TraversalStrategy.PreOrderReverse || 
                        strategy == TraversalStrategy.PostOrderReverse;
            _started = false;
        }

        public bool MoveNext()
        {
            if(!_started)
            {
                Reset();
                _started = true;
                return _current != null;
            }
            switch (_strategy)
            {
                case TraversalStrategy.PreOrder: return MoveNextPreOrder();
                case TraversalStrategy.InOrder: return MoveNextInOrder();
                case TraversalStrategy.PostOrder: return MoveNextPostOrder();
                case TraversalStrategy.PreOrderReverse: return MoveNextPreOrder();
                case TraversalStrategy.InOrderReverse: return MoveNextInOrder();
                case TraversalStrategy.PostOrderReverse: return MoveNextPostOrder();
            }
        }

        private TNode? Left(TNode node) => !_flipped ? node.Left : node.Right;
        private TNode? Right(TNode node) => !_flipped ? node.Right : node.Left;
        private bool IsLeftChild(TNode node) => !_flipped ? node.IsLeftChild : node.IsRightChild;
        private bool IsRightChild(TNode node) => !_flipped ? node.IsRightChild : node.IsLeftChild;


        private bool MoveNextInOrder()
        {
            if(Right(_current) != null)
            {
                _current = Right(_current);
                _depth++;
                while (Left(_current) != null)
                {
                    _depth++;
                    _current = Left(_current);
                }
                return true;
            }
            if(IsLeftChild(_current))
            {
                _depth--;
                _current = _current.Parent;
                return true;
            }

            while (IsRightChild(_current))
            {
                _depth--;
                _current = _current.Parent;
            }
            if (_current.Parent == null) return false;

            _depth--;
            _current = _current.Parent;
            return true;
        }

        private bool MoveNextPreOrder()
        {
            if (Left(_current) != null) 
            {
                _current = Left(_current);
                _depth++;
                return true;
            }
            if(Right(_current) != null)
            {
                _current = Right(_current);
                _depth++;
                return true;
            }
            TNode par = _current.Parent;
            while(par != null)
            {
                if (IsLeftChild(_current) && Right(par) != null)
                {
                    _current = Right(par);
                    return true;
                }
                _depth--;
                _current = par;
                par = par.Parent;
            }
            return false;
        }

        private bool MoveNextPostOrder()
        {
            if(IsLeftChild(_current))
            {
                _current = _current.Parent;
                _depth--;
                if(Right(_current) == null)
                {
                    return true;
                }
                _current = Right(_current);
                _depth++;
                while(true)
                {
                    while (Left(_current) != null)
                    {
                        _depth++;
                        _current = Left(_current);
                    }
                    if (Right(_current) != null)
                    {
                        _depth++;
                        _current = Right(_current);
                    }
                    else break;
                }
                return true;
            }
            if (IsRightChild(_current))
            {
                _depth--;
                _current = _current.Parent;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _depth = 0;
            if (_root == null) return;
            if (_strategy == TraversalStrategy.PreOrder || _strategy == TraversalStrategy.PreOrderReverse) _current = _root;
            if (_strategy == TraversalStrategy.InOrder || _strategy == TraversalStrategy.InOrderReverse)
            {
                _current = _root;
                while (Left(_current) != null)
                {
                    _current = Left(_current);
                    _depth++;
                }
            }
            if (_strategy == TraversalStrategy.PostOrder || _strategy == TraversalStrategy.PostOrderReverse)
            {
                _current = _root;
                while(true)
                {
                    while (Left(_current) != null)
                    {
                        _current = Left(_current);
                        _depth++;
                    }
                    if (Right(_current) != null)
                    {
                        _current = Right(_current);
                        _depth++;
                    }
                    else break;
                }
            }
        }


        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return InOrder().Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value)).GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}