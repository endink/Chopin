// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling.Impl
{
    /// <summary>
    ///     Provides the default implementation of {@link EvictionPolicy} used by the
    ///     pools. Objects will be evicted if the following conditions are met:
    ///     This class is immutable and thread-safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultEvictionPolicy<T> : IEvictionPolicy<T>
    {
        public bool Evict(EvictionConfig config, IPooledObject<T> underTest, int idleCount)
        {
            if ((config.IdleSoftEvictTime < underTest.IdleTimeMillis &&
                    config.MinIdle < idleCount) ||
                config.IdleEvictTime < underTest.IdleTimeMillis)
            {
                return true;
            }
            return false;
        }
    }
}