// Copyright (c) labijie.com. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Chopin.Pooling.Collections;
using Chopin.Pooling.Impl.Atom;
using Chopin.Pooling.Impl.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chopin.Pooling.Impl
{
    /// <summary>
    ///     * A configurable <code>KeyedObjectPool</code> implementation.
    ///     When coupled with the appropriate { @link KeyedPooledObjectFactory },
    ///     <code>GenericKeyedObjectPool</code> provides robust pooling functionality for
    ///     keyed objects.A<code> GenericKeyedObjectPool</code> can be viewed as a map
    ///     of sub-pools, keyed on the (unique) key values provided to the
    ///     {@link #preparePool preparePool}, {@link #addObject addObject} or
    ///     {@link #borrowObject borrowObject} methods. Each time a new key value is
    ///     provided to one of these methods, a sub-new pool is created under the given
    ///     key to be managed by the containing<code>GenericKeyedObjectPool.</code>
    ///     Optionally, one may configure the pool to examine and possibly Evict objects
    ///     as they sit idle in the pool and to ensure that a minimum number of idle
    ///     objects is maintained for each key. This is performed by an "idle object
    ///     eviction" thread, which runs asynchronously. Caution should be used when
    ///     configuring this optional feature.Eviction runs contend with client threads
    ///     for access to objects in the pool, so if they run too frequently performance
    ///     issues may result.
    ///     Implementation note: To prevent possible deadlocks, care has been taken to
    ///     ensure that no call to a factory method will occur within a synchronization
    ///     block.See POOL - 125 and DBCP-44 for more information.
    ///     This class is intended to be thread-safe.
    /// </summary>
    /// <typeparam name="TKey">The type of keys maintained by this pool.</typeparam>
    /// <typeparam name="TValue">Type of element pooled in this pool.</typeparam>
    public class GenericKeyedObjectPool<TKey, TValue> : BaseGenericObjectPool<TValue>, IKeyedObjectPool<TKey, TValue>, IDisposable
    {
        private readonly ReaderWriterLockSlim _keyLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly AtomInt _numTotal = 0;
        private readonly List<TKey> _poolKeyList = new List<TKey>(); // @GuardedBy("keyLock")
        private readonly object _closeLock = new object();
        private readonly IDictionary<TKey, ObjectDeque<TValue>> poolMap = new ConcurrentDictionary<TKey, ObjectDeque<TValue>>();
        private readonly GenericKeyedObjectPoolConfig _config;
        private TKey _evictionKey = default(TKey);
        private IEnumerable<TKey> _evictionKeyIterator = null;
        private ILogger _logger;

        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        public GenericKeyedObjectPool(IKeyedPooledObjectFactory<TKey, TValue> factory,
            GenericKeyedObjectPoolConfig genericKeyedObjectPoolConfig,
            IEvictionTimer timer = null,
            ILoggerFactory loggerFactory = null)
            : base(genericKeyedObjectPoolConfig, timer, loggerFactory)
        {
            this._logger = loggerFactory?.CreateLogger(this.GetType().Name) ?? NullLogger.Instance;
            this._config = genericKeyedObjectPoolConfig ?? new GenericKeyedObjectPoolConfig();
            this.Factory = factory ?? throw new ArgumentException("factory may not be null");

            this.StartEvictor(this.TimeBetweenEvictionRunsMillis);
        }

        /// <summary>
        ///     Obtain a reference to the factory used to create, destroy and validate
        ///     the objects used by this pool.
        /// </summary>
        public IKeyedPooledObjectFactory<TKey, TValue> Factory { get; }

        private bool HasBorrowWaiters
        {
            get
            {

                foreach (var kvp in this.TraversalConcurrentDic(this.poolMap))
                {
                    ObjectDeque<TValue> deque = kvp.Value;
                    if (deque != null)
                    {
                        BlockingList<IPooledObject<TValue>> pool = deque.GetIdleObjects();
                        if (pool.HasTakeWaiters)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private int NumTests
        {
            get
            {
                int totalIdle = this.NumIdle;
                int numTests = this.NumTestsPerEvictionRun;
                if (numTests >= 0)
                {
                    return Math.Min(numTests, totalIdle);
                }
                return (int)(Math.Ceiling(totalIdle / Math.Abs((double)numTests)));
            }
        }

        /// <summary>
        ///     /// Returns the limit on the number of object instances allocated by the pool
        ///     (checked out or idle), per key.When the limit is reached, the sub-pool
        ///     is said to be exhausted.A negative value indicates no limit.
        /// </summary>
        public int MaxTotalPerKey
        {
            get => this._config.MaxTotalPerKey;
            set => this._config.MaxTotalPerKey = value;
        }

        /// <summary>
        ///     /// Returns the cap on the number of "idle" instances per key in the pool.
        ///     If maxIdlePerKey is set too low on heavily loaded systems it is possible
        ///     you will see objects being destroyed and almost immediately new objects
        ///     being created.This is a result of the active threads momentarily
        ///     returning objects faster than they are requesting them them, causing the
        ///     number of idle objects to rise above maxIdlePerKey.The best value for
        ///     maxIdlePerKey for heavily loaded system will vary but the default is a
        ///     good starting point.
        /// </summary>
        public int MaxIdlePerKey
        {
            get { return this._config.MaxIdlePerKey; }
            set { this._config.MaxIdlePerKey = value; }
        }

        /// <summary>
        ///     the target for the minimum number of idle objects to maintain in
        ///     each of the keyed sub-pools.This setting only has an effect if it is
        ///     positive and{@link #getTimeBetweenEvictionRunsMillis()} is greater than
        ///     zero. If this is the case, an attempt is made to ensure that each
        ///     sub-pool has the required minimum number of instances during idle object
        ///     eviction runs.
        ///     If the configured value of minIdlePerKey is greater than the configured
        ///     value for maxIdlePerKey then the value of maxIdlePerKey will be used
        ///     instead.
        /// </summary>
        public int MinIdlePerKey
        {
            get
            {
                int maxIdlePerKeySave = this.MaxIdlePerKey;
                if (this._config.MinIdlePerKey > maxIdlePerKeySave)
                {
                    return maxIdlePerKeySave;
                }
                else
                {
                    return this._config.MinIdlePerKey;
                }
            }
            set { this._config.MinIdlePerKey = value; }
        }

        /// <summary>
        ///     See {@link GenericKeyedObjectPool#getNumActivePerKey()}
        ///     @return See {@link GenericKeyedObjectPool#getNumActivePerKey()}
        /// </summary>
        public IDictionary<string, int> NumActivePerKey
        {
            get
            {
                var result = new Dictionary<string, int>();

                foreach (KeyValuePair<TKey, ObjectDeque<TValue>> keyValuePair in this.TraversalConcurrentDic(this.poolMap))
                {
                    if (keyValuePair.Value != null)
                    {
                        result.Add(keyValuePair.Key.ToString(), keyValuePair.Value.GetAllObjects().Count - keyValuePair.Value.GetIdleObjects().Size);
                    }
                }

                return result;
            }
        }

        private IEnumerable<KeyValuePair<TK, TV>> TraversalConcurrentDic<TK, TV>(IDictionary<TK, TV> dic)
        {
            TK[] keys = dic.Keys.ToArray();
            foreach (TK key in keys)
            {
                yield return new KeyValuePair<TK, TV>(key, dic[key]);
            }
        }

        /// <summary>
        ///     See {@link GenericKeyedObjectPool#getNumWaiters()}
        ///     @return See {@link GenericKeyedObjectPool#getNumWaiters()}
        /// </summary>
        public int NumWaiters
        {
            get
            {
                int result = 0;

                if (this.BlockWhenExhausted)
                {
                    foreach (ObjectDeque<TValue> poolMapValue in this.poolMap.Values)
                    {
                        result += poolMapValue.GetIdleObjects().TakeQueueLength;
                    }
                }
                return result;
            }
        }

        /// <summary>
        ///     See {@link GenericKeyedObjectPool#getNumWaitersByKey()}
        ///     @return See {@link GenericKeyedObjectPool#getNumWaitersByKey()}
        /// </summary>
        public IDictionary<string, int> NumWaitersByKey
        {
            get
            {
                var result = new Dictionary<string, int>();

                foreach (KeyValuePair<TKey, ObjectDeque<TValue>> keyValuePair in this.TraversalConcurrentDic(this.poolMap))
                {
                    if (keyValuePair.Value != null)
                    {
                        result.Add(keyValuePair.Key.ToString(), keyValuePair.Value.GetIdleObjects().TakeQueueLength);
                    }
                    else
                    {
                        result.Add(keyValuePair.Key.ToString(), 0);
                    }
                }
                return result;
            }
        }

        protected override void DisposeManagedResources()
        {
            this.Clear();
            this._keyLock?.Dispose();
        }


        /// <summary>
        ///     Equivalent to
        ///     <code>{@link #borrowObject(Object, long) borrowObject}(key,
        ///  {@link #getMaxWaitMillis()})</code>
        ///     .
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue BorrowObject(TKey key)
        {
            return this.BorrowObject(key, this.MaxWaitMillis);
        }

        /// <summary>
        ///     Returns an object to a keyed sub-pool.
        ///     If {@link #getMaxIdlePerKey() maxIdle} is set to a positive value and the
        ///     number of idle instances under the given key has reached this value, the
        ///     returning instance is destroyed.
        ///     If {@link #getTestOnReturn() testOnReturn} == true, the returning
        ///     instance is validated before being returned to the idle instance sub-pool
        ///     under the given key.In this case, if validation fails, the instance is
        ///     destroyed.
        ///     Exceptions encountered destroying objects for any reason are swallowed
        ///     but notified via a {@link SwallowedExceptionListener}.
        /// </summary>
        /// <param name="key">pool key</param>
        /// <param name="obj">instance to return to the keyed pool</param>
        public void ReturnObject(TKey key, TValue obj)
        {
            ObjectDeque<TValue> objectDeque = this.poolMap[key];

            IPooledObject<TValue> p = objectDeque.GetAllObjects()[new IdentityWrapper<TValue>(obj)];

            if (p == null)
            {
                throw new ArgumentException("Returned object not currently part of this pool");
            }

            lock (p)
            {
                PooledObjectState state = p.State;
                if (state != PooledObjectState.Allocated)
                {
                    throw new ArgumentException("Object has already been returned to this pool or is invalid");
                }
                else
                {
                    p.MarkReturning();
                }
            }

            long activeTime = p.ActiveTimeMillis;
            if (this.TestOnReturn)
            {
                if (!this.Factory.ValidateObject(key, p))
                {
                    try
                    {
                        this.Destory(key, p, true);
                    }
                    catch (Exception e)
                    {
                        this.SwallowException(e);
                    }

                    if (objectDeque.GetIdleObjects().HasTakeWaiters)
                    {
                        try
                        {
                            this.AddObject(key);
                        }
                        catch (Exception e)
                        {
                            this.SwallowException(e);
                        }
                    }
                    this.UpdateStatsReturn(activeTime);
                    return;
                }
            }

            try
            {
                this.Factory.PassivateObject(key, p);
            }
            catch (Exception e1)
            {
                this.SwallowException(e1);
                try
                {
                    this.Destory(key, p, true);
                }
                catch (Exception e)
                {
                    SwallowException(e);
                }
                if (objectDeque.GetIdleObjects().HasTakeWaiters)
                {
                    try
                    {
                        AddObject(key);
                    }
                    catch (Exception e)
                    {
                        SwallowException(e);
                    }
                }
                UpdateStatsReturn(activeTime);
                return;
            }

            if (!p.Deallocate())
            {
                throw new ArgumentException("Object has already been returned to this pool");
            }

            int maxIdle = this.MaxIdlePerKey;
            BlockingList<IPooledObject<TValue>> idleObjects = objectDeque.GetIdleObjects();

            if (this.IsClosed || maxIdle > -1 && maxIdle <= idleObjects.Size)
            {
                try
                {
                    this.Destory(key, p, true);
                }
                catch (Exception e)
                {
                    this.SwallowException(e);
                }
            }
            else
            {
                idleObjects.AddFirst(p);

                if (this.IsClosed)
                {
                    // Pool closed while object was being added to idle objects.
                    // Make sure the returned object is destroyed rather than left
                    // in the idle object pool (which would effectively be a leak)
                    this.Clear(key);
                }
            }

            if (this.HasBorrowWaiters)
            {
                this.ReuseCapacity();
            }

            this.UpdateStatsReturn(activeTime);
        }

        /// <summary>
        ///     Activation of this method decrements the active count associated with
        ///     the given keyed pool and attempts to destroy<code>obj.</code>
        /// </summary>
        /// <param name="key">pool key</param>
        /// <param name="obj">instance to invalidate</param>
        public void InvalidateObject(TKey key, TValue obj)
        {
            ObjectDeque<TValue> objectDeque = this.poolMap[key];
            IPooledObject<TValue> p = objectDeque.GetAllObjects()[new IdentityWrapper<TValue>(obj)];
            if (p == null)
            {
                throw new ArgumentException(
                    "Object not currently part of this pool");
            }

            lock (p)
            {
                if (p.State != PooledObjectState.Invalid)
                {
                    this.Destory(key, p, true);
                }
            }
            if (objectDeque.GetIdleObjects().HasTakeWaiters)
            {
                this.AddObject(key);
            }
        }

        /// <summary>
        ///     Clears any objects sitting idle in the pool by removing them from the
        ///     idle instance sub-pools and then invoking the configured
        ///     PoolableObjectFactory's
        ///     {@link KeyedPooledObjectFactory#destroyObject(Object, PooledObject)}
        ///     method on each idle instance.
        ///     Implementation notes:
        ///     <li>
        ///         This method does not destroy or effect in any way instances that are
        ///         checked out when it is invoked.
        ///     </li>
        ///     <li>
        ///         Invoking this method does not prevent objects being returned to the
        ///         idle instance pool, even during its execution. Additional instances may
        ///         be returned while removed items are being destroyed.
        ///     </li>
        ///     <li>
        ///         Exceptions encountered destroying idle instances are swallowed
        ///         but notified via a {@link SwallowedExceptionListener}.
        ///     </li>
        /// </summary>
        public void Clear()
        {
            foreach (TKey key in this.poolMap.Keys.ToArray())
            {
                this.Clear(key);
            }
        }

        /// <summary>
        ///     Clears the specified sub-pool, removing all pooled instances
        ///     corresponding to the given<code>key</code>.Exceptions encountered
        ///     destroying idle instances are swallowed but notified via a
        /// </summary>
        /// <param name="key"></param>
        public void Clear(TKey key)
        {
            ObjectDeque<TValue> objectDeque = this.Register(key);

            try
            {
                BlockingList<IPooledObject<TValue>> idleObjects = objectDeque.GetIdleObjects();

                IPooledObject<TValue> p = idleObjects.PollFirst();

                while (p != null)
                {
                    try
                    {
                        this.Destory(key, p, true);
                    }
                    catch (Exception e)
                    {
                        this.SwallowException(e);
                    }
                    p = idleObjects.PollFirst();
                }

                idleObjects.Dispose();
            }
            finally
            {
                this.Deregister(key);
            }
        }

        public int NumActive => this._numTotal.Value - this.NumIdle;

        public override int NumIdle => this.poolMap.Sum(x => x.Value.GetIdleObjects().Size);

        public int GetNumActive(TKey key)
        {
            ObjectDeque<TValue> objDeque = this.poolMap[key];
            return objDeque.GetAllObjects().Count - objDeque.GetIdleObjects().Size;
        }

        public int GetNumIdle(TKey key)
        {
            ObjectDeque<TValue> dequeue = this.poolMap[key];
            return dequeue?.GetIdleObjects().Size ?? 0;
        }

        /// <summary>
        ///     Closes the keyed object pool. Once the pool is closed,
        ///     {@link #borrowObject(Object)} will fail with IllegalStateException, but
        ///     {@link #returnObject(Object, Object)} and
        ///     {@link #invalidateObject(Object, Object)} will continue to work, with
        ///     returned objects destroyed on return.
        ///     Destroys idle instances in the pool by invoking{@link #clear()}.
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

                this.StartEvictor(-1L);

                this._closed = true;

                this.Clear();

                foreach (ObjectDeque<TValue> poolMapValue in this.poolMap.Values)
                {
                    poolMapValue.GetIdleObjects().InteruptTakeWaiters();
                }
                this.Clear();
            }
        }

        public void AddObject(TKey key)
        {
            this.AssertOpen();
            this.Register(key);
            try
            {
                IPooledObject<TValue> p = this.Create(key);
                this.AddIdleObject(key, p);
            }
            finally
            {
                this.Deregister(key);
            }
        }

        /// <summary>
        ///     /// Borrows an object from the sub-pool associated with the given key using
        ///     the specified waiting time which only applies if
        ///     {@link #getBlockWhenExhausted()} is true.
        ///     If there is one or more idle instances available in the sub-pool
        ///     associated with the given key, then an idle instance will be selected
        ///     based on the value of {@link #getLifo()}, activated and returned.  If
        ///     activation fails, or {@link #getTestOnBorrow() testOnBorrow} is set to
        ///     <code>true</code> and validation fails, the instance is destroyed and the
        ///     next available instance is examined.This continues until either a valid
        ///     instance is returned or there are no more idle instances available.
        ///     If there are no idle instances available in the sub-pool associated with
        ///     the given key, behavior depends on the {@link #getMaxTotalPerKey()
        ///     maxTotalPerKey}, {@link #getMaxTotal() maxTotal}, and (if applicable)
        ///     {@link #getBlockWhenExhausted()} and the value passed in to the
        ///     <code>borrowMaxWaitMillis</code> parameter.If the number of instances checked
        ///     out from the sub-pool under the given key is less than
        ///     <code>maxTotalPerKey</code> and the total number of instances in
        ///     circulation (under all keys) is less than<code>maxTotal</code>, a new
        ///     instance is created, activated and(if applicable) validated and returned
        ///     to the caller.If validation fails, a<code> NoSuchElementException</code>
        ///     will be thrown.
        ///     If the associated sub-pool is exhausted (no available idle instances and
        ///     no capacity to create new ones), this method will either block
        ///     ({@link #getBlockWhenExhausted()} is true) or throw a
        ///     <code>NoSuchElementException</code>
        ///     ({@link #getBlockWhenExhausted()} is false).
        ///     The length of time that this method will block when
        ///     {@link #getBlockWhenExhausted()} is true is determined by the value
        ///     passed in to the <code>borrowMaxWait</code> parameter.
        ///     When<code> maxTotal</code> is set to a positive value and this method is
        ///     invoked when at the limit with no idle instances available under the requested
        ///     key, an attempt is made to create room by clearing the oldest 15% of the
        ///     elements from the keyed sub-pools.
        ///     When the pool is exhausted, multiple calling threads may be
        ///     simultaneously blocked waiting for instances to become available. A
        ///     "fairness" algorithm has been implemented to ensure that threads receive
        ///     available instances in request arrival order.
        /// </summary>
        /// <param name="key">pool key</param>
        /// <param name="borrowMaxWaitMillis">The time to wait in milliseconds for an object to become available</param>
        /// <returns></returns>
        public TValue BorrowObject(TKey key, long borrowMaxWaitMillis)
        {
            this.AssertOpen();

            IPooledObject<TValue> p = null;

            // Get local copy of current config so it is consistent for entire
            // method execution
            bool blockWhenExhausted = this.BlockWhenExhausted;

            DateTime waitTime = DateTime.Now;
            ObjectDeque<TValue> objectDeque = this.Register(key);

            try
            {
                while (p == null)
                {
                    bool create = false;
                    if (blockWhenExhausted)
                    {
                        p = objectDeque.GetIdleObjects().PollFirst();
                        if (p == null)
                        {
                            p = this.Create(key);
                            if (p != null)
                            {
                                create = true;
                            }
                        }

                        if (p == null)
                        {
                            if (borrowMaxWaitMillis < 0)
                            {
                                p = objectDeque.GetIdleObjects().TakeFirst();
                            }
                            else
                            {
                                p = objectDeque.GetIdleObjects().PollFirst();
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
                        p = objectDeque.GetIdleObjects().PollFirst();
                        if (p == null)
                        {
                            p = this.Create(key);
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
                            this.Factory.ActivateObject(key, p);
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                this.Destory(key, p, true);
                            }
                            catch (Exception)
                            {
                            }
                            p = null;
                            if (create)
                            {
                                var nsee = new NoSuchElementException("Unable to activate object", e);
                                throw nsee;
                            }
                        }

                        if (p != null && (this.TestOnBorrow || create && this.TestOnCreate))
                        {
                            bool validate = false;

                            Exception validationEx = null;
                            try
                            {
                                validate = this.Factory.ValidateObject(key, p);
                            }
                            catch (Exception e)
                            {
                                validationEx = e;
                            }

                            if (!validate)
                            {
                                try
                                {
                                    this.Destory(key, p, true);
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
            }
            finally
            {
                this.Deregister(key);
            }
            this.UpdateStatsBorrow(p, (long)(DateTime.Now - waitTime).TotalMilliseconds);
            return p.Object;
        }

        /// <summary>
        ///     See {@link GenericKeyedObjectPool#listAllObjects()}
        ///     @return See {@link GenericKeyedObjectPool#listAllObjects()}
        /// </summary>
        public IDictionary<string, List<DefaultPooledObjectInfo<TValue>>> ListAllObjects()
        {
            var result = new Dictionary<string, List<DefaultPooledObjectInfo<TValue>>>();

            foreach (KeyValuePair<TKey, ObjectDeque<TValue>> keyValuePair in this.poolMap)
            {
                if (keyValuePair.Value != null)
                {
                    var lst = new List<DefaultPooledObjectInfo<TValue>>();

                    result.Add(keyValuePair.Key.ToString(), lst);
                    foreach (IPooledObject<TValue> p in keyValuePair.Value.GetAllObjects().Values)
                    {
                        lst.Add(new DefaultPooledObjectInfo<TValue>(p));
                    }
                }
            }
            return result;
        }

        /// <summary>
        ///     Clears oldest 15% of objects in pool.  The method sorts the objects into
        ///     a TreeMap and then iterates the first 15% for removal.
        /// </summary>
        public void ClearOldest()
        {
            var map = new SortedDictionary<IPooledObject<TValue>, TKey>();
            foreach (KeyValuePair<TKey, ObjectDeque<TValue>> keyValuePair in this.poolMap)
            {
                if (keyValuePair.Value != null)
                {
                    foreach (IPooledObject<TValue> idleObj in keyValuePair.Value.GetIdleObjects())
                    {
                        map.Add(idleObj, keyValuePair.Key);
                    }
                }
            }

            // Now iterate created map and kill the first 15% plus one to account
            // for zero
            int itemsToRemove = (int)(map.Count * 0.15) + 1;

            foreach (KeyValuePair<IPooledObject<TValue>, TKey> keyValuePair in map)
            {
                if (itemsToRemove <= 0)
                {
                    break;
                }
                bool destoryed = true;
                try
                {
                    destoryed = this.Destory(keyValuePair.Value, keyValuePair.Key, false);
                }
                catch (Exception e)
                {
                    this.SwallowException(e);
                }

                if (destoryed)
                {
                    itemsToRemove--;
                }
            }
        }

        /// <summary>
        ///     Attempt to create one new instance to serve from the most heavily
        ///     loaded pool that can add a new instance.
        ///     This method exists to ensure liveness in the pool when threads are
        ///     parked waiting and capacity to create instances under the requested keys
        ///     subsequently becomes available.
        ///     This method is not guaranteed to create an instance and its selection
        ///     of the most loaded pool that can create an instance may not always be
        ///     correct, since it does not lock the pool and instances may be created,
        ///     borrowed, returned or destroyed by other threads while it is executing.
        /// </summary>
        private void ReuseCapacity()
        {
            int maxTotalPerKeySave = this.MaxTotalPerKey;

            int maxQueueLength = 0;

            BlockingList<IPooledObject<TValue>> mostLoaded = null;
            TKey loadedKey = default(TKey);

            foreach (KeyValuePair<TKey, ObjectDeque<TValue>> pooledItem in this.poolMap)
            {
                if (pooledItem.Value != null)
                {
                    BlockingList<IPooledObject<TValue>> pool = pooledItem.Value.GetIdleObjects();
                    int queueLength = pool.TakeQueueLength;
                    if (this.GetNumActive(pooledItem.Key) < maxTotalPerKeySave && queueLength > maxQueueLength)
                    {
                        maxQueueLength = queueLength;
                        mostLoaded = pool;
                        loadedKey = pooledItem.Key;
                    }
                }
            }

            // Attempt to add an instance to the most loaded pool
            if (mostLoaded != null)
            {
                this.Register(loadedKey);
                try
                {
                    IPooledObject<TValue> p = this.Create(loadedKey);
                }
                catch (Exception e)
                {
                    this.SwallowException(e);
                }
                finally
                {
                    this.Deregister(loadedKey);
                }
            }
        }

        private void EvictCore()
        {
            this.AssertOpen();

            if (this.NumIdle == 0)
            {
                return;
            }

            IPooledObject<TValue> underTest = null;
            IEvictionPolicy<TValue> evictionPolicy = this.EvictionPolicy;

            lock (evictionPolicy)
            {
                var evictionConfig = new EvictionConfig(
                    this.MinEvictableIdleTimeMillis,
                    this.SoftMinEvictableIdleTimeMillis,
                    this.MinIdlePerKey);

                bool testWhileIdle = this.TestWhileIdle;

                for (int i = 0, m = this.NumTests; i < m; i++)
                {
                    if (this.evictionIterator == null || !this.evictionIterator.Any())
                    {
                        if (this._evictionKeyIterator == null || !this._evictionKeyIterator.Any())
                        {
                            var keyCopy = new List<TKey>();
                            this._keyLock.TryEnterReadLock(-1);
                            try
                            {
                                keyCopy.AddRange(this._poolKeyList);
                            }
                            finally
                            {
                                this._keyLock.ExitReadLock();
                            }
                            this._evictionKeyIterator = keyCopy;
                        }

                        foreach (TKey key in this._evictionKeyIterator)
                        {
                            this._evictionKey = key;
                            ObjectDeque<TValue> objectDeque = this.poolMap[key];
                            if (objectDeque == null)
                            {
                                continue;
                            }

                            BlockingList<IPooledObject<TValue>> idleObjects = objectDeque.GetIdleObjects();
                            this.evictionIterator = new EvictionIterator<IPooledObject<TValue>>(idleObjects);
                            if (this._evictionKeyIterator.Any())
                            {
                                break;
                            }
                            this.evictionIterator = null;
                        }
                    }

                    if (this._evictionKeyIterator == null)
                    {
                        return;
                    }

                    BlockingList<IPooledObject<TValue>> idleObjectsQueue;
                    try
                    {
                        this.evictionIterator.GetEnumerator().MoveNext();
                        underTest = this.evictionIterator.GetEnumerator().Current;
                        idleObjectsQueue = this.evictionIterator.IdleObjects;
                    }
                    catch (NoSuchElementException)
                    {
                        // Object was borrowed in another thread
                        // Don't count this as an eviction test so reduce i;
                        i--;
                        this.evictionIterator = null;
                        continue;
                    }

                    if (!underTest.StartEvictionTest())
                    {
                        // Object was borrowed in another thread
                        // Don't count this as an eviction test so reduce i;
                        i--;
                        continue;
                    }

                    bool evict;
                    try
                    {
                        evict = evictionPolicy.Evict(evictionConfig, underTest, this.poolMap[this._evictionKey].GetIdleObjects().Size);
                    }
                    catch (Exception e)
                    {
                        PoolUtils.CheckRethrow(e);
                        this.SwallowException(new Exception("", e));
                        evict = false;
                    }

                    if (evict)
                    {
                        this.Destory(this._evictionKey, underTest, true);
                        this.DestroyedByEvictorCount.IncrementAndGet();
                    }
                    else
                    {
                        if (testWhileIdle)
                        {
                            bool active = false;
                            try
                            {
                                this.Factory.ActivateObject(this._evictionKey, underTest);
                                active = true;
                            }
                            catch (Exception e)
                            {
                                this.Logger.LogError(0, e, $"{this.Factory?.GetType().Name} invoke ActivateObject fault.");
                                this.Destory(this._evictionKey, underTest, true);
                                this.DestroyedByBorrowValidationCount.IncrementAndGet();
                            }
                            if (active)
                            {
                                if (!this.Factory.ValidateObject(this._evictionKey, underTest))
                                {
                                    this.Destory(this._evictionKey, underTest, true);
                                    this.DestroyedByEvictorCount.IncrementAndGet();
                                }
                                else
                                {
                                    try
                                    {
                                        this.Factory.PassivateObject(this._evictionKey, underTest);
                                    }
                                    catch (Exception e)
                                    {
                                        this.Logger.LogError(0, e, $"{this.Factory?.GetType().Name} invoke PassivateObject fault.");
                                        this.Destory(this._evictionKey, underTest, true);
                                        this.DestroyedByEvictorCount.IncrementAndGet();
                                    }
                                }
                            }
                        }
                        if (!underTest.EndEvictionTest(idleObjectsQueue))
                        {
                            // TODO - May need to add code here once additional
                            // states are used
                        }
                    }
                }
            }
            
        }

        public override void Evict()
        {
            try
            {
                this.EvictCore();
            }
            finally
            {
                if (this.Logger.IsEnabled(LogLevel.Debug))
                {
                    this.Logger.LogDebug($"pool status: actived {this.NumActive}, destroyed {this.DestroyedCount}, " +
                        $"created {this.CreatedCount}, returned {this.ReturnedCount}, borrowed {this.BorrowedCount}");
                }
            }
        }

        private IPooledObject<TValue> Create(TKey key)
        {
            int maxTotalPerKeySave = this.MaxTotalPerKey; // Per key
            int maxTotal = this.MaxTotal; // All keys

            // Check against the overall limit
            bool loop = true;

            while (loop)
            {
                int newNumTotal = this._numTotal.IncrementAndGet();
                if (maxTotal > -1 && newNumTotal > maxTotal)
                {
                    this._numTotal.DecrementAndGet();
                    if (this.NumIdle == 0)
                    {
                        return null;
                    }
                    else
                    {
                        this.ClearOldest();
                    }
                }
                else
                {
                    loop = false;
                }
            }

            ObjectDeque<TValue> objectDeque = this.poolMap[key];
            long newCreateCount = Interlocked.Increment(ref objectDeque.createCount);

            if (maxTotalPerKeySave > -1 && newCreateCount > maxTotalPerKeySave || newCreateCount > int.MaxValue)
            {
                this._numTotal.DecrementAndGet();
                Interlocked.Decrement(ref objectDeque.createCount);
                return null;
            }

            IPooledObject<TValue> p = null;
            try
            {
                p = this.Factory.MakeObject(key);
                this.Logger.LogDebug($"object was created at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            }
            catch (Exception ex)
            {
                this.Logger.LogError(0, ex, $"object was creating fault at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                this._numTotal.DecrementAndGet();
                Interlocked.Decrement(ref objectDeque.createCount);
                throw;
            }

            this.CreatedCount.IncrementAndGet();
            objectDeque.GetAllObjects().Add(new IdentityWrapper<TValue>(p.Object), p);
            return p;
        }

        /// <summary>
        ///     Destory the wrapped, pooled object.
        /// </summary>
        /// <param name="key">The key associated with the object to destroy.</param>
        /// <param name="toDestroy">The wrapped object to be destroyed</param>
        /// <param name="always">
        ///     Should the object be destroyed even if it is not currently in the set of idle objects for the
        ///     given key
        /// </param>
        /// <returns>if the object was destroyed, otherwise </returns>
        private bool Destory(TKey key, IPooledObject<TValue> toDestroy, bool always)
        {
            ObjectDeque<TValue> objectDeque = this.Register(key);

            try
            {
                bool isIdle = objectDeque.GetIdleObjects().Remove(toDestroy);

                if (isIdle || always)
                {
                    objectDeque.GetAllObjects().Remove(new IdentityWrapper<TValue>(toDestroy.Object));
                    toDestroy.Invalidate();

                    try
                    {
                        this.Factory.DestroyObject(key, toDestroy);
                        this.Logger.LogDebug($"object was destoried at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError(0, ex, $"destory object fault at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                        throw;
                    }
                    finally
                    {
                        Interlocked.Decrement(ref objectDeque.createCount);
                        //Interlocked.Increment(ref this._destroyedCount);
                        this.DestroyedCount.IncrementAndGet();
                        this._numTotal.DecrementAndGet();
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                this.Deregister(key);
            }
        }

        /// <summary>
        ///     De-register the use of a key by an object.
        ///     register() and deregister() must always be used as a pair.
        /// </summary>
        /// <param name="key">he key to de-register</param>
        private void Deregister(TKey key)
        {
            ObjectDeque<TValue> objectDeque;

            objectDeque = this.poolMap[key];

            long numInterested = Interlocked.Decrement(ref objectDeque.numInterested);
            if (numInterested == 0 && objectDeque.CreateCount == 0)
            {
                this._keyLock.TryEnterReadLock(-1);
                try
                {
                    if (objectDeque.CreateCount == 0 && objectDeque.NumInterested == 0)
                    {
                        // NOTE: Keys must always be removed from both poolMap and
                        //       poolKeyList at the same time while protected by
                        //       keyLock.writeLock()
                        this.poolMap.Remove(key);
                        this._poolKeyList.Remove(key);
                    }
                }
                finally
                {
                    if (_keyLock.IsReadLockHeld)
                        this._keyLock.ExitReadLock();
                }
            }
        }

        private ObjectDeque<TValue> Register(TKey key)
        {
            ObjectDeque<TValue> objectDeque = null;

            try
            {
                this._keyLock.TryEnterReadLock(-1);
                if (!this.poolMap.TryGetValue(key, out objectDeque))
                {
                    this._keyLock.ExitReadLock();
                    this._keyLock.TryEnterWriteLock(-1);
                    if (!this.poolMap.TryGetValue(key, out objectDeque))
                    {
                        objectDeque = new ObjectDeque<TValue>(this.BorrowStrategy);
                        Interlocked.Increment(ref objectDeque.numInterested);
                        this.poolMap.Add(key, objectDeque);
                        this._poolKeyList.Add(key);
                    }
                    else
                    {
                        Interlocked.Increment(ref objectDeque.numInterested);
                    }
                }
                else
                {
                    Interlocked.Increment(ref objectDeque.numInterested);
                }
            }
            finally
            {
                //this._keyLock.ExitReadLock();
                //this._keyLock.ExitWriteLock();
                if (this._keyLock.IsReadLockHeld)
                {
                    this._keyLock.ExitReadLock();
                }
                if (this._keyLock.IsWriteLockHeld)
                {
                    this._keyLock.ExitWriteLock();
                }
            }
            return objectDeque;
        }

        internal override void EnsureMinIdle()
        {
            if (this.MinIdlePerKey < 1)
            {
                return;
            }

            foreach (TKey k in this.poolMap.Keys)
            {
                this.EnsureMinIdle(k);
            }
        }

        /// <summary>
        ///     Ensure that the configured number of minimum idle objects is available in
        ///     the pool for the given key.
        /// </summary>
        /// <param name="k">The key to check for idle objects</param>
        private void EnsureMinIdle(TKey k)
        {
            ObjectDeque<TValue> objectDeque = this.poolMap[k];

            // objectDeque == null is OK here. It is handled correctly by both
            // methods called below.

            // this method isn't synchronized so the
            // calculateDeficit is done at the beginning
            // as a loop limit and a second time inside the loop
            // to stop when another thread already returned the
            // needed objects

            int deficit = this.CalculateDeficit(objectDeque);
            for (int i = 0; i < deficit && this.CalculateDeficit(objectDeque) > 0; i++)
            {
                this.AddObject(k);
            }
        }

        /// <summary>
        ///     Add an object to the set of idle objects for a given key.
        /// </summary>
        /// <param name="key">The key to associate with the idle object</param>
        /// <param name="p"> The wrapped object to add.</param>
        private void AddIdleObject(TKey key, IPooledObject<TValue> p)
        {
            if (p != null)
            {
                this.Factory.PassivateObject(key, p);
                BlockingList<IPooledObject<TValue>> idleObjects = this.poolMap[key].GetIdleObjects();

                idleObjects.AddFirst(p);
            }
        }

        /// <summary>
        ///     Registers a key for pool control and ensures that
        /// </summary>
        /// <param name="key"></param>
        public void PreparePool(TKey key)
        {
            if (this.MinIdlePerKey < 1)
            {
                return;
            }
            this.EnsureMinIdle(key);
        }

        /// <summary>
        ///     Calculate the number of objects that need to be created to attempt to
        ///     maintain the minimum number of idle objects while not exceeded the limits
        ///     on the maximum number of objects either per key or totally.
        /// </summary>
        /// <param name="objectDeque"></param>
        /// <returns></returns>
        private int CalculateDeficit(ObjectDeque<TValue> objectDeque)
        {
            if (objectDeque == null)
            {
                return this.MinIdlePerKey;
            }

            // Used more than once so keep a local copy so the value is consistent
            int maxTotal = this.MaxTotal;
            int maxTotalPerKeySave = this.MaxTotalPerKey;

            int objectDefecit = 0;

            // Calculate no of objects needed to be created, in order to have
            // the number of pooled objects < maxTotalPerKey();
            objectDefecit = this.MinIdlePerKey - objectDeque.GetIdleObjects().Size;
            if (maxTotalPerKeySave > 0)
            {
                int growLimit = Math.Max(
                    0,
                    maxTotalPerKeySave - objectDeque.GetIdleObjects().Size);
                objectDefecit = Math.Min(objectDefecit, growLimit);
            }

            // Take the maxTotal limit into account
            if (maxTotal > 0)
            {
                int growLimit = Math.Max(0, maxTotal - this.NumActive - this.NumIdle);
                objectDefecit = Math.Min(objectDefecit, growLimit);
            }

            return objectDefecit;
        }

        /// <summary>
        ///     Maintains information on the per key queue for a given key.
        /// </summary>
        /// <typeparam name="S"></typeparam>
        private class ObjectDeque<S>
        {
            private readonly ConcurrentDictionary<IdentityWrapper<S>, IPooledObject<S>> allObjects = new ConcurrentDictionary<IdentityWrapper<S>, IPooledObject<S>>();
            private readonly BlockingList<IPooledObject<S>> idleObjects;
            internal int createCount = 0;
            internal int numInterested = 0;

            public ObjectDeque(BorrowStrategy strategy)
            {
                this.idleObjects = new BlockingList<IPooledObject<S>>(strategy);
            }

            public int CreateCount => this.createCount;

            public long NumInterested => this.numInterested;

            public BlockingList<IPooledObject<S>> GetIdleObjects() => this.idleObjects;

            public IDictionary<IdentityWrapper<S>, IPooledObject<S>> GetAllObjects() => this.allObjects;
        }
    }
}