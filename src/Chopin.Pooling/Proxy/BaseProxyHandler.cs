// Copyright (c) labijie.com. All rights reserved.

using System;
using System.Reflection;

namespace Chopin.Pooling.Proxy
{
    class BaseProxyHandler<T>
    {
        private readonly IUsageTracking<T> _tracker;
        protected IUsageTracking<T> Tracker => _tracker;

        internal BaseProxyHandler(T pooledObject, IUsageTracking<T> tracker)
        {
            this.PooledObject = pooledObject;
            this._tracker = tracker;
        }

        internal T PooledObject { get; private set; }

        internal T DisableProxy()
        {
            T tmp = this.PooledObject;
            this.PooledObject = default(T);
            return tmp;
        }

        internal void ValidateProxiedObject()
        {
            if (this.PooledObject != null)
                return;
            throw new InvalidOperationException("This object may no longer be used as it has been returned to the Object Pool.");
        }

        internal virtual object Invoke(MethodInfo method, object[] args)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            this.ValidateProxiedObject();
            T @object = this.PooledObject;
            this._tracker?.Use(@object);
            return method.Invoke(@object, args);
        }
    }
}