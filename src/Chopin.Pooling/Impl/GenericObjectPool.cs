// Copyright (c) labijie.com. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Chopin.Pooling.Collections;
using Chopin.Pooling.Impl.Exceptions;

namespace Chopin.Pooling.Impl
{
    public class GenericObjectPool<T> : BaseGenericObjectPool<T>, IObjectPool<T>, IUsageTracking<T>
    {
        private readonly IPooledObjectFactory<T> _factory;
        private BlockingList<IPooledObject<T>> _idleObjects;
        private int _minIdle;
        private volatile AbandonedConfig _abandonedConfig = null;
        private readonly ConcurrentDictionary<IdentityWrapper<T>, IPooledObject<T>> _allObjects = new ConcurrentDictionary<IdentityWrapper<T>, IPooledObject<T>>();
        private object _closeLock = new object();

        public GenericObjectPool(IPooledObjectFactory<T> factory)
            : this(factory, new GenericObjectPoolConfig())
        {

        }

        public GenericObjectPool(IPooledObjectFactory<T> factory, GenericObjectPoolConfig genericObjectPoolConfig)
            :
            base(genericObjectPoolConfig)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            this._factory = factory;

            this._idleObjects = new BlockingList<IPooledObject<T>>(BorrowStrategy.LIFO);
            this.SetConfig(genericObjectPoolConfig);
            StartEvictor(genericObjectPoolConfig.TimeBetweenEvictionRunsMillis);
        }

        public GenericObjectPool(IPooledObjectFactory<T> factory, GenericObjectPoolConfig config, AbandonedConfig abandonedConfig)
            : this(factory, config)
        {
            this.SetAbandonedConfig(abandonedConfig);
        }

        public int MaxIdle { get; set; }

        public bool Lifo { get; set; }

        public int MinIdle
        {
            set => this._minIdle = value;

            get => this._minIdle > this.MaxIdle ? this.MaxIdle : this._minIdle;
        }

        public bool IsAbandonedConfig => this._abandonedConfig != null;

        public bool LogAbandoned => this._abandonedConfig != null && this._abandonedConfig.LogAbandoned;

        public bool RemoveAbandonedOnBorrow => this._abandonedConfig != null && this._abandonedConfig.RemoveAbandonedOnBorrow;

        public bool RemoveAbandonedOnMaintenance => this._abandonedConfig != null && this._abandonedConfig.RemoveAbandonedOnMaintenance;

        public int RemoveAbandonedTimeout => this._abandonedConfig?.RemoveAbandonedTimeout ?? int.MaxValue;


        private void SetConfig(GenericObjectPoolConfig conf)
        {
            this.MaxIdle = conf.MaxIdle;
            this.MinIdle = conf.MinIdle;
            this.MaxTotal = conf.MaxTotal;
            this.MaxWaitMillis = conf.MaxWaitMillis;
            this.BlockWhenExhausted = conf.BlockWhenExhausted;
            this.TestOnCreate = conf.TestOnCreate;
            this.TestOnBorrow = conf.TestOnBorrow;
            this.TestOnReturn = conf.TestOnReturn;
            this.TestWhileIdle = conf.TestWhileIdle;
            this.NumTestsPerEvictionRun = conf.NumTestsPerEvictionRun;
            this.MinEvictableIdleTimeMillis = conf.MinEvictableIdleTimeMillis;
            this.TimeBetweenEvictionRunsMillis = conf.TimeBetweenEvictionRunsMillis;
            this.SoftMinEvictableIdleTimeMillis = conf.SoftMinEvictableIdleTimeMillis;
            this.EvictionPolicyClassName = conf.EvictionPolicyClassName;
        }

        private void SetAbandonedConfig(AbandonedConfig abandonedConfig)
        {
            if (abandonedConfig == null)
            {
                this._abandonedConfig = null;
            }
            else
            {
                this._abandonedConfig = new AbandonedConfig();
                this._abandonedConfig.LogAbandoned = abandonedConfig.LogAbandoned;
                this._abandonedConfig.LogWriter = abandonedConfig.LogWriter;
                this._abandonedConfig.RemoveAbandonedOnBorrow = abandonedConfig.RemoveAbandonedOnBorrow;
                this._abandonedConfig.RemoveAbandonedOnMaintenance = abandonedConfig.RemoveAbandonedOnMaintenance;
                this._abandonedConfig.RemoveAbandonedTimeout = abandonedConfig.RemoveAbandonedTimeout;
                this._abandonedConfig.UseUsageTracking = abandonedConfig.UseUsageTracking;
            }
        }

        public IPooledObjectFactory<T> Factory => this._factory;

        public int IdleCount { get; }

        public int ActiveCount { get; }

        public T BorrowObject()
        {
            return this.BorrowObject(this.MaxWaitMillis);
        }

        public int NumActive => this._allObjects.Count - this._idleObjects.Size;

        private T BorrowObject(long borrowMaxWaitMillis)
        {
            this.AssertOpen();

            var ac = this._abandonedConfig;
            if (ac != null && ac.RemoveAbandonedOnBorrow && this.NumIdle < 2 && this.NumActive > this.MaxTotal - 3)
            {
                this.RemoveAbandoned(ac);
            }

            IPooledObject<T> p = null;

            bool blockWhenExhausted = base.BlockWhenExhausted;

            bool create;
            var waitTime = DateTime.Now;

            while (p == null)
            {
                create = false;
                if (blockWhenExhausted)
                {
                    p = this._idleObjects.PollFirst();
                    if (p == null)
                    {
                        p = this.Create();
                        if (p != null)
                        {
                            create = true;
                        }
                    }

                    if (p == null)
                    {
                        if (borrowMaxWaitMillis < 0)
                        {
                            p = this._idleObjects.TakeFirst();
                        }
                        else
                        {
                            p = this._idleObjects.PollFirst(TimeSpan.FromMilliseconds(borrowMaxWaitMillis));
                        }
                    }

                    if (p == null)
                    {
                        throw new NoSuchElementException("Timeout waiting for idle object");
                    }

                    if (!p.Allocate())
                    {
                        p = null;
                    }
                }
                else
                {
                    p = this._idleObjects.PollFirst();
                    if (p == null)
                    {
                        p = this.Create();
                        if (p != null)
                        {
                            create = true;
                        }
                    }

                    if (p == null)
                    {
                        throw new NoSuchElementException("Pool exhausted");
                    }
                    if (!p.Allocate())
                    {
                        p = null;
                    }
                }

                if (p != null)
                {
                    try
                    {
                        this.Factory.ActivateObject(p);
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            this.Destroy(p);
                        }
                        catch (Exception)
                        {
                        }

                        p = null;
                        if (create)
                        {
                            var nsee = new NoSuchElementException(
                                "Unable to activate object",
                                e);
                            throw nsee;
                        }
                    }

                    if (p != null && (this.TestOnBorrow || create && this.TestOnCreate))
                    {
                        var validate = false;
                        Exception validationEx = null;

                        try
                        {
                            validate = this.Factory.ValidateObject(p);
                        }
                        catch (Exception e)
                        {
                            PoolUtils.CheckRethrow(e);
                            validationEx = e;
                        }

                        if (!validate)
                        {
                            try
                            {
                                this.Destroy(p);
                                this.DestroyedByBorrowValidationCount.IncrementAndGet();
                            }
                            catch (Exception)
                            {
                            }
                            p = null;
                            if (create)
                            {
                                var nsee = new NoSuchElementException(
                                    "Unable to validate object",
                                    validationEx);
                                throw nsee;
                            }
                        }
                    }
                }
            }

            base.UpdateStatsBorrow(p, (long)(DateTime.Now - waitTime).TotalMilliseconds);
            return p.Object;
        }

        public void ReturnObject(T obj)
        {
            bool isGet = this._allObjects.TryGetValue(new IdentityWrapper<T>(obj), out IPooledObject<T> p);

            if (!isGet)
            {
                if (!this.IsAbandonedConfig)
                {
                    throw new ArgumentException("Returned object not currently part of this pool");
                }
                else
                {
                    return;
                }
            }

            lock (p)
            {
                if (p.State != PooledObjectState.Allocated)
                {
                    throw new ArgumentException("Object has already been returned to this pool or is invalid");
                }
                else
                {
                    p.MarkReturning();
                    ;
                }
            }

            long activeTime = p.ActiveTimeMillis;

            if (this.TestOnReturn)
            {
                if (!this.Factory.ValidateObject(p))
                {
                    try
                    {
                        this.Destroy(p);
                    }
                    catch (Exception e)
                    {
                        base.SwallowException(e);
                    }

                    try
                    {
                        this.EnsureIdle(1, false);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    base.UpdateStatsReturn(activeTime);
                    return;
                }
            }

            try
            {
                this.Factory.PassivateObject(p);
            }
            catch (Exception e1)
            {
                base.SwallowException(e1);
                try
                {
                    this.Destroy(p);
                }
                catch (Exception e)
                {
                    this.SwallowException(e);
                }
                try
                {
                    this.EnsureIdle(1, false);
                }
                catch (Exception e)
                {
                    this.SwallowException(e);
                }
                this.UpdateStatsReturn(activeTime);
                return;
            }

            if (!p.Deallocate())
            {
                throw new ArgumentException("Object has already been returned to this pool or is invalid");
            }

            int maxIdleSave = this.MaxIdle;
            if (this.IsClosed || maxIdleSave > -1 && maxIdleSave <= this._idleObjects.Size)
            {
                try
                {
                    this.Destroy(p);
                }
                catch (Exception e)
                {
                    this.SwallowException(e);
                }
            }
            else
            {
                this._idleObjects.AddFirst(p);

                if (this.IsClosed)
                {
                    this.Clear();
                }
            }
            this.UpdateStatsReturn(activeTime);
        }

        public void InvalidateObject(T obj)
        {
            bool isGet = this._allObjects.TryGetValue(new IdentityWrapper<T>(obj), out IPooledObject<T> p);
            if (!isGet)
            {
                if (this.IsAbandonedConfig)
                {
                    return;
                }
                else
                {
                    throw new ArgumentException("Invalidated object not currently part of this pool");
                }
            }

            lock (p)
            {
                if (p.State != PooledObjectState.Invalid)
                {
                    this.Destroy(p);
                }
            }
            this.EnsureIdle(1, false);
        }

        public void Clear()
        {
            IPooledObject<T> p = this._idleObjects.PollFirst();

            while (p != null)
            {
                try
                {
                    this.Destroy(p);
                }
                catch (Exception e)
                {
                    this.SwallowException(e);
                }
                p = this._idleObjects.PollFirst();
            }
        }

        public void AddObject()
        {
            this.AssertOpen();
            if (this.Factory == null)
            {
                throw new ArgumentException("Cannot add objects without a factory.");
            }

            var p = this.Create();
            this.AddIdleObject(p);
        }

        private void AddIdleObject(IPooledObject<T> p)
        {
            if (p != null)
            {
                Factory.PassivateObject(p);
                _idleObjects.AddFirst(p);
            }
        }

        private void EnsureIdle(int idleCount, bool always)
        {
            if (IdleCount < 1 || IsClosed || (!always && !this._idleObjects.HasTakeWaiters))
            {
                return;
            }

            while (this._idleObjects.Size < idleCount)
            {
                var p = this.Create();
                if (p == null)
                {
                    break;
                }
                this._idleObjects.AddFirst(p);
            }
            if (this.IsClosed)
            {
                this.Clear();
            }
        }

        private void Destroy(IPooledObject<T> toDestory)
        {
            toDestory.Invalidate();
            this._idleObjects.Remove(toDestory);
            this._allObjects.TryRemove(new IdentityWrapper<T>(toDestory.Object), out IPooledObject<T> p);

            try
            {
                this.Factory.DestroyObject(toDestory);
            }
            finally 
            {
                this.DestroyedCount.IncrementAndGet();
                this.CreatedCount.DecrementAndGet();
            }
        }

        private void RemoveAbandoned(AbandonedConfig ac)
        {
            var now = DateTime.Now;
            var timeout = now - TimeSpan.FromSeconds(ac.RemoveAbandonedTimeout);

            var it = this._allObjects.Values.ToArray();
            var remove = new List<IPooledObject<T>>();
            foreach (IPooledObject<T> pooledObject in it)
            {
                lock (pooledObject)
                {
                    if (pooledObject.State == PooledObjectState.Allocated && pooledObject.LastUsedTime <= timeout)
                    {
                        pooledObject.MarkAbandoned();
                        remove.Add(pooledObject);
                    }
                }
            }

            foreach (IPooledObject<T> pooledObject in remove)
            {
                if (ac.LogAbandoned)
                {
                    pooledObject.PrintStackTrace(ac.LogWriter);
                }
                try
                {
                    InvalidateObject(pooledObject.Object);
                }
                catch (Exception)
                {
                }
            }
        }

        public override int NumIdle => this._idleObjects.Size;

        /// <summary>
        ///     Closes the pool, destroys the remaining idle objects
        /// </summary>
        public override void Close()
        {
            if (this.IsClosed)
            {
                return;
            }

            lock (this._closeLock)
            {
                if (this.IsClosed)
                {
                    return;
                }
                this.StartEvictor(-1);
                this._closed = true;
                this.Clear();
                this._idleObjects.InteruptTakeWaiters();
            }
        }

        /// <summary>
        ///     Perform <code>numTests</code> idle object eviction tests, evicting
        ///     examined objects that meet the criteria for eviction.If
        ///     <code>testWhileIdle</code> is true, examined objects are validated
        ///     when visited(and removed if invalid); otherwise only objects that
        ///     have been idle for more than<code>minEvicableIdleTimeMillis</code>
        ///     are removed.
        /// </summary>
        public override void Evict()
        {
            this.AssertOpen();

            if (this._idleObjects.Size > 0)
            {
                IPooledObject<T> unserTest = null;
                var evictionPolicy = this.EvictionPolicy;

                lock (this._evictionLock)
                {
                    var evictionConfig = new EvictionConfig(this.MinEvictableIdleTimeMillis, this.SoftMinEvictableIdleTimeMillis, this.MinIdle);

                    bool testWhileIdle = this.TestWhileIdle;

                    for (int i = 0, m = this.NumTests; i < m; i++)
                    {
                        if (this.evictionIterator == null || this.evictionIterator.Any())
                        {
                            this.evictionIterator = new EvictionIterator<IPooledObject<T>>(this._idleObjects);
                        }

                        if (!this.evictionIterator.GetEnumerator().MoveNext())
                        {
                            return;
                        }

                        try
                        {
                            this.evictionIterator.GetEnumerator().MoveNext();
                            unserTest = this.evictionIterator.GetEnumerator().Current;
                        }
                        catch (NoSuchElementException)
                        {
                            i--;
                            this.evictionIterator = null;
                            continue;
                        }

                        if (!unserTest.StartEvictionTest())
                        {
                            i--;
                            continue;
                        }

                        bool evict;
                        try
                        {
                            evict = evictionPolicy.Evict(evictionConfig, unserTest, this._idleObjects.Size);
                        }
                        catch (Exception e)
                        {
                            PoolUtils.CheckRethrow(e);
                            base.SwallowException(e);
                            evict = false;
                        }

                        if (evict)
                        {
                            this.Destroy(unserTest);
                            this.DestroyedByBorrowValidationCount.IncrementAndGet();
                        }
                        else
                        {
                            if (this.TestWhileIdle)
                            {
                                bool active = false;
                                try
                                {
                                    this.Factory.ActivateObject(unserTest);
                                    active = true;
                                }
                                catch (Exception)
                                {
                                    this.Destroy(unserTest);
                                    this.DestroyedByEvictorCount.IncrementAndGet();
                                }
                                if (active)
                                {
                                    if (!this.Factory.ValidateObject(unserTest))
                                    {
                                        this.Destroy(unserTest);
                                        this.DestroyedByEvictorCount.IncrementAndGet();
                                    }
                                    else
                                    {
                                        try
                                        {
                                            this.Factory.PassivateObject(unserTest);
                                        }
                                        catch (Exception)
                                        {
                                            this.Destroy(unserTest);
                                            this.DestroyedByEvictorCount.IncrementAndGet();
                                        }
                                    }
                                }
                            }
                            if (!unserTest.EndEvictionTest(this._idleObjects))
                            {
                                // TODO - May need to add code here once additional
                                // states are used
                            }
                        }
                    }
                }
            }

            AbandonedConfig ac = this._abandonedConfig;
            if (ac != null && ac.RemoveAbandonedOnMaintenance)
            {
                RemoveAbandoned(ac);
            }
        }

        public int NumTests => this.NumTestsPerEvictionRun >= 0 ? Math.Min(this.NumTestsPerEvictionRun, this._idleObjects.Size) : (int)Math.Ceiling((double)(_idleObjects.Size / Math.Abs((int)this.NumTestsPerEvictionRun)));

        /// <summary>
        ///     Tries to ensure that the configured minimum number of idle instances are
        ///     available in the pool.
        /// </summary>
        internal override void EnsureMinIdle()
        {
            this.EnsureIdle(this.MinIdle,true);
        }

        /// <summary>
        ///     his method is called every time a pooled object to enable the pool to
        ///     better track borrowed objects.
        /// </summary>
        /// <param name="pooledObject">The object that is being used</param>
        public void Use(T pooledObject)
        {
            throw new NotImplementedException();
        }

        public void PreparePool()
        {
            {
                if (this.MinIdle < 1)
                {
                    return;
                }
                this.EnsureMinIdle();
            }
        }

        private IPooledObject<T> Create()
        {
            int localMaxTotal = this.MaxTotal;

            long newCreateCount = this.CreatedCount.IncrementAndGet();
            if (localMaxTotal > -1 && newCreateCount > localMaxTotal || newCreateCount > int.MaxValue)
            {
                this.CreatedCount.DecrementAndGet();
                return null;
            }

            IPooledObject<T> p;
            try
            {
                p = this.Factory.MakeObject();
            }
            catch
            {
                this.CreatedCount.DecrementAndGet();
                throw;
            }

            var ac = this._abandonedConfig;
            if (ac != null && ac.LogAbandoned)
            {
                p.LogAbandoned = true;
            }

            this.CreatedCount.IncrementAndGet();
            this._allObjects.TryAdd(new IdentityWrapper<T>(p.Object), p);
            return p;
        }
    }
}