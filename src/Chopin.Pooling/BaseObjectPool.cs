// Copyright (c) labijie.com. All rights reserved.

using System;

namespace Chopin.Pooling
{
    /// <summary>
    ///     A simple base implementation of {@link ObjectPool}.
    ///     Optional operations are implemented to either do nothing, return a value
    ///     indicating it is unsupported or throw {@link UnsupportedOperationException}.
    ///     This class is intended to be thread-safe.
    /// </summary>
    /// <typeparam name="T">Type of element pooled in this pool.</typeparam>
    public abstract class BaseObjectPool<T> : IObjectPool<T>
    {
        private volatile bool _closed = false;

        public bool IsClosed => this._closed;

        public abstract T BorrowObject();

        public abstract void ReturnObject(T obj);

        public abstract void InvalidateObject(T obj);

        public virtual void AddObject()
        {
            throw new NotImplementedException();
        }

        public virtual int IdleCount => -1;

        public virtual int ActiveCount => -1;

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public virtual void Close()
        {
            this._closed = true;
        }

        protected void AssertOpen()
        {
            if (this.IsClosed)
            {
                throw new ArgumentException("Pool not open");
            }
        }
    }
}