// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling
{
    /// <summary>
    ///     See <see cref="BaseObjectPool{T}" /> for a simple base implementation.
    /// </summary>
    /// <typeparam name="T">Type of element pooled in this pool.</typeparam>
    public interface IObjectPool<T>
    {
        int IdleCount { get; }

        int ActiveCount { get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        T BorrowObject();

        void ReturnObject(T obj);

        void InvalidateObject(T obj);

        void AddObject();

        void Clear();

        void Close();
    }
}