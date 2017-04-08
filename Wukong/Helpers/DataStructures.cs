using System.Collections.Generic;

namespace Wukong.Helpers
{
    public static class Helpers
    {
        public static LinkedListNode<T> NextOrFirst<T>(this LinkedListNode<T> current, LinkedList<T> list)
        {
            if (current?.List == null) return list.First;
            if (current.Next == null)
            {
                return current.List?.First;
            }
            return current.Next;
        }

        public static ISet<T> Intersect<T>(this ISet<T> a, IEnumerable<T> b)
        {
            var tmp = new HashSet<T>(a);
            tmp.IntersectWith(b);
            return tmp;
        }
    }
}