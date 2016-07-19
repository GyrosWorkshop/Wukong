using System.Collections.Generic;

namespace Wukong.Helpers
{
    public static class Helpers
    {
        public static LinkedListNode<string> NextOrFirst(this LinkedListNode<string> current)
        {
            if (current?.Next == null)
            {
                return current?.List?.First;
            }
            return current?.Next;
        }

        public static ISet<string> Intersect(this ISet<string> a, IEnumerable<string> b)
        {
            var tmp = new HashSet<string>(a);
            tmp.IntersectWith(b);
            return tmp;
        }
    }
}