// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling
{
    public interface IPooledObjectFactory<T>
    {
        IPooledObject<T> MakeObject();

        void DestroyObject(IPooledObject<T> @object);

        bool ValidateObject(IPooledObject<T> @object);

        void ActivateObject(IPooledObject<T> @object);

        void PassivateObject(IPooledObject<T> @object);
    }
}