// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling
{
    /// <summary>
    ///     A base implementation of <code>KeyedPooledObjectFactory</code>.
    ///     All operations defined here are essentially no-op's.
    ///     This class is immutable, and therefore thread-safe.
    ///     <see cref="IKeyedPooledObjectFactory{TKey,TValue}" />
    /// </summary>
    /// <typeparam name="TKey">The type of keys managed by this factory.</typeparam>
    /// <typeparam name="TValue">Type of element managed by this factory.</typeparam>
    public abstract class BaseKeyedPooledObjectFactory<TKey, TValue> : IKeyedPooledObjectFactory<TKey, TValue>
    {
        public virtual IPooledObject<TValue> MakeObject(TKey key)
        {
            return this.Wrap(this.Create(key));
        }

        /// <summary>
        ///     Destroy an instance no longer needed by the pool.
        ///     The default implementation is a no-op.
        /// </summary>
        /// @param key the key used when selecting the instance
        /// @param p a {@code PooledObject} wrapping the the instance to be destroyed
        ////
        public virtual void DestroyObject(TKey key, IPooledObject<TValue> p)
        {
        }

        /// <summary>
        ///     Ensures that the instance is safe to be returned by the pool.
        ///     The default implementation always returns {@code true}.
        /// </summary>
        /// @param key the key used when selecting the object
        /// @param p a {@code PooledObject} wrapping the the instance to be validated
        /// @return always
        /// <code>true</code>
        /// in the default implementation
        ////
        public virtual bool ValidateObject(TKey key, IPooledObject<TValue> p)
        {
            return true;
        }

        /// <summary>
        ///     Reinitialize an instance to be returned by the pool.
        ///     The default implementation is a no-op.
        /// </summary>
        /// @param key the key used when selecting the object
        /// @param p a {@code PooledObject} wrapping the the instance to be activated
        ////
        public virtual void ActivateObject(TKey key, IPooledObject<TValue> p)
        {
        }

        /// <summary>
        ///     Uninitialize an instance to be returned to the idle object pool.
        ///     The default implementation is a no-op.
        /// </summary>
        /// @param key the key used when selecting the object
        /// @param p a {@code PooledObject} wrapping the the instance to be passivated
        ////
        public virtual void PassivateObject(TKey key, IPooledObject<TValue> p)
        {
        }

        /// <summary>
        ///     Create an instance that can be served by the pool.
        /// </summary>
        /// @param key the key used when constructing the object
        /// @return an instance that can be served by the pool
        /// 
        /// @throws Exception if there is a problem creating a new instance,
        /// this will be propagated to the code requesting an object.
        ////
        public abstract TValue Create(TKey key);

        /// <summary>
        ///     Wrap the provided instance with an implementation of
        ///     {@link PooledObject}.
        /// </summary>
        /// @param value the instance to wrap
        /// 
        /// @return The provided instance, wrapped by a {@link PooledObject}
        ////
        public abstract IPooledObject<TValue> Wrap(TValue value);
    }
}