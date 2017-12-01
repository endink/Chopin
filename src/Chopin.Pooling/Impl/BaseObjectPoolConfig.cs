// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling.Impl
{
    public class BaseObjectPoolConfig
    {
        /// <summary>
        ///     The default value for the {@code blockWhenExhausted} configuration
        /// </summary>
        public const bool DefaultBlockWhenExhausted = true;

        /// <summary>
        ///     The default value for the {@code maxWait} configuration attribute.
        /// </summary>
        public const long DefaultMaxWaitMillis = -1L;

        public const BorrowStrategy DefaultBorrowStrategy = BorrowStrategy.LIFO;
        public static long DefaultSoftMinEvictableIdleTimeMillis = -1;
        public static long DefaultMinEvictableIdleTimeMillis = 1000L * 60L * 30L;
        public static int DefaultNumTestsPerEvictionRun = 3;
        public static long DefaultTimeBetweenEvictionRunsMillis = -1L;
        public static bool DefaultTestWhileIdle = false;
        public static bool DefaultTestOnReturn = false;
        public static bool DefaultTestOnBorrow = false;
        public static bool DefaultTestOnCreate = false;


        /// <summary>
        ///     The default value for the {@code maxTotal} configuration attribute.
        /// </summary>
        public const int DEFAULT_MAX_TOTAL = -1;

        /// <summary>
        ///     Get the value for the {@code maxTotal} configuration attribute
        ///     for pools created with this configuration instance.
        /// </summary>
        public int MaxTotal { get; set; } = DEFAULT_MAX_TOTAL;

        /// <summary>
        ///     Get the value for the <see cref="BorrowStrategy"/> configuration attribute for pools
        ///     created with this configuration instance.
        /// </summary>
        public BorrowStrategy BorrowStrategy { get; set; } = DefaultBorrowStrategy;

        ///// <summary>
        ///// Get the value for the {@code fairness} configuration attribute for pools created with this configuration instance.
        ///// The current setting of {@code fairness} for this configuration instance
        ///// </summary>
        //public bool Fairness { get; set; } = false;

        /// <summary>
        ///     Get the value for the {@code maxWait} configuration attribute for pools
        ///     created with this configuration instance.
        /// </summary>
        public long MaxWaitMillis { get; set; } = DefaultMaxWaitMillis;

        /// <summary>
        ///     Get the value for the {@code minEvictableIdleTimeMillis} configuration
        ///     attribute for pools created with this configuration instance.
        /// </summary>
        public long MinEvictableIdleTimeMillis { get; set; } = DefaultMinEvictableIdleTimeMillis;

        /// <summary>
        ///     Get the value for the {@code softMinEvictableIdleTimeMillis}
        ///     configuration attribute for pools created with this configuration
        ///     instance.
        /// </summary>
        public long SoftMinEvictableIdleTimeMillis { get; set; } = DefaultSoftMinEvictableIdleTimeMillis;

        /// <summary>
        ///     Get the value for the {@code numTestsPerEvictionRun} configuration
        ///     attribute for pools created with this configuration instance.
        /// </summary>
        public int NumTestsPerEvictionRun { get; set; } = DefaultNumTestsPerEvictionRun;

        /// <summary>
        ///     Get the value for the {@code testOnCreate} configuration attribute for
        ///     pools created with this configuration instance.
        /// </summary>
        public bool TestOnCreate { get; set; } = DefaultTestOnCreate;

        /// <summary>
        ///     Get the value for the {@code testOnBorrow} configuration attribute for
        ///     pools created with this configuration instance.
        /// </summary>
        public bool TestOnBorrow { get; set; } = DefaultTestOnBorrow;

        /// <summary>
        ///     Get the value for the {@code testOnReturn} configuration attribute for
        ///     pools created with this configuration instance.
        /// </summary>
        public bool TestOnReturn { get; set; } = DefaultTestOnReturn;

        /// <summary>
        ///     Get the value for the {@code testWhileIdle} configuration attribute for
        ///     pools created with this configuration instance.
        /// </summary>
        public bool TestWhileIdle { get; set; } = DefaultTestWhileIdle;

        /// <summary>
        ///     Get the value for the {@code timeBetweenEvictionRunsMillis} configuration
        ///     attribute for pools created with this configuration instance.
        /// </summary>
        public long TimeBetweenEvictionRunsMillis { get; set; } = DefaultTimeBetweenEvictionRunsMillis;

        /// <summary>
        ///     Get the value for the {@code evictionPolicyClassName} configuration
        ///     attribute for pools created with this configuration instance.
        /// </summary>
        public string EvictionPolicyClassName { get; set; }

        /// <summary>
        ///     Get the value for the {@code blockWhenExhausted} configuration attribute
        ///     for pools created with this configuration instance.
        /// </summary>
        public bool BlockWhenExhausted { get; set; } = DefaultBlockWhenExhausted;

    }
}