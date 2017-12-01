// Copyright (c) labijie.com. All rights reserved.

using System.IO;

namespace Chopin.Pooling.Impl
{
    /// <summary>
    ///     Configuration settings for abandoned object removal.
    /// </summary>
    public class AbandonedConfig
    {
        /// <summary>
        ///     Flag to remove abandoned objects if they exceed the
        ///     removeAbandonedTimeout when borrowObject is invoked.
        ///     The default value is false
        ///     If set to true, abandoned objects are removed by borrowObject if
        ///     there are fewer than 2 idle objects available in the pool and
        ///     <code>getNumActive() > getMaxTotal() - 3</code>
        /// </summary>
        public bool RemoveAbandonedOnBorrow { get; set; } = false;

        /// <summary>
        ///     Flag to remove abandoned objects if they exceed the
        ///     removeAbandonedTimeout when pool maintenance (the "evictor") runs
        ///     The default value is false.
        ///     If set to true, abandoned objects are removed by the pool
        ///     maintenance thread when it runs.  This setting has no effect
        ///     unless maintenance is enabled by setting
        ///     {@link GenericObjectPool#getTimeBetweenEvictionRunsMillis() timeBetweenEvictionRunsMillis}
        ///     to a positive number.
        /// </summary>
        public bool RemoveAbandonedOnMaintenance { get; set; } = false;

        /// <summary>
        ///     Timeout in seconds before an abandoned object can be removed.
        ///     The time of most recent use of an object is the maximum (latest) of
        ///     {@link TrackedUse#getLastUsed()} (if this class of the object implements
        ///     TrackedUse) and the time when the object was borrowed from the pool.
        ///     The default value is 300 seconds.
        /// </summary>
        public int RemoveAbandonedTimeout { get; set; } = 300;

        /// <summary>
        ///     Flag to log stack traces for application code which abandoned
        ///     an object.
        ///     Defaults to false.
        ///     Logging of abandoned objects adds overhead for every object created
        ///     because a stack trace has to be generated.
        /// </summary>
        public bool LogAbandoned { get; set; } = false;

        /// <summary>
        ///     Returns the log writer being used by this configuration to log
        ///     information on abandoned objects. If not set, a PrintWriter based on
        ///     System.out with the system default encoding is used.
        /// </summary>
        public TextWriter LogWriter { get; set; }

        /// <summary>
        ///     If the pool implements {@link UsageTracking}, should the pool record a
        ///     stack trace every time a method is called on a pooled object and retain
        ///     the most recent stack trace to aid debugging of abandoned objects?
        ///     return <code>true</code> if usage tracking is enabled
        /// </summary>
        public bool UseUsageTracking { get; set; } = false;
    }
}