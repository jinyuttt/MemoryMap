using System;
using System.Collections.Generic;
using System.Text;

/**
* 命名空间: MemoryMap.Buffer 
* 类 名： MemoryMapPool
* 版本 ：v1.0
* Copyright (c) year 
*/

namespace MemoryMap.Buffer
{
    /// <summary>
    /// 功能描述    ：MemoryMapPool  
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018/10/15 14:41:30 
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018/10/15 14:41:30 
    /// </summary>
   public class MemoryMapPool :BufferPool<byte[]>
    {
        public int arrayBufSize = 1024;//1K
        public int ArrayBufSize { get { return arrayBufSize; } set { arrayBufSize = value; } }


        /// <summary>
        /// 创建缓存
        /// </summary>
        /// <returns></returns>
        public override BaseBuffer<byte[]> Create()
        {
               MemoryMapBuffer buffer = new MemoryMapBuffer();
               buffer.Data = new byte[arrayBufSize];
               return buffer;
        }

       /// <summary>
       /// 释放缓存
       /// </summary>
       /// <param name="buffer"></param>
        public override void Free(BaseBuffer<byte[]> buffer)
        {
            MemoryMapBuffer mapBuffer =(MemoryMapBuffer) buffer;
            mapBuffer.OffSet = 0;
            mapBuffer.Size = 0;
            base.Free(mapBuffer);
            
        }


    }
}
