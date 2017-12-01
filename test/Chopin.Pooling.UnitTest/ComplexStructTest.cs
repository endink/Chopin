using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Chopin.Pooling.Impl;
using Xunit;

namespace Chopin.Pooling.UnitTest
{
    public class ComplexStructTest
    {
        [Fact]
        public void PoolComplexStructTest()
        {
            var hashSet = new HashSet<int>();
            var pool = new GenericKeyedObjectPool<string, VerifyStruct>(new VerifyStructFactory(), new GenericKeyedObjectPoolConfig() { TestOnBorrow = true, TestOnReturn = true });
            for (int i = 0; i < 10; i++)
            {
                var obj = pool.BorrowObject("test");
                Assert.True(obj.Enable);
                pool.ReturnObject("test",obj);
                hashSet.Add(obj.GetHashCode());
                Thread.Sleep(1000);
            }
            Assert.True(hashSet.Count > 1);
            
        }
    }

    public class VerifyStructFactory : IKeyedPooledObjectFactory<string, VerifyStruct>
    {
        public IPooledObject<VerifyStruct> MakeObject(string key)
        {
            return new DefaultPooledObject<VerifyStruct>(new VerifyStruct());
        }

        public void DestroyObject(string key, IPooledObject<VerifyStruct> p)
        {

        }

        public bool ValidateObject(string key, IPooledObject<VerifyStruct> p)
        {
            return p.Object.Enable;
        }

        public void ActivateObject(string key, IPooledObject<VerifyStruct> p)
        {

        }

        public void PassivateObject(string key, IPooledObject<VerifyStruct> p)
        {

        }
    }
}
