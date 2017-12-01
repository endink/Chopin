using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chopin.Pooling.Impl;

namespace Chopin.Pooling.Sample
{
    public class GenericPoolSample
    {
        private GenericObjectPool<SampleClass> pool;

        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        public GenericPoolSample()
        {
            this.pool = new GenericObjectPool<SampleClass>(new SampleClassFactory(),new GenericObjectPoolConfig(){TestWhileIdle = true,TestOnBorrow = true,TestOnReturn = true});
            
        }

        public void Test()
        {
            while (true)
            {
                var obj = this.pool.BorrowObject();
                Console.WriteLine($"obj id:{obj.Id} browsered!");
                this.pool.ReturnObject(obj);
                Console.WriteLine($"obj id:{obj.Id} retured!");
                Thread.Sleep(1000);
            }
        }

        public void TestParalle()
        {
            Parallel.For(0, 1024, (i)=>
            {
                var obj = this.pool.BorrowObject();
                Console.WriteLine($"obj id:{obj.Id} browsered!");
                this.pool.ReturnObject(obj);
                Console.WriteLine($"obj id:{obj.Id} retured!");
            });
        }
    }

    public class SampleClass : IDisposable
    {
        private static int _no;

        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        public SampleClass()
        {
            this.Id = Interlocked.Increment(ref _no);
        }

        public int Id { get; }

        /// <summary>执行与释放或重置非托管资源关联的应用程序定义的任务。</summary>
        public void Dispose()
        {
            Console.WriteLine($"object id:{this.Id} is destroied");
        }
    }

    public class SampleClassFactory : IPooledObjectFactory<SampleClass>
    {
        public IPooledObject<SampleClass> MakeObject()
        {
           return new DefaultPooledObject<SampleClass>(new SampleClass());
        }

        public void DestroyObject(IPooledObject<SampleClass> @object)
        {
            @object.Object.Dispose();
        }

        public bool ValidateObject(IPooledObject<SampleClass> @object)
        {
            if(DateTime.Now - @object.CreateTime > TimeSpan.FromSeconds(5))
            {
               return @object.Object.Id % 2 == 0;
            }
            else
            {
                return true;
            }
        }

        public void ActivateObject(IPooledObject<SampleClass> @object)
        {
            
        }

        public void PassivateObject(IPooledObject<SampleClass> @object)
        {
            
        }
    }
}
