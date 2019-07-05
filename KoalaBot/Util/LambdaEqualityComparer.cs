using System;
using System.Collections.Generic;
using System.Text;

namespace KoalaBot.Util
{
    /// <summary>
    /// Generic Equality comparer
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    public class LambdaEqualityComparer<TSource> : IEqualityComparer<TSource>
    {
        private Func<TSource, TSource, bool> _equalityFunction;
        private Func<TSource, int> _hashFunction;

        public LambdaEqualityComparer(Func<TSource, TSource, bool> equality, Func<TSource, int> hash)
        {
            _equalityFunction = equality;
            _hashFunction = hash;
        }

        public bool Equals(TSource x, TSource y)
        {
            if (_equalityFunction != null)
                return _equalityFunction.Invoke(x, y);

            return x.Equals(y);
        }

        public int GetHashCode(TSource obj)
        {
            if (_hashFunction != null)
                return _hashFunction(obj);

            return obj.GetHashCode();
        }

        /// <summary>
        /// Creates a new Equality Comparer that can be used in Unions
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="equality"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static LambdaEqualityComparer<TSource> Create(Func<TSource, TSource, bool> equality, Func<TSource, int> hash = null)
        {
            return new LambdaEqualityComparer<TSource>(equality, hash);
        }
    }
}
