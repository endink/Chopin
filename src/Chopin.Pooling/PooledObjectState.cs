// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling
{
    /// <summary>
    ///     <see cref="IPooledObject{T}" />的状态
    /// </summary>
    public enum PooledObjectState
    {
        /// <summary>
        ///     在Queue种，未被使用
        /// </summary>
        Idle,

        /// <summary>
        ///     正在呗使用
        /// </summary>
        Allocated,

        /// <summary>
        ///     在Queue种，正在被测试，可能被逐出
        /// </summary>
        Eviction,

        /// <summary>
        ///     不在队列中, 目前正在测试是否可能被逐出。在测试时, 从队列头部借用该对象, 并将其从队列中移除。一旦测试完成, 它应该返回到队列的头部。
        /// </summary>
        EvictionReturnToHead,

        /// <summary>
        ///     在队列中，已通过测试
        /// </summary>
        Validation,

        /// <summary>
        ///     Not in queue, currently being validated. The object was borrowed while
        ///     being validated and since testOnBorrow was configured, it was removed
        ///     from the queue and pre-allocated. It should be allocated once validation
        ///     completes.
        /// </summary>
        ValidationPreallocated,

        /// <summary>
        ///     Not in queue, currently being validated. An attempt to borrow the object
        ///     was made while previously being tested for eviction which removed it from
        ///     the queue. It should be returned to the head of the queue once validation
        ///     completes.
        /// </summary>
        ValidationReturnToHead,

        /// <summary>
        ///     维护错误（eg:eviction 测试或validation）将被销毁
        /// </summary>
        Invalid,

        /// <summary>
        ///     视为被遗弃，将被销毁
        /// </summary>
        Abandoned,

        /// <summary>
        ///     正在从池中返回
        /// </summary>
        Returning
    }
}