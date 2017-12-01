using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chopin.Pooling.Impl.Atom
{
    public sealed class AtomLong
    {
        private long _value;

        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        public AtomLong(long value)
        {
            this._value = value;
        }

        public long Value => this._value;
        public long DecrementAndGet()
        {
           return Interlocked.Decrement(ref this._value);
        }

        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref this._value);
        }

        public static implicit operator AtomLong(long value)
        {
            return new AtomLong(value);
        }
        public override string ToString()
        {
            return this._value.ToString();
        }
    }
}
