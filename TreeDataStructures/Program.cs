using TreeDataStructures.Implementations.RedBlackTree;
using TreeDataStructures.Interfaces;

RedBlackTree<int, string> Tree = new RedBlackTree<int, string>();

int[] keys = new[] { 50, 30, 70, 20, 40, 60, 80 };
foreach (var k in keys) Tree.Add(k, k.ToString());

foreach (var it2 in Tree.InOrder())
{
    Console.WriteLine(it2.Value);
}


