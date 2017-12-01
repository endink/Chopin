// Copyright (c) labijie.com. All rights reserved.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Chopin.Pooling.Impl.Exceptions;
using Chopin.Pooling.Impl;

namespace Chopin.Pooling.Collections
{
    public class BlockingList<T> : IEnumerable<T>, IDisposable
    {
        private BlockingCollection<T> _blockingCollection;
        private readonly HashSet<T> _deleted;
        private volatile bool _disposed;
        private IProducerConsumerCollection<T> _stack;
        private long _waiters;

        public BlockingList(BorrowStrategy strategy)
        {
            this.Strategy = strategy;
            this._stack = this.GetInnerCollection(strategy);
            this._blockingCollection = new BlockingCollection<T>(this._stack);
            this._deleted = new HashSet<T>();
        }

        public BorrowStrategy Strategy { get; }

        public bool HasTakeWaiters
        {
            get
            {
                this.ThrowIfDisposed();
                return this.TakeQueueLength > 0;
            }
        }

        public int Size
        {
            get
            {
                this.ThrowIfDisposed();
                return this._stack.Count - this._deleted.Count;
            }
        }

        public int TakeQueueLength
        {
            get
            {
                this.ThrowIfDisposed();
                return (int)Interlocked.Read(ref this._waiters);
            }
        }

        public void Dispose()
        {
            if (this._disposed)
            {
                this._disposed = true;
                this._blockingCollection?.Dispose();
                this._stack = null;
                this._blockingCollection = null;
            }
        }



        private IProducerConsumerCollection<T> GetInnerCollection(BorrowStrategy strategy)
        {
            switch (strategy)
            {
                case BorrowStrategy.LIFO:
                    return new ConcurrentStack<T>();
                case BorrowStrategy.FIFO:
                    return new ConcurrentQueue<T>();
                case BorrowStrategy.Random:
                default:
                    return new ConcurrentBag<T>();
            }
        }

        private bool TryPeek(BorrowStrategy strategy, out T item)
        {
            switch (strategy)
            {
                case BorrowStrategy.LIFO:
                    return ((ConcurrentStack<T>)_stack).TryPeek(out item);
                case BorrowStrategy.FIFO:
                    return ((ConcurrentQueue<T>)_stack).TryPeek(out item);
                case BorrowStrategy.Random:
                default:
                    return ((ConcurrentBag<T>)_stack).TryPeek(out item);
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            this.ThrowIfDisposed();
            return this._blockingCollection.Except(this._deleted).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            this.ThrowIfDisposed();
            return this._blockingCollection.Except(this._deleted).GetEnumerator();
        }

        private void ThrowIfDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        public T GetFirst()
        {
            this.ThrowIfDisposed();
            if (this.TryPeek(this.Strategy, out T item))
            {
                if (!this.Destroy(item))
                {
                    return item;
                }
                return this.GetFirst();
            }
            throw new NoSuchElementException();
        }

        public T TakeFirst()
        {
            this.ThrowIfDisposed();
            return this.TakeFirst(false);
        }

        private T TakeFirst(bool inloop)
        {
            if (!inloop)
            {
                Interlocked.Increment(ref this._waiters);
            }
            try
            {
                T item = this._blockingCollection.Take();
                if (!this.Destroy(item))
                {
                    return item;
                }
                return this.TakeFirst(true);
            }
            finally
            {
                if (!inloop)
                {
                    Interlocked.Decrement(ref this._waiters);
                }
            }
        }

        public bool Remove(T o)
        {
            this.ThrowIfDisposed();
            return this._deleted.Add(o);
        }

        private bool Destroy(T o)
        {
            if (this._deleted.Contains(o))
            {
                this._deleted.Remove(o);
                return true;
            }
            return false;
        }

        public T PollFirst()
        {
            return this.PollFirst(TimeSpan.FromMilliseconds(5));
        }

        public T PollFirst(TimeSpan waitTimeSpan)
        {
            this.ThrowIfDisposed();
            if (this._blockingCollection.TryTake(out T item, (int)waitTimeSpan.TotalMilliseconds))
            {
                if (!this.Destroy(item))
                {
                    return item;
                }
                return this.PollFirst();
            }
            return default(T);
        }

        public bool OfferFirst(T item)
        {
            this.ThrowIfDisposed();
            return this._blockingCollection.TryAdd(item, 5);
        }

        public void AddFirst(T item)
        {
            this.ThrowIfDisposed();
            this._blockingCollection.Add(item);
        }

        public void InteruptTakeWaiters()
        {
            this.ThrowIfDisposed();
            this._blockingCollection.CompleteAdding();
        }
    }
}