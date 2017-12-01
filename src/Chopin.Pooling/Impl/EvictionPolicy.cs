// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling.Impl
{
    /// <summary>
    ///     To provide a custom eviction policy (i.e. something other than {@link
    ///     DefaultEvictionPolicy} for a pool, users must provide an implementation of
    ///     this interface that provides the required eviction policy.
    /// </summary>
    /// <typeparam name="T">the type of objects in the pool</typeparam>
    public interface IEvictionPolicy<T>
    {
        /// <summary>
        ///     This method is called to test if an idle object in the pool should be
        ///     evicted or not.
        /// </summary>
        /// <param name="config">The pool configuration settings related to eviction</param>
        /// <param name="underTest">The pooled object being tested for eviction</param>
        /// <param name="idleCount">The current number of idle objects in the pool including the object under test</param>
        /// <returns></returns>
        bool Evict(EvictionConfig config, IPooledObject<T> underTest, int idleCount);
    }
}