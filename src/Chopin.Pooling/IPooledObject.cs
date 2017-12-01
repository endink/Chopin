// Copyright (c) labijie.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Chopin.Pooling.Collections;

namespace Chopin.Pooling
{
    public interface IPooledObject<T> : IEqualityComparer<T>
    {
        /// <summary>
        ///     创建包装对象的时间
        /// </summary>
        DateTime CreateTime { get; }

        /// <summary>
        ///     此对象在上次活动状态下话费的时间（毫秒）（如果它一直处于活动状态，下次调用将返回时间增量）
        /// </summary>
        long ActiveTimeMillis { get; }

        /// <summary>
        ///     获取此对象上次处于空闲状态的时间（毫秒）（如果它一直空闲，下次调用将返回空闲时间增量）
        /// </summary>
        long IdleTimeMillis { get; }

        /// <summary>
        ///     此对象最后一次被借用的时刻
        /// </summary>
        /// <returns></returns>
        DateTime LastBorrowTime { get; }

        /// <summary>
        ///     包装对象最后一次被返回的时刻
        /// </summary>
        /// <returns></returns>
        DateTime LastReturnTime { get; }

        /// <summary>
        ///     返回上次使用此对象时间的估计值。如果池对象实现了TrackedUse,
        ///     则返回TrackedUse.LastUsed和LastBorrowTime 中的最大值。
        ///     否则此方法将提供和LastBorrowTime的值
        /// </summary>
        DateTime LastUsedTime { get; }

        /**
         * Is abandoned object tracking being used? If this is true the
         * implementation will need to record the stack trace of the last caller to
         * borrow this object.
         *
         * @param   logAbandoned    The new configuration setting for abandoned
         *                          object tracking
         */

        bool LogAbandoned { set; }

        /**
         * Returns the state of this object.
         * @return state
         */

        PooledObjectState State { get; }

        /// <summary>
        ///     获取此实例所包装的基础对象。
        /// </summary>
        /// <value>返回被包装的对象</value>
        T Object { get; }

        /**
         * Attempt to place the pooled object in the
         * {@link PooledObjectState#EVICTION} state.
         *
         * @return <code>true</code> if the object was placed in the
         *         {@link PooledObjectState#EVICTION} state otherwise
         *         <code>false</code>
         */
        bool StartEvictionTest();

        /**
         * Called to inform the object that the eviction test has ended.
         *
         * @param idleQueue The queue of idle objects to which the object should be
         *                  returned
         *
         * @return  Currently not used
         */
        bool EndEvictionTest(BlockingList<IPooledObject<T>> idleQueue);

        /**
         * Allocates the object.
         *
         * @return {@code true} if the original state was {@link PooledObjectState#IDLE IDLE}
         */
        bool Allocate();

        /**
         * Deallocates the object and sets it {@link PooledObjectState#IDLE IDLE}
         * if it is currently {@link PooledObjectState#ALLOCATED ALLOCATED}.
         *
         * @return {@code true} if the state was {@link PooledObjectState#ALLOCATED ALLOCATED}
         */
        bool Deallocate();

        /**
         * Sets the state to {@link PooledObjectState#INVALID INVALID}
         */
        void Invalidate();

        /**
         * Record the current stack trace as the last time the object was used.
         */
        void Use();

        /**
         * Prints the stack trace of the code that borrowed this pooled object and
         * the stack trace of the last code to use this object (if available) to
         * the supplied writer.
         *
         * @param   writer  The destination for the debug output
         */
        void PrintStackTrace(TextWriter writer);

        /**
         * Marks the pooled object as abandoned.
         */
        void MarkAbandoned();

        /**
         * Marks the object as returning to the pool.
         */
        void MarkReturning();
    }
}