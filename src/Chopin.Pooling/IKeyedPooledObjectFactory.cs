// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling
{
    public interface IKeyedPooledObjectFactory<TKey, TValue>
    {
        /**
    * Create an instance that can be served by the pool and
    * wrap it in a {@link PooledObject} to be managed by the pool.
    *
    * @param key the key used when constructing the object
    *
    * @return a {@code PooledObject} wrapping an instance that can
    * be served by the pool.
    *
    * @throws Exception if there is a problem creating a new instance,
    *    this will be propagated to the code requesting an object.
    */
        IPooledObject<TValue> MakeObject(TKey key);

        /**
         * Destroy an instance no longer needed by the pool.
         *
         * It is important for implementations of this method to be aware that there
         * is no guarantee about what state <code>obj</code> will be in and the
         * implementation should be prepared to handle unexpected errors.
         * 
         * Also, an implementation must take in to consideration that instances lost
         * to the garbage collector may never be destroyed.
         *
         * @param key the key used when selecting the instance
         * @param p a {@code PooledObject} wrapping the instance to be destroyed
         *
         * @throws Exception should be avoided as it may be swallowed by
         *    the pool implementation.
         *
         * @see #validateObject
         * @see KeyedObjectPool#invalidateObject
         */
        void DestroyObject(TKey key, IPooledObject<TValue> p);

        /**
         * Ensures that the instance is safe to be returned by the pool.
         *
         * @param key the key used when selecting the object
         * @param p a {@code PooledObject} wrapping the instance to be validated
         *
         * @return <code>false</code> if <code>obj</code> is not valid and should
         *         be dropped from the pool, <code>true</code> otherwise.
         */
        bool ValidateObject(TKey key, IPooledObject<TValue> p);

        /**
         * Reinitialize an instance to be returned by the pool.
         *
         * @param key the key used when selecting the object
         * @param p a {@code PooledObject} wrapping the instance to be activated
         *
         * @throws Exception if there is a problem activating <code>obj</code>,
         *    this exception may be swallowed by the pool.
         *
         * @see #destroyObject
         */
        void ActivateObject(TKey key, IPooledObject<TValue> p);

        /**
         * Uninitialize an instance to be returned to the idle object pool.
         *
         * @param key the key used when selecting the object
         * @param p a {@code PooledObject} wrapping the instance to be passivated
         *
         * @throws Exception if there is a problem passivating <code>obj</code>,
         *    this exception may be swallowed by the pool.
         *
         * @see #destroyObject
         */
        void PassivateObject(TKey key, IPooledObject<TValue> p);
    }
}