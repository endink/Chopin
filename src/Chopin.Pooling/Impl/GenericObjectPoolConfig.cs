// Copyright (c) labijie.com. All rights reserved.

namespace Chopin.Pooling.Impl
{
    public class GenericObjectPoolConfig : BaseObjectPoolConfig
    {

        public int MaxIdle { get; set; } = 8;

        public int MinIdle { get; set; } = 0;
    }
}