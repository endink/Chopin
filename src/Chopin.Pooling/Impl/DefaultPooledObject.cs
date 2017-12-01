// Copyright (c) labijie.com. All rights reserved.

using System;
using System.IO;
using Chopin.Pooling.Collections;

namespace Chopin.Pooling.Impl
{
    /// <summary>
    ///     This wrapper is used to track the additional information, such as state, for
    ///     the pooled objects.
    /// </summary>
    public class DefaultPooledObject<T> : IPooledObject<T>
    {
        private readonly T obj;
        private DateTime _lastUseTime;
        private volatile Exception borrowedBy = null;
        private volatile Exception usedBy = null;

        public DefaultPooledObject(T obj)
        {
            this.obj = obj;
        }

        public long BorrowedCount { get; private set; }

        public DateTime CreateTime { get; } = DateTime.Now;

        public long ActiveTimeMillis
        {
            get
            {
                DateTime rTime = this.LastReturnTime;
                DateTime bTime = this.LastBorrowTime;

                if (rTime > bTime)
                {
                    return (long)(rTime - bTime).TotalMilliseconds;
                }
                else
                {
                    return (long)(DateTime.Now - bTime).TotalMilliseconds;
                }
            }
        }

        public long IdleTimeMillis
        {
            get
            {
                long elapsed = (long)(DateTime.Now - this.LastReturnTime).TotalMilliseconds;
                return elapsed >= 0 ? elapsed : 0L;
            }
        }

        public DateTime LastBorrowTime { get; private set; } = DateTime.Now;

        public DateTime LastReturnTime { get; private set; } = DateTime.Now;

        public DateTime LastUsedTime
        {
            get
            {
                if (this.obj is ITrackedUse)
                {
                    return ((ITrackedUse)this.obj).LastUsed > this._lastUseTime ? (this.obj as ITrackedUse).LastUsed : this._lastUseTime;
                }
                else
                {
                    return this._lastUseTime;
                }
            }
            private set => this._lastUseTime = value;
        }

        

        public bool Allocate()
        {
            lock (this)
            {
                if (this.State == PooledObjectState.Idle)
                {
                    this.State = PooledObjectState.Allocated;
                    this.LastBorrowTime = DateTime.Now;
                    this.LastUsedTime = DateTime.Now;
                    this.BorrowedCount++;
                    if (this.LogAbandoned)
                    {
                        this.borrowedBy = new AbandonedObjectCreatedException();
                    }
                    return true;
                }
                else if (this.State == PooledObjectState.Eviction)
                {
                    // TODO Allocate anyway and ignore eviction test
                    this.State = PooledObjectState.EvictionReturnToHead;
                    return false;
                }
                // TODO if validating and testOnBorrow == true then pre-allocate for
                // performance
                return false;
            }
        }

        public bool Deallocate()
        {
            if (this.State == PooledObjectState.Allocated ||
                this.State == PooledObjectState.Returning)
            {
                this.State = PooledObjectState.Idle;
                this.LastReturnTime = DateTime.Now;
                this.borrowedBy = null;
                return true;
            }

            return false;
        }

        public bool Equals(T x, T y)
        {
            return object.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }

        public T Object
        {
            get { return this.obj; }
        }

        public PooledObjectState State { get; private set; } = PooledObjectState.Idle;

        public void Invalidate()
        {
            lock (this)
            {
                this.State = PooledObjectState.Invalid;
            }
        }

        public void MarkAbandoned()
        {
            this.State = PooledObjectState.Abandoned;
        }

        public void MarkReturning()
        {
            this.State = PooledObjectState.Returning;
        }

        public void PrintStackTrace(TextWriter writer)
        {
            bool written = false;
            Exception borrowedByCopy = this.borrowedBy;
            if (borrowedByCopy != null)
            {
                writer.WriteLine(borrowedByCopy.StackTrace);
                written = true;
            }
            Exception usedByCopy = this.usedBy;
            if (usedByCopy != null)
            {
                writer.WriteLine(borrowedByCopy.StackTrace);
                written = true;
            }
            if (written)
            {
                writer.Flush();
            }
        }

        public bool LogAbandoned { get; set; } = false;

        public bool StartEvictionTest()
        {
            lock (this)
            {
                if (this.State == PooledObjectState.Idle)
                {
                    this.State = PooledObjectState.Eviction;
                    return true;
                }

                return false;
            }
        }

        public void Use()
        {
            this.LastUsedTime = DateTime.Now;
            this.usedBy = new Exception("The last code to use this object was: ");
        }

        public int CompareTo(IPooledObject<T> other)
        {
            long lastActiveDiff = (long)(this.LastReturnTime - other.LastReturnTime).TotalMilliseconds;
            if (lastActiveDiff == 0)
            {
                // Make sure the natural ordering is broadly consistent with equals
                // although this will break down if distinct objects have the same
                // identity hash code.
                // see java.lang.Comparable Javadocs
                return this.GetHashCode() - other.GetHashCode();
            }
            // handle int overflow
            return (int)Math.Min(Math.Max(lastActiveDiff, long.MinValue), int.MaxValue);
        }

        public bool EndEvictionTest(BlockingList<IPooledObject<T>> idleQueue)
        {
            lock (this)
            {
                if (this.State == PooledObjectState.Eviction)
                {
                    this.State = PooledObjectState.Idle;
                    return true;
                }
                else if (this.State == PooledObjectState.EvictionReturnToHead)
                {
                    this.State = PooledObjectState.Idle;
                    if (!idleQueue.OfferFirst(this))
                    {
                        // TODO - Should never happen
                    }
                }

                return false;
            }
        }

        /// <summary>返回表示当前对象的字符串。</summary>
        /// <returns>表示当前对象的字符串。</returns>
        public override string ToString()
        {
            return $"{nameof(this.obj)}: {this.obj}, {nameof(this.BorrowedCount)}: {this.BorrowedCount}, {nameof(this.CreateTime)}: {this.CreateTime}, {nameof(this.ActiveTimeMillis)}: {this.ActiveTimeMillis}, {nameof(this.IdleTimeMillis)}: {this.IdleTimeMillis}, {nameof(this.LastBorrowTime)}: {this.LastBorrowTime}, {nameof(this.LastReturnTime)}: {this.LastReturnTime}, {nameof(this.LastUsedTime)}: {this.LastUsedTime}, {nameof(this.Object)}: {this.Object}, {nameof(this.State)}: {this.State}, {nameof(this.LogAbandoned)}: {this.LogAbandoned}";
        }
    }
}