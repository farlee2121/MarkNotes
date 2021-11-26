public abstract class TreeNode
{
    public abstract IEnumerable<TreeNode> GetChildren();
}

public class Branch : TreeNode
{
    private readonly IEnumerable<TreeNode> nodes;

    public Branch(IEnumerable<TreeNode> nodes)
    {
        this.nodes = nodes;
    }

    public override IEnumerable<TreeNode> GetChildren()
    {
        return nodes;
    }
}

public class Leaf<T> : TreeNode
{
    private readonly T value;

    public Leaf(T value)
    {
        this.value = value;
    }

    public T Value { get => value; }

    public override IEnumerable<TreeNode> GetChildren() => new TreeNode[0];
}

public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> range)
{
    foreach (T item in range)
    {
        queue.Enqueue(item);
    }
}


public static void Walk<T>(T rootNode, Func<T, IEnumerable<T>> getChildren, Action<T> nodeAction)
{
    Queue<T> unwalked = new Queue<T>();
    unwalked.Enqueue(rootNode);
    while (unwalked.Any())
    {
        var node = unwalked.Dequeue();
        unwalked.EnqueueRange(getChildren(node));
        nodeAction(node);
    }
}

public static IEnumerable<T> Collect<T>(T rootNode, Func<T, IEnumerable<T>> getChildren, Func<T, bool> test){
    List<T> collected = new List<T>();
    Walk(rootNode, getChildren, (node) => {
        if(test(node)) collected.Add(node);
    });

    return collected;
}

public static TAgg Fold<TNode, TAgg>(TNode rootNode, Func<TNode, IEnumerable<TNode>> getChildren, Func<TAgg,TNode,TAgg> accumulate, TAgg initial)
{
    TAgg aggregate = initial;
    Walk(rootNode, getChildren, (TNode node) => {
        aggregate = accumulate(aggregate, node);
    });
    return aggregate;
}

void TestTree()
{
    var testTree = new Branch(new TreeNode[] {new Leaf<int>(1),
        new Branch(new TreeNode[]{new Leaf<int>(2), new Leaf<int>(3)}),
        new Branch(new TreeNode[]{new Leaf<int>(4),
                new Branch(new TreeNode[]{new Leaf<int>(5), new Leaf<int>(6)})
        })
    });
    //ast.

    Walk(testTree, (TreeNode n) => n.GetChildren(), n =>
    {
        if (n is Leaf<int> leaf) Console.WriteLine(leaf.Value);
        else if (n is Branch branch) Console.WriteLine("branch");
    });
}
