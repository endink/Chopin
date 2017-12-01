// Copyright (c) labijie.com. All rights reserved.

using System;

namespace Chopin.Pooling
{
    /// <summary>
    ///     Pools that unavoidably swallow exceptions may be configured with an instance
    ///     of this listener so the user may receive notification of when this happens.
    ///     The listener should not throw an exception when called but pools calling
    ///     listeners should protect themselves against exceptions anyway.
    /// </summary>
    public interface ISwallowedExceptionListener
    {
        /// <summary>
        ///     This method is called every time the implementation unavoidably swallows
        ///     an exception.
        /// </summary>
        /// <param name="e">The exception that was swallowed</param>
        void OnSwallowException(Exception e);
    }
}