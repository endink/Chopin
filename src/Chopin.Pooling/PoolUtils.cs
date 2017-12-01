// Copyright (c) labijie.com. All rights reserved.

using System;

namespace Chopin.Pooling
{
    public sealed class PoolUtils
    {
        public static void CheckRethrow(Exception exception)
        {
            if (exception is OutOfMemoryException || exception is OverflowException || exception is InvalidCastException)
            {
                throw exception;
            }
        }
    }
}