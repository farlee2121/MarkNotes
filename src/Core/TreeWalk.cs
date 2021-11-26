namespace Notedown.Core
{
    using System.Collections.Generic;

    public static class Tree
    {
        private static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> range)
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

        public static TAgg Fold<TNode, TAgg>(TNode rootNode, Func<TNode, IEnumerable<TNode>> getChildren, Func<TAgg,TNode,TAgg> accumulate, TAgg initial)
		{
			TAgg aggregate = initial;
			Walk(rootNode, getChildren, (TNode node) => {
				aggregate = accumulate(aggregate, node);
			});
			return aggregate;
		}

        public static IEnumerable<T> Collect<T>(T rootNode, Func<T, IEnumerable<T>> getChildren, Func<T, bool> test)
        {
            List<T> collected = new List<T>();
            Walk(rootNode, getChildren, (node) =>
            {
                if (test(node)) collected.Add(node);
            });

            return collected;
        }

    }
}