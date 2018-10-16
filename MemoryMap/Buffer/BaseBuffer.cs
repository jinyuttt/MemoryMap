using System;
using System.Collections.Generic;
using System.Text;

/**
* 命名空间: MemoryMap 
* 类 名： ObjectBuffer
* 版本 ：v1.0
* Copyright (c) year 
*/

namespace MemoryMap
{
    /// <summary>
    /// 功能描述    ：ObjectBuffer  
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018/10/12 18:18:17 
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018/10/12 18:18:17 
    /// </summary>
   public class BaseBuffer<T>
    {
        private DateTime useTime = DateTime.Now;
        /// <summary>
        /// 真实数据对象
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 使用的时间
        /// </summary>
        public DateTime DateTime { get { return useTime; } set { useTime = value; } }

        //缓存丢失清理数据
        public virtual void Dispose()
        {

        }

        /// <summary>
        /// 重置
        /// </summary>
        public virtual void Reset()
        {

        }
    }
}
