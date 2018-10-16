using System;
using System.Collections.Generic;
using System.Text;

/**
* 命名空间: MemoryMap 
* 类 名： RemoveEntity
* 版本 ：v1.0
* Copyright (c) year 
*/

namespace MemoryMap
{
    /// <summary>
    /// 功能描述    ：RemoveEntity  
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018/10/14 19:41:26 
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018/10/14 19:41:26 
    /// </summary>
   public class RemoveEntity<TKey,TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }

        public RemoveType RemoveType { get; set; }
    }
}
