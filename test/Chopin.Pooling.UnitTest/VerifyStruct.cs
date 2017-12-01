using System;
using System.Collections.Generic;
using System.Text;

namespace Chopin.Pooling.UnitTest
{
    public class VerifyStruct
    {
        public VerifyStruct()
        {
            this.CreateTime = DateTime.Now;
        }

        public DateTime CreateTime { get; }

        public bool Enable => (DateTime.Now - this.CreateTime).TotalSeconds < 2;
    }
}
