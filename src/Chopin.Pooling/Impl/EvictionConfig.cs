// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling.Impl
{
    public class EvictionConfig
    {
        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        public EvictionConfig(long idleEvictTime, long idleSoftEvictTime, int minIdle)
        {
            this.IdleEvictTime = idleEvictTime > 0 ? idleEvictTime : long.MaxValue;
            this.IdleSoftEvictTime = idleSoftEvictTime > 0 ? idleSoftEvictTime : long.MaxValue;
            this.MinIdle = minIdle;
        }

        /// <summary>
        ///     Obtain the {@code idleEvictTime} for this eviction configuration
        ///     instance.
        ///     How the evictor behaves based on this value will be determined by the
        ///     configured {@link EvictionPolicy}.
        /// </summary>
        public long IdleEvictTime { get; }

        /// <summary>
        ///     /// Obtain the {@code idleSoftEvictTime} for this eviction configuration
        ///     instance.
        ///     How the evictor behaves based on this value will be determined by the
        ///     configured {@link EvictionPolicy}.
        /// </summary>
        public long IdleSoftEvictTime { get; }

        /// <summary>
        ///     Obtain the {@code minIdle} for this eviction configuration instance.
        ///     How the evictor behaves based on this value will be determined by the
        ///     configured {@link EvictionPolicy}.
        /// </summary>
        public int MinIdle { get; }
    }
}