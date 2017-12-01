// Copyright (c) labijie.com. All rights reserved.

using System;

namespace Chopin.Pooling.Impl
{
    class AbandonedObjectCreatedException : Exception
    {
        public DateTime _createdTime;

        public AbandonedObjectCreatedException()
        {
            this._createdTime = DateTime.Now;
        }

        /// <summary>获取描述当前异常的消息。</summary>
        /// <returns>解释异常原因的错误消息或空字符串 ("")。</returns>
        public override string Message => this._createdTime.ToString();
    }
}