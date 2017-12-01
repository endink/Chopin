using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chopin.Pooling.Impl
{
    /// <summary>
    /// object borrow strategy
    /// </summary>
    public enum BorrowStrategy
    {
        /// <summary>
        /// last in first out (stack)
        /// </summary>
        LIFO,
        /// <summary>
        /// first in first out (queue)
        /// </summary>
        FIFO,
        /// <summary>
        /// radom out
        /// </summary>
        Random
    }
}
