using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.IO;
using MemoryMap.Buffer;
using System.Threading.Tasks;
using System.Threading;
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
        private MemoryMapPool mapPool = null;
        const long  DefaultSize = 20 * 1024 * 1024;//内存映射视图大小
        const long  WriteCacheSzie = 20 * 1024 * 1024;//FileStream方法写入大小
        const int fsCacheSize = 5 * 1024 * 1024;//FileStream内部缓存大小
        private  MemoryMapBuffer mapBuffer = null;
        private MemoryMapBuffer copyBuffer = null;
        private AutoResetEvent autoReset = new AutoResetEvent(true);

        /// <summary>
        /// 读取数据
        /// </summary>
        private List<MemoryMapBuffer> lstData = new List<MemoryMapBuffer>();

        /// <summary>
        /// 锁定对象
        /// </summary>
        private object lock_obj = new object();

        /// <summary>
        /// 写入文件时文件路径
        /// 采用缓冲写入
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 当前缓存大小
        /// </summary>
        public int BufferSize { get { return mapPool.BufferSize; } }

        /// <summary>
        /// 读取整个文件结束
        /// </summary>
        public bool IsAllComplete { get { return IsComplete && lstData.Count == 0; } }

        /// <summary>
        /// 完成当前读取
        /// </summary>
        public bool IsComplete { get; set; }

        public MemoryMapStream()
        {
            mapPool = new MemoryMapPool();
            mapBuffer = new MemoryMapBuffer();
            copyBuffer = new MemoryMapBuffer();
            mapPool.ArrayBufSize = 5 * 1024 * 1024;//1个buffer5M;
            mapPool.MaxBufferSize = 100;//500M缓存;
            mapPool.InitPool(10);//50M准备；
            mapBuffer.Data = new byte[WriteCacheSzie];
            copyBuffer.Data = new byte[WriteCacheSzie];
          
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

        #region 内存映射读取

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
            FileStream fs = new FileStream(path, FileMode.Open,FileAccess.Read,FileShare.ReadWrite,fsCacheSize);
            len = fs.Length;
            //读取文件，capacity参数不能小于文件长度；
            //但是capacity只是一个值，并不是就需要这么大内存
            //内存在下一步创建视图时确定大小
            //需要把视图部分加载到内存
            using (MemoryMappedFile memoryFile = MemoryMappedFile.CreateFromFile(fs, mapName, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true))
            {

                while (len > 0)
                {
                    //20M读取
                    //
                    size = len > DefaultSize ? DefaultSize : len;
                   //offset,size参数决定了当前访问位置及内存中大小。
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
                                r = accessor.ReadArray<byte>(index, buf.Data, buf.OffSet, buf.Capacity);
                                index += r;
                                buf.Size = r;
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
        public void MemoryMapAppendFile(string path, byte[] content, string mapName = null)
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists)
            {
                file.Create();
            }
            long len = file.Length;

            using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, fsCacheSize))
            { 
            //写入时，尤其是追加数据，capacity必须大于文件长度
            //写入时，所以要在文件长度上加入写入的数据
            //但是最后文件是设置的capacity大小
            using (MemoryMappedFile memoryFile = MemoryMappedFile.CreateFromFile(fs, mapName, len + content.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
            {
                //offset,size参数决定了当前访问位置及内存中大小。
                using (var accessor = memoryFile.CreateViewAccessor(len, content.Length))
                {
                    accessor.WriteArray<byte>(0, content, 0, content.Length);
                }
            }
        }
           
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
            using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, fsCacheSize))
            {
                using (MemoryMappedFile memoryFile = MemoryMappedFile.CreateFromFile(fs, mapName, len + buffer.Size, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
                {
                    using (var accessor = memoryFile.CreateViewAccessor(len, buffer.Size))
                    {
                        accessor.WriteArray<byte>(0, buffer.Data, buffer.OffSet, buffer.Size);
                    }
                }
            }
        }

        #endregion

        #region FileStream 直接写入

        /// <summary>
        /// 文件流写入
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        public void FileStreamAppendFile(string path, byte[] content )
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
        public  void FileStreamAppendFile(string path, MemoryMapBuffer buffer)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite,fsCacheSize))
            {
                fileStream.Write(buffer.Data, buffer.OffSet, buffer.Size);
                fileStream.Flush();
            }
        }

        #endregion


        #region FileStream放入缓存写入
        /// <summary>
        /// 写入文件
        /// 需要设置FilePath属性
        /// </summary>
        /// <param name="content"></param>
        public void FileWrite(byte[]content)
        {
            
            if(mapBuffer.Size+content.Length<=WriteCacheSzie)
            {
                Array.Copy(content,0,mapBuffer.Data,mapBuffer.Size,content.Length);
                mapBuffer.Size += content.Length;
            }
            else
            {
               
                FileStreamAppendFile(FilePath, mapBuffer);
                mapBuffer.OffSet = 0;
                mapBuffer.Size = 0;
                Array.Copy(content, 0, mapBuffer.Data, mapBuffer.Size, content.Length);
                mapBuffer.Size += content.Length;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        public  void FileWrite(MemoryMapBuffer buffer)
        {
            if (mapBuffer.Size + buffer.Size <= WriteCacheSzie)
            {
                Array.Copy(buffer.Data, buffer.OffSet, mapBuffer.Data, mapBuffer.Size, buffer.Size);
                mapBuffer.Size += buffer.Size;
            }
            else
            {
                   autoReset.WaitOne();
                   Array.Copy(mapBuffer.Data, mapBuffer.OffSet, copyBuffer.Data, 0, mapBuffer.Size);
                   copyBuffer.Size = mapBuffer.Size;
                   Task.Factory.StartNew(() => {
                     FileStreamAppendFile(FilePath, copyBuffer);
                     autoReset.Set();
                 });
 
                mapBuffer.Size = 0;
                Array.Copy(buffer.Data, buffer.OffSet, mapBuffer.Data, mapBuffer.Size, buffer.Size);
                mapBuffer.Size += buffer.Size;
            }
        }
       
        /// <summary>
        /// 写入文件
        /// 需要设置FilePath属性
        /// </summary>
        public void FileFlush()
        {
            autoReset.WaitOne();
            FileStreamAppendFile(FilePath, mapBuffer);
        }

        #endregion


        #region FileStream 读文件
        /// <summary>
        /// FileStream读取数据
        /// </summary>
        /// <param name="path"></param>
        public void FileRead(string path)
        {
            IsComplete = false;
            int r = 0;
            using (FileStream fs = new FileStream(path, FileMode.Open,FileAccess.Read,FileShare.ReadWrite,fsCacheSize))
            {
                while (true)
                {
                    MemoryMapBuffer buf = (MemoryMapBuffer)mapPool.GetBuffer();
                    r = fs.Read(buf.Data, 0, buf.Capacity);
                    buf.Size = r;
                    lock (lock_obj)
                    {
                        lstData.Add(buf);
                    }
                   if(r<buf.Capacity)
                    {
                        break;
                    }
                }

            }
            IsComplete = true;

        }

        #endregion
    }
}
