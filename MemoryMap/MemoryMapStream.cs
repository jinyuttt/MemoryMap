using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.IO;
using MemoryMap.Buffer;
/**
* 命名空间: MemoryMap 
* 类 名： MemoryMapStream
* 版本 ：v1.0
* Copyright (c) year 
*/

namespace MemoryMap
{
    /// <summary>
    /// 功能描述    ：MemoryMapStream  
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018/10/15 2:08:45 
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018/10/15 2:08:45 
    /// </summary>
   public class MemoryMapStream
    {
        MemoryMapPool mapPool = null;
        private long minSegment = 10 * 1024 * 1024;//10M
        private long pageSize = 1048576;//1M
        const long DefaultCapacity = 20 * 1024 * 1024;
        const long  DefaultSize = 20 * 1024 * 1024;

        /// <summary>
        /// 读取数据
        /// </summary>
        private List<MemoryMapBuffer> lstData = new List<MemoryMapBuffer>();

        /// <summary>
        /// 锁定对象
        /// </summary>
        private object lock_obj = new object();

        /// <summary>
        /// 当前缓存大小
        /// </summary>
        public int BufferSize { get { return mapPool.BufferSize; } }

        /// <summary>
        /// 读取整个文件结束
        /// </summary>
        public bool IsAllComplete { get; set; }

        /// <summary>
        /// 完成当前读取
        /// </summary>
        public bool IsComplete { get; set; }

        public MemoryMapStream()
        {
            mapPool = new MemoryMapPool();
            mapPool.ArrayBufSize = 5 * 1024 * 1024;//1个buffer5M;
            mapPool.MaxBufferSize = 100;//500M缓存;
            mapPool.InitPool(10);//50M准备；
          
        }
        public void InitBuffer(int num=10)
        {
            mapPool.InitPool(num);
        }
        public void FreeBuffer(MemoryMapBuffer buffer)
        {
            mapPool.Free(buffer);
        }

        public MemoryMapBuffer GetMemoryMapBuffer()
        {
            lock(lock_obj)
            {
                if(lstData.Count>0)
                {
                    MemoryMapBuffer buffer = lstData[0];
                    lstData.RemoveAt(0);
                    return buffer;
                }
                else
                {
                    return null;
                }
            }
        }

     
        /// <summary>
        /// 读取文件
        /// 20M分段读取
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mapName"></param>
        public async  void ReadAllSegmentFileAsync(string path, string mapName = null)
        {
            IsComplete = false;
            FileInfo file = new FileInfo(path);
            if (!file.Exists)
            {
                return;
            }
           
            long offset = 0;
            long len = 0;
            long size = DefaultSize;
            FileStream fs = new FileStream(path, FileMode.Open);
            len = fs.Length;
            using (MemoryMappedFile memoryFile = MemoryMappedFile.CreateFromFile(fs, mapName, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true))
            {

                while (len > 0)
                {
                    //20M读取
                    //
                    size = len > DefaultSize ? DefaultSize : len;
                    using (var accessor = memoryFile.CreateViewAccessor(offset, size, MemoryMappedFileAccess.Read))
                    {
                        int index = 0;//当前读取
                        int r = 0;//当前读取长度
                        offset += size;//设置便宜值
                        len -= size;//剩余长度
                        while (true)
                        {

                            BaseBuffer<byte[]> item = null;
                            if (mapPool.TryGetBuffer(out item, 1000))
                            {
                                MemoryMapBuffer buf = (MemoryMapBuffer)item;
                                r = accessor.ReadArray<byte>(index, buf.Data, buf.Offset, buf.Capacity - buf.Size);
                                index += r;
                                buf.Size = buf.Capacity - buf.Offset;
                                lock (lock_obj)
                                {
                                    lstData.Add(buf);
                                }
                                if (r < buf.Size || index >= DefaultSize)
                                {
                                    //20M读取完成
                                    break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("MemoryMapStream:无法获取缓存，业务处理慢");
                            }
                        }

                    }

                }
            }
            IsComplete = true;
            fs.Close();
        }

        /// <summary>
        /// 内存映射文件写入数据
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        /// <param name="mapName"></param>
        public void  MemoryMapAppendFile(string path,byte[] content, string mapName = null)
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists)
            {
                file.Create();
            }
            long len = file.Length;
            FileStream fs = null;
            if (len > 0)
            {
               fs= new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            }
            else
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
            }
            using (MemoryMappedFile memoryFile = MemoryMappedFile.CreateFromFile(fs, mapName, len+content.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
            {
                using (var accessor = memoryFile.CreateViewAccessor(len, content.Length))
                {
                    accessor.WriteArray<byte>(0, content, 0, content.Length);
                }
            }
            fs.Close();
        }

        /// <summary>
        /// 内存映射文件写入数据
        /// </summary>
        /// <param name="path"></param>
        /// <param name="buffer"></param>
        /// <param name="mapName"></param>
        public void  MemoryMapAppendFile(string path, MemoryMapBuffer buffer, string mapName = null)
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists)
            {
                file.Create();
            }
            long len = file.Length;
            FileStream fs = null;
            if (len > 0)
            {
                fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            }
            else
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
            }
            using (MemoryMappedFile memoryFile = MemoryMappedFile.CreateFromFile(fs, mapName, len + buffer.Size, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
            {
                using (var accessor = memoryFile.CreateViewAccessor(len, buffer.Size))
                {
                    accessor.WriteArray<byte>(0, buffer.Data, buffer.Offset, buffer.Size);
                }
            }
            fs.Close();
            mapPool.Free(buffer);
        }


        /// <summary>
        /// 文件流写入
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        public  void FileStreamAppendFile(string path, byte[] content )
        {
            FileStream fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

            fileStream.WriteAsync(content, 0, content.Length);

            fileStream.Close();

            fileStream.Dispose();
        }

       /// <summary>
       /// 文件流写入
       /// </summary>
       /// <param name="path"></param>
       /// <param name="buffer"></param>
        public void FileStreamAppendFile(string path, MemoryMapBuffer buffer)
        {
            FileStream fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

            fileStream.WriteAsync(buffer.Data, buffer.Offset, buffer.Size);

            fileStream.Close();

            fileStream.Dispose();
        }

    }
}
