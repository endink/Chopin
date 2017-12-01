// Copyright (c) labijie.com. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Chopin.Pooling.Collections;
using Chopin.Pooling.Impl.Atom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chopin.Pooling.Impl
{
    public abstract class BaseGenericObjectPool<T> : IDisposable
    {
        public const int MeanTimingStatsCacheSize = 100;
        protected readonly object _evictionLock = new object();
        private readonly StatsStore activeTimes = new StatsStore(MeanTimingStatsCacheSize);
        private readonly StatsStore idleTimes = new StatsStore(MeanTimingStatsCacheSize);
        private readonly StatsStore waitTimes = new StatsStore(MeanTimingStatsCacheSize);
        internal long _activeTimes;

        private BaseObjectPoolConfig _baseObjectPoolConfig;
        private long _borrowedCount;
        protected volatile bool _closed = false;
        private AtomLong _createdCount = 0;
        private AtomLong _destroyedByEvictorCount =0;
        private AtomLong _destroyedCount = 0;
        internal IEvictionPolicy<T> _evictionPolicy;
        private AtomLong _maxBorrowWaitTimeMillis=0;
        private AtomLong _returnedCount = 0;
        private AtomLong destroyedByBorrowValidationCount = 0;
        internal EvictionIterator<IPooledObject<T>> evictionIterator = null;
        private Evictor evictor = null;
        private long maxBorrowWaitTimeMillis = 0L;
        private ILogger _logger;
        private IEvictionTimer _timer;
        private volatile bool _disposed;

        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        protected BaseGenericObjectPool(BaseObjectPoolConfig baseObjectPoolConfig, IEvictionTimer timer = null, ILoggerFactory loggerFactory=null)
        {
            this._logger = loggerFactory?.CreateLogger(this.GetType().FullName) ?? NullLogger.Instance;
            this._timer = timer ?? new EvictionTimer();

            this._baseObjectPoolConfig = baseObjectPoolConfig ?? new BaseObjectPoolConfig();
            this.CreationStackTrace = this.GetStackTrace(new Exception());
        }

        protected ILogger Logger => _logger;

        /// <summary>
        ///     Returns the maximum number of objects that can be allocated by the pool
        ///     (checked out to clients, or idle awaiting checkout) at a given time. When
        ///     negative, there is no limit to the number of objects that can be
        ///     managed by the pool at one time.
        /// </summary>
        public int MaxTotal
        {
            get { return _baseObjectPoolConfig.MaxTotal; }
            set { _baseObjectPoolConfig.MaxTotal = value; }
        }

        /// <summary>
        ///     Returns whether to block when the <code>borrowObject()</code> method is
        ///     invoked when the pool is exhausted (the maximum number of "active"
        ///     objects has been reached).
        /// </summary>
        public bool BlockWhenExhausted
        {
            get { return _baseObjectPoolConfig.BlockWhenExhausted; }
            set { _baseObjectPoolConfig.BlockWhenExhausted = value; }
        }

        /// <summary>
        ///     Returns the maximum amount of time (in milliseconds) the
        ///     <code>borrowObject()</code> method should block before throwing an
        ///     exception when the pool is exhausted and
        ///     {@link #getBlockWhenExhausted} is true. When less than 0, the
        ///     <code>borrowObject()</code> method may block indefinitely.
        /// </summary>
        public long MaxWaitMillis
        {
            get { return _baseObjectPoolConfig.MaxWaitMillis; }
            set { _baseObjectPoolConfig.MaxWaitMillis = value; }
        }

        /// <summary>
        ///     Returns whether the pool has LIFO (last in, first out) behaviour with
        ///     respect to idle objects - always returning the most recently used object
        ///     from the pool, or as a FIFO (first in, first out) queue, where the pool
        ///     always returns the oldest object in the idle object pool.
        /// </summary>
        public BorrowStrategy BorrowStrategy
        {
            get { return this._baseObjectPoolConfig.BorrowStrategy; }
        }

        /// <summary>
        ///     Returns whether objects created for the pool will be validated before
        ///     being returned from the <code>borrowObject()</code> method. Validation is
        ///     performed by the <code>validateObject()</code> method of the factory
        ///     associated with the pool. If the object fails to validate, then
        ///     <code>borrowObject()</code> will fail.
        /// </summary>
        public bool TestOnCreate
        {
            get { return _baseObjectPoolConfig.TestOnCreate; }
            set { _baseObjectPoolConfig.TestOnCreate = value; }
        }

        /// <summary>
        ///     Returns whether objects borrowed from the pool will be validated before
        ///     being returned from the<code>borrowObject()</code> method.Validation is
        ///     performed by the<code> validateObject()</code> method of the factory
        ///     associated with the pool. If the object fails to validate, it will be
        ///     removed from the pool and destroyed, and a new attempt will be made to
        ///     borrow an object from the pool.
        /// </summary>
        public bool TestOnBorrow
        {
            get { return _baseObjectPoolConfig.TestOnBorrow; }
            set { _baseObjectPoolConfig.TestOnBorrow = value; }
        }

        /// <summary>
        ///     Returns whether objects borrowed from the pool will be validated when
        ///     they are returned to the pool via the<code>returnObject()</code> method.
        ///     Validation is performed by the<code> validateObject()</code> method of
        ///     the factory associated with the pool. Returning objects that fail validation
        ///     are destroyed rather then being returned the pool.
        /// </summary>
        public bool TestOnReturn
        {
            get { return _baseObjectPoolConfig.TestOnReturn; }
            set { _baseObjectPoolConfig.TestOnReturn = value; }
        }

        /// <summary>
        ///     /// Returns whether objects sitting idle in the pool will be validated by the
        ///     idle object evictor(if any - see
        ///     {
        ///     @link #setTimeBetweenEvictionRunsMillis(long)}). Validation is performed
        ///     by the<code> validateObject()</code> method of the factory associated
        ///     with the pool. If the object fails to validate, it will be removed from
        ///     the pool and destroyed.
        /// </summary>
        public bool TestWhileIdle
        {
            get { return _baseObjectPoolConfig.TestWhileIdle; }
            set { _baseObjectPoolConfig.TestWhileIdle = value; }
        }

        /// <summary>
        ///     /// Returns the number of milliseconds to sleep between runs of the idle
        ///     object evictor thread.When non-positive, no idle object evictor thread
        ///     will be run.
        /// </summary>
        public long TimeBetweenEvictionRunsMillis
        {
            get { return _baseObjectPoolConfig.TimeBetweenEvictionRunsMillis; }
            set { _baseObjectPoolConfig.TimeBetweenEvictionRunsMillis = value; }
        }

        /// <summary>
        ///     /// Returns the maximum number of objects to examine during each run (if any)
        ///     of the idle object evictor thread.When positive, the number of tests
        ///     performed for a run will be the minimum of the configured value and the
        ///     number of idle instances in the pool.When negative, the number of tests
        ///     performed will be
        ///     <code> ceil({
        /// @link #getNumIdle}/
        ///  abs({
        /// @link #getNumTestsPerEvictionRun}))</code>
        ///     which means that when the
        ///     value is <code> -n </code> roughly one nth of the idle objects will be
        ///     tested per run.
        /// </summary>
        public int NumTestsPerEvictionRun
        {
            get { return _baseObjectPoolConfig.NumTestsPerEvictionRun; }
            set { _baseObjectPoolConfig.NumTestsPerEvictionRun = value; }
        }

        /// <summary>
        ///     * Returns the minimum amount of time an object may sit idle in the pool
        ///     before it is eligible for eviction by the idle object evictor(if any -
        ///     see {
        ///     @link #setTimeBetweenEvictionRunsMillis(long)}). When non-positive,
        ///     no objects will be evicted from the pool due to idle time alone.
        /// </summary>
        public long MinEvictableIdleTimeMillis
        {
            get { return _baseObjectPoolConfig.MinEvictableIdleTimeMillis; }
            set { _baseObjectPoolConfig.MinEvictableIdleTimeMillis = value; }
        }

        /// <summary>
        ///     * Returns the minimum amount of time an object may sit idle in the pool
        ///     before it is eligible for eviction by the idle object evictor(if any -
        ///     see { @link #setTimeBetweenEvictionRunsMillis(long)}),
        ///     with the extra condition that at least<code>minIdle</code> object
        ///     instances remain in the pool. This setting is overridden by
        ///     {@link #getMinEvictableIdleTimeMillis} (that is, if
        ///     {@link #getMinEvictableIdleTimeMillis} is positive, then
        ///     {@link #getSoftMinEvictableIdleTimeMillis} is ignored).
        /// </summary>
        public long SoftMinEvictableIdleTimeMillis
        {
            get { return _baseObjectPoolConfig.SoftMinEvictableIdleTimeMillis; }
            set { _baseObjectPoolConfig.SoftMinEvictableIdleTimeMillis = value; }
        }

        protected bool IsDisposed => _disposed;

        /// <summary>
        ///     Returns the name of the {@link EvictionPolicy} implementation that is
        ///     used by this pool.
        /// </summary>
        public string EvictionPolicyClassName { get; set; }

        /// <summary>
        ///     Has this pool instance been closed.
        /// </summary>
        public bool IsClosed => this._closed;

        public ISwallowedExceptionListener SwallowedExceptionListener { get; set; }

        /// <summary>
        ///     Returns the {@link EvictionPolicy} defined for this pool.
        /// </summary>
        /// <returns></returns>
        protected IEvictionPolicy<T> EvictionPolicy => this._evictionPolicy;

        public string CreationStackTrace { get; }

        public long BorrowedCount => this._borrowedCount;

        public AtomLong ReturnedCount =>  this._returnedCount;

        public AtomLong CreatedCount => this._createdCount;

        public AtomLong DestroyedCount => this._destroyedCount;

        public AtomLong DestroyedByEvictorCount => this._destroyedByEvictorCount;

        public long MeanActiveTimeMillis => Interlocked.Read(ref this._activeTimes);

        public AtomLong MeanIdleTimeMillis => this._maxBorrowWaitTimeMillis;

        public abstract int NumIdle { get; }
        public long MaxBorrowWaitTimeMillis { get => maxBorrowWaitTimeMillis; set => maxBorrowWaitTimeMillis = value; }

        /// <summary>
        /// The mean time threads wait to borrow an object based on the last {@link MEAN_TIMING_STATS_CACHE_SIZE} objects borrowed from the pool. 
        /// @return mean time in milliseconds that a recently served thread has had to wait to borrow an object from the pool
        /// </summary>
        public long MeanBorrowWaitTimeMillis => waitTimes.GetMean();


        /// <summary>
        ///     See {@link GenericKeyedObjectPool#getDestroyedByBorrowValidationCount()}
        ///     @return See {@link GenericKeyedObjectPool#getDestroyedByBorrowValidationCount()}
        /// </summary>
        public AtomLong DestroyedByBorrowValidationCount =>this.destroyedByBorrowValidationCount;

        /// <summary>
        ///     Closes the pool, destroys the remaining idle objects
        /// </summary>
        public abstract void Close();

        /// <summary>
        ///     Perform <code>numTests</code> idle object eviction tests, evicting
        ///     examined objects that meet the criteria for eviction.If
        ///     <code>testWhileIdle</code> is true, examined objects are validated
        ///     when visited(and removed if invalid); otherwise only objects that
        ///     have been idle for more than<code>minEvicableIdleTimeMillis</code>
        ///     are removed.
        /// </summary>
        public abstract void Evict();

        /// <summary>
        ///     Verifies that the pool is open.
        /// </summary>
        internal void AssertOpen()
        {
            if (this.IsClosed)
            {
                throw new ArgumentException("Pool not open");
            }
        }

        /// <summary>
        ///     Starts the evictor with the given delay. If there is an evictor
        ///     running when this method is called, it is stopped and replaced with a
        ///     new evictor with the specified delay.
        ///     This method needs to be final, since it is called from a constructor.
        ///     See POOL-195.
        /// </summary>
        /// <param name="delay">time in milliseconds before start and between eviction runs</param>
        internal void StartEvictor(long delay)
        {
            lock (this._evictionLock)
            {
                if (null != this.evictor)
                {
                    this._timer.Cancel(this.evictor);
                    this.evictor = null;
                    this.evictionIterator = null;
                }
                if (delay > 0)
                {
                    this.evictor = new Evictor(
                        () =>
                        {
                            try
                            {
                                this.Evict();
                            }
                            catch (Exception e)
                            {
                                this.Logger.LogError(0, e, "evictor scheduling fault.");
                                this.SwallowException(e);
                            }
                        });
                    this._timer.Schedule(this.evictor, TimeSpan.FromMilliseconds(delay), TimeSpan.FromMilliseconds(delay));
                }
            }
        }

        /// <summary>
        ///     Tries to ensure that the configured minimum number of idle instances are
        ///     available in the pool.
        /// </summary>
        internal abstract void EnsureMinIdle();

        protected void SwallowException(Exception exception)
        {
            ISwallowedExceptionListener listener = this.SwallowedExceptionListener;
            
            try
            {
                listener?.OnSwallowException(exception);
            }
            catch (Exception ex)
            {
                PoolUtils.CheckRethrow(ex);
            }
        }

        private string GetStackTrace(Exception e)
        {
            return e.StackTrace;
        }

        internal void UpdateStatsBorrow(IPooledObject<T> p, long waitTime)
        {
            Interlocked.Increment(ref this._borrowedCount);
            this.idleTimes.Add(p.IdleTimeMillis);
            this.waitTimes.Add(waitTime);

            long currentMax;
            do
            {
                currentMax = this._maxBorrowWaitTimeMillis.Value;
                if (currentMax >= waitTime)
                {
                    break;
                }
            }
            while (InterLockedEx.CompareAndSet(ref this.maxBorrowWaitTimeMillis, currentMax, waitTime));
        }

        protected void UpdateStatsReturn(long activeTime)
        {
            this._returnedCount.IncrementAndGet();
            this.activeTimes.Add(activeTime);
        }

        ~BaseGenericObjectPool()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DisposeManagedResources()
        {

        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    _timer?.Dispose();
                    _timer = null;
                    this.DisposeManagedResources();
                }
            }
        }

        private class StatsStore
        {
            private readonly int size;
            private readonly long[] values;
            private int index;

            /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
            public StatsStore(int size)
            {
                this.size = size;
                this.values = new long[size];
                for (int i = 0; i < size; i++)
                {
                    this.values[i] = -1;
                }
            }

            public void Add(long value)
            {
                lock (typeof(StatsStore))
                {
                    this.values[this.index] = value;
                    this.index++;
                    if (this.index == this.size)
                    {
                        this.index = 0;
                    }
                }
            }

            /// <summary>
            ///     Returns the mean of the cached values.
            /// </summary>
            /// <returns></returns>
            public long GetMean()
            {
                double result = 0;
                int counter = 0;
                for (int i = 0; i < this.size; i++)
                {
                    long value = this.values[i];
                    if (value != -1)
                    {
                        counter++;
                        result = result * ((counter - 1) / (double)counter) +
                            value / (double)counter;
                    }
                }
                return (long)result;
            }
        }
    }

    internal class IdentityWrapper<T>
    {
        private readonly T instance;

        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        public IdentityWrapper(T instance)
        {
            this.instance = instance;
        }

        /// <summary>确定指定的对象是否等于当前对象。</summary>
        /// <returns>如果指定的对象等于当前的对象，则为 true；否则为 false。</returns>
        /// <param name="obj">将与当前对象进行比较的对象。 </param>
        public override bool Equals(object obj)
        {
            return ((IdentityWrapper<T>)obj).instance.Equals(this.instance);
        }

        /// <summary>作为默认哈希函数。</summary>
        /// <returns>当前对象的哈希代码。</returns>
        public override int GetHashCode()
        {
            return this.instance.GetHashCode();
        }
    }

    class EvictionIterator<T> : IEnumerable<T>
    {

        public EvictionIterator(BlockingList<T> idleObjects)
        {
            this.IdleObjects = idleObjects ?? throw new ArgumentNullException("blocking list cant not be null", nameof(idleObjects));
        }

        /// <summary>返回一个循环访问集合的枚举器。</summary>
        /// <returns>可用于循环访问集合的 <see cref="T:System.Collections.Generic.IEnumerator`1" />。</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.IdleObjects.ToArray()).GetEnumerator();
        }

        /// <summary>返回一个循环访问集合的枚举器。</summary>
        /// <returns>可用于循环访问集合的 <see cref="T:System.Collections.IEnumerator" /> 对象。</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this.IdleObjects.ToArray()).GetEnumerator();
        }

        public BlockingList<T> IdleObjects { get; }
    }
}