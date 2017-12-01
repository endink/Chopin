// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling.Proxy
{
    interface ProxyCreator<T>
    {
        T CreateProxy(T pooledObject, IUsageTracking<T> tracker);

        T ResolveProxy(T proxy);
    }
}