using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chopin.Pooling.Impl
{
    public class InterLockedEx
    {
        /// <summary>
        /// 和 <see cref="Interlocked.Increment(ref int)"/> 功能相同，区别在于返回原始值。
        /// </summary>
        public static int GetAndIncrement(ref int location)
        {
            return Interlocked.Increment(ref location) - 1;
        }

        /// <summary>
        /// 和 <see cref="Interlocked.Increment(ref long)"/> 功能相同，区别在于返回原始值。
        /// </summary>
        public static long GetAndIncrement(ref long location)
        {
            return Interlocked.Increment(ref location) - 1;
        }

        /// <summary>
        /// 和 <see cref="Interlocked.CompareExchange(ref int, int, int)"/> 功能相同，区别在于返回是否交换成功。
        /// </summary>
        public static bool CompareAndSet(ref int location1, int except, int update)
        {
            int oldValue = Interlocked.CompareExchange(ref location1, update, except);
            return oldValue == except;
        }

        /// <summary>
        /// 和 <see cref="Interlocked.CompareExchange(ref long, long, long)"/> 功能相同，区别在于返回是否交换成功。
        /// </summary>
        public static bool CompareAndSet(ref long location1, long except, long update)
        {
            long oldValue = Interlocked.CompareExchange(ref location1, update, except);
            return oldValue == except;
        }
    }
}
