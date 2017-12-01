// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling
{
    /// <summary>
    ///     This interface may be implemented by an object pool to enable clients
    ///     (primarily those clients that wrap pools to provide pools with extended
    ///     features) to provide additional information to the pool relating to object
    ///     using allowing more informed decisions and reporting to be made regarding
    ///     abandoned objects.
    /// </summary>
    /// <typeparam name="T">The type of object provided by the pool.</typeparam>
    public interface IUsageTracking<T>
    {
        /// <summary>
        ///     his method is called every time a pooled object to enable the pool to
        ///     better track borrowed objects.
        /// </summary>
        /// <param name="pooledObject">The object that is being used</param>
        void Use(T pooledObject);
    }
}