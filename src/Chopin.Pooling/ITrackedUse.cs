// Copyright (c) labijie.com. All rights reserved.

using System;

namespace Chopin.Pooling
{
    /// <summary>
    ///     This interface allows pooled objects to make information available about when
    ///     and how they were used available to the object pool.The object pool may, but
    ///     is not required, to use this information to make more informed decisions when
    ///     determining the state of a pooled object - for instance whether or not the
    ///     object has been abandoned.
    /// </summary>
    public interface ITrackedUse
    {
        /// <summary>
        ///     Get the last time this object was used.
        /// </summary>
        /// <returns></returns>
        DateTime LastUsed { get; }
    }
}