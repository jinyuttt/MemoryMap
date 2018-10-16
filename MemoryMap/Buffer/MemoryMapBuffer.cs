using System;
using System.Collections.Generic;
using System.Text;

/**
* 命名空间: MemoryMap.Buffer 
* 类 名： MemoryMapBuffer
* 版本 ：v1.0
* Copyright (c) year 
*/

namespace MemoryMap.Buffer
{
    /// <summary>
    /// 功能描述    ：MemoryMapBuffer  
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018/10/15 14:40:42 
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018/10/15 14:40:42 
    /// </summary>
   public class MemoryMapBuffer:BaseBuffer<byte[]>
    {
        public int Offset { get; set; }
        public int Size { get; set; }

        public int Capacity { get { return Data.Length; } }
    }
}
