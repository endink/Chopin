using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chopin.Pooling.Impl.Atom
{
    public class AtomInt
    {
        private int _value;

        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        public AtomInt(int value)
        {
            this._value = value;
        }

        public int Value => this._value;

        public int DecrementAndGet()
        {
            return Interlocked.Decrement(ref this._value);
        }

        public int IncrementAndGet()
        {
            return Interlocked.Increment(ref this._value);
        }

        public static implicit operator AtomInt(int value)
        {
            return new AtomInt(value);
        }

        public override string ToString()
        {
            return this._value.ToString();
        }
    }
}
