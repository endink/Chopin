// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling.Impl
{
    public class GenericKeyedObjectPoolConfig : BaseObjectPoolConfig
    {
        /// <summary>
        ///     The default value for the {@code maxTotalPerKey} configuration attribute.
        /// </summary>
        public const int DEFAULT_MAX_TOTAL_PER_KEY = 8;

        /// <summary>
        ///     The default value for the {@code minIdlePerKey} configuration attribute.
        /// </summary>
        public const int DEFAULT_MIN_IDLE_PER_KEY = 0;

        /// <summary>
        ///     The default value for the {@code maxIdlePerKey} configuration attribute.
        /// </summary>
        public const int DEFAULT_MAX_IDLE_PER_KEY = 8;

        /// <summary>
        ///     Get the value for the {@code maxTotalPerKey} configuration attribute
        ///     for pools created with this configuration instance.
        /// </summary>
        public int MaxTotalPerKey { get; set; } = DEFAULT_MAX_TOTAL_PER_KEY;

        /// <summary>
        ///     Get the value for the {@code minIdlePerKey} configuration attribute
        ///     for pools created with this configuration instance.
        /// </summary>
        public int MinIdlePerKey { get; set; } = DEFAULT_MIN_IDLE_PER_KEY;

        /// <summary>
        ///     Get the value for the {@code maxIdlePerKey} configuration attribute
        ///     for pools created with this configuration instance.
        /// </summary>
        public int MaxIdlePerKey { get; set; } = DEFAULT_MAX_IDLE_PER_KEY;
    }
}