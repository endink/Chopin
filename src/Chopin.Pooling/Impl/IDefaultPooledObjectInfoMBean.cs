// Copyright (c) labijie.com. All rights reserved.

using System;

namespace Chopin.Pooling.Impl
{
    /// <summary>
    ///     /// The interface that defines the information about pooled objects that will be
    ///     exposed via JMX.
    ///     NOTE: This interface exists only to define those attributes and methods that
    ///     will be made available via JMX.It must not be implemented by clients
    ///     as it is subject to change between major, minor and patch version
    ///     releases of commons pool.Clients that implement this interface may
    ///     not, therefore, be able to upgrade to a new minor or patch release
    ///     without requiring code changes.
    /// </summary>
    public interface IDefaultPooledObjectInfoMBean
    {
        /// <summary>
        ///     Obtain the time (using the same basis as
        ///     {@link System#currentTimeMillis()}) that pooled object was created.
        ///     @return The creation time for the pooled object
        /// </summary>
        DateTime CreateTime { get; }

        /// <summary>
        ///     Obtain the time that pooled object was created.
        ///     @return The creation time for the pooled object formated as
        ///     <code>yyyy-MM-dd HH:mm:ss Z</code>
        /// </summary>
        string CreateTimeFormatted { get; }

        /// <summary>
        ///     Obtain the time (using the same basis as
        ///     {@link System#currentTimeMillis()}) the polled object was last borrowed.
        ///     @return The time the pooled object was last borrowed
        /// </summary>
        DateTime LastBorrowTime { get; }

        /// <summary>
        ///     Obtain the time that pooled object was last borrowed.
        ///     @return The last borrowed time for the pooled object formated as
        ///     <code>yyyy-MM-dd HH:mm:ss Z</code>
        /// </summary>
        string LastBorrowTimeFormatted { get; }

        /// <summary>
        ///     Obtain the stack trace recorded when the pooled object was last borrowed.
        ///     @return The stack trace showing which code last borrowed the pooled
        ///     object
        /// </summary>
        string LastBorrowTrace { get; }

        /// <summary>
        ///     Obtain the time (using the same basis as
        ///     {@link System#currentTimeMillis()})the wrapped object was last returned.
        ///     @return The time the object was last returned
        /// </summary>
        DateTime LastReturnTime { get; }

        /// <summary>
        ///     Obtain the time that pooled object was last returned.
        ///     @return The last returned time for the pooled object formated as
        ///     <code>yyyy-MM-dd HH:mm:ss Z</code>
        /// </summary>
        string LastReturnTimeFormatted { get; }

        /// <summary>
        ///     Obtain the name of the class of the pooled object.
        ///     @return The pooled object's class name
        ///     @see Class#getName()
        /// </summary>
        string PooledObjectType { get; }

        /// <summary>
        ///     Provides a String form of the wrapper for debug purposes. The format is
        ///     not fixed and may change at any time.
        ///     @return A string representation of the pooled object
        ///     @see Object#toString()
        /// </summary>
        string PooledObjectToString { get; }

        /// <summary>
        ///     Get the number of times this object has been borrowed.
        ///     @return The number of times this object has been borrowed.
        ///     @since 2.1
        /// </summary>
        long BorrowedCount { get; }
    }
}