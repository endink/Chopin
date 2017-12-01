// Copyright (c) labijie.com. All rights reserved.

using System;
using System.IO;

namespace Chopin.Pooling.Impl
{
    public class DefaultPooledObjectInfo<T> : IDefaultPooledObjectInfoMBean
    {
        private readonly IPooledObject<T> _pooledObject;

        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        public DefaultPooledObjectInfo(IPooledObject<T> pooledObject)
        {
            this._pooledObject = pooledObject;
        }

        /// <summary>
        ///     Obtain the time (using the same basis as
        ///     {@link System#currentTimeMillis()}) that pooled object was created.
        ///     @return The creation time for the pooled object
        /// </summary>
        public DateTime CreateTime => this._pooledObject.CreateTime;

        /// <summary>
        ///     Obtain the time that pooled object was created.
        ///     @return The creation time for the pooled object formated as
        ///     <code>yyyy-MM-dd HH:mm:ss Z</code>
        /// </summary>
        public string CreateTimeFormatted => this._pooledObject.CreateTime.ToString("yyyy-MM-dd HH:mm:ss Z");

        /// <summary>
        ///     Obtain the time (using the same basis as
        ///     {@link System#currentTimeMillis()}) the polled object was last borrowed.
        ///     @return The time the pooled object was last borrowed
        /// </summary>
        public DateTime LastBorrowTime => this._pooledObject.LastBorrowTime;

        /// <summary>
        ///     Obtain the time that pooled object was last borrowed.
        ///     @return The last borrowed time for the pooled object formated as
        ///     <code>yyyy-MM-dd HH:mm:ss Z</code>
        /// </summary>
        public string LastBorrowTimeFormatted => this._pooledObject.LastBorrowTime.ToString("yyyy-MM-dd HH:mm:ss Z");

        /// <summary>
        ///     Obtain the stack trace recorded when the pooled object was last borrowed.
        ///     @return The stack trace showing which code last borrowed the pooled
        ///     object
        /// </summary>
        public string LastBorrowTrace
        {
            get
            {
                var wr = new StringWriter();
                this._pooledObject.PrintStackTrace(wr);
                return wr.ToString();
            }
        }

        /// <summary>
        ///     Obtain the time (using the same basis as
        ///     {@link System#currentTimeMillis()})the wrapped object was last returned.
        ///     @return The time the object was last returned
        /// </summary>
        public DateTime LastReturnTime => this._pooledObject.LastReturnTime;

        /// <summary>
        ///     Obtain the time that pooled object was last returned.
        ///     @return The last returned time for the pooled object formated as
        ///     <code>yyyy-MM-dd HH:mm:ss Z</code>
        /// </summary>
        public string LastReturnTimeFormatted => this._pooledObject.LastReturnTime.ToString("yyyy-MM-dd HH:mm:ss Z");

        /// <summary>
        ///     Obtain the name of the class of the pooled object.
        ///     @return The pooled object's class name
        ///     @see Class#getName()
        /// </summary>
        public string PooledObjectType => this._pooledObject.Object.GetType().FullName;

        /// <summary>
        ///     Provides a String form of the wrapper for debug purposes. The format is
        ///     not fixed and may change at any time.
        ///     @return A string representation of the pooled object
        ///     @see Object#toString()
        /// </summary>
        public string PooledObjectToString => this._pooledObject.Object.ToString();

        /// <summary>
        ///     Get the number of times this object has been borrowed.
        ///     @return The number of times this object has been borrowed.
        ///     @since 2.1
        /// </summary>
        public long BorrowedCount
        {
            get
            {
                if (this._pooledObject is DefaultPooledObject<T>)
                {
                    return ((DefaultPooledObject<T>)this._pooledObject).BorrowedCount;
                }
                else
                {
                    return -1;
                }
            }
        }
    }
}