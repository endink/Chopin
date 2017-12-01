// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling
{
    public abstract class BasePooledObjectFactory<T> : IPooledObjectFactory<T>
    {
        public virtual IPooledObject<T> MakeObject()
        {
            return this.Wrap(this.Create());
        }

        public virtual void DestroyObject(IPooledObject<T> @object)
        {
        }

        public bool ValidateObject(IPooledObject<T> @object)
        {
            return true;
        }

        public void ActivateObject(IPooledObject<T> @object)
        {
        }

        public void PassivateObject(IPooledObject<T> @object)
        {
        }

        /// <summary>
        ///     Creates an object instance, to be wrapped in a <see cref="IPooledObject{T}" />
        ///     This method must support concurrent, multi-threaded
        ///     activation
        /// </summary>
        /// <returns>an instance to be served by the pool</returns>
        public abstract T Create();

        /// <summary>
        ///     Wrap the provided instance with an implementation of
        /// </summary>
        /// <param name="obj">the instance to wrap</param>
        /// <returns>he provided instance, wrapped by a {@link PooledObject}</returns>
        public abstract IPooledObject<T> Wrap(T obj);
    }
}