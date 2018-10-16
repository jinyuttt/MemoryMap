using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/**
* 命名空间: MemoryMap 
* 类 名： BufferPool
* 版本 ：v1.0
* Copyright (c) year 
*/

namespace MemoryMap
{
    /// <summary>
    /// 功能描述    ：BufferPool  
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018/10/12 18:17:47 
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018/10/12 18:17:47 
    /// </summary>
    public class BufferPool<T>
    {
        /// <summary>
        /// 缓存结构
        /// </summary>
        protected Stack<BaseBuffer<T>> buffers = new Stack<BaseBuffer<T>>(100);
        private readonly object lock_obj = new object();
        private int useNum = 0;//当前缓存使用
        private volatile bool isRun = false;//已经启动运算
        private int[] record = null;//记录
        protected int waitTime = 20;//计算的分钟时间
        private int index = 0;//索引计算
        private int  maxNum = 0;//最近一段时间最大使用
        private int havNum = 0;//已经创建的缓存
        private int minWaitTime = 100;//
        private int maxBufferNum = int.MaxValue;
        
        public BufferPool()
        {
            record = new int[waitTime];

        }

        /// <summary>
        /// 设置运行的最大值
        /// </summary>
        public int MaxBufferSize { get { return maxBufferNum; } set { maxBufferNum = value; } }
        /// <summary>
        /// 初始化缓存对象
        /// </summary>
        /// <param name="initNum"></param>
        public void InitPool(int initNum = 10)
        {
            if (initNum > 0)
            {
                for (int i = 0; i < initNum; i++)
                {
                    buffers.Push(Create());
                }
            }
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private void  RefreshCount()
        {
            if(isRun)
            {
                return;
            }
            isRun = true;
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(60000);//1分钟
                //
                record[index] = useNum;
                index++;
                if(index%waitTime==0)
                {
                    //监测waitTime分钟内没有使用的
                    lock (lock_obj)
                    {
                        var bufs= buffers.ToArray();
                        buffers.Clear();
                        foreach(var buf in bufs)
                        {
                            if((DateTime.Now-buf.DateTime).TotalMinutes<waitTime)
                            {
                                buffers.Push(buf);
                            }
                            else
                            {
                                buf.Dispose();
                            }
                        }
                        //
                        int sum = 0;
                        int avg = 0;
                        for(int i=0;i<record.Length;i++)
                        {
                            sum += record[i];
                        }
                        //计算时间内的平均值
                        avg = sum / record.Length;
                        //如果当前使用小于平均值，则最多只保留平均值个数
                        //说明当前使用可能在下降
                        if(useNum<avg&&buffers.Count>avg)
                        {
                            int num = buffers.Count - avg;
                            for (int i=0;i<num;i++)
                            {
                                var buf=  buffers.Pop();
                                buf.Dispose();//不使用时释放
                            }
                        }
                       if(useNum>avg&&useNum>maxNum&&buffers.Count> maxNum)
                        {
                            //当前使用大于平均值，并且大于最近的最大值，说明再继续增加可能
                            //那就以最大缓存值为准
                            int num = buffers.Count - avg;
                            for (int i = 0; i < num; i++)
                            {
                                var buf = buffers.Pop();
                                buf.Dispose();//不使用时释放
                            }
                        }

                    }
                }
                isRun = false;
            });
        }
        /// <summary>
        /// 创建缓存对象
        /// </summary>
        /// <returns></returns>
        public virtual BaseBuffer<T> Create()
        {
            Interlocked.Increment(ref havNum);
            return new BaseBuffer<T>();
        }

        /// <summary>
        /// 获取缓存对象
        /// </summary>
        /// <returns></returns>
        public BaseBuffer<T> GetBuffer()
        {
            lock (lock_obj)
            {
                try
                {
                    if (useNum < havNum)
                    {
                        //正在使用的小于已经创建的缓存
                        BaseBuffer<T> cache = buffers.Pop();
                        cache.DateTime = DateTime.Now;
                        useNum++;
                        this.RefreshCount();
                        return cache;
                    }
                    else if(havNum<maxBufferNum)
                    {
                        return Create();
                    }
                    else
                    {
                        return null;
                    }
                }
                catch
                {
                    return Create();
                }
            }
        }

        /// <summary>
        /// 超时获取数据
        /// </summary>
        /// <param name="waitTime">等待时间(毫秒)</param>
        /// <param name="buffer">获取的buffer对象</param>
        /// <returns>获取成功</returns>
         public bool TryGetBuffer( out  BaseBuffer<T>  buffer, int waitTime = 0)
        {
            buffer = null;
            if(waitTime<1)
            {
                if((buffer = GetBuffer())==null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                int sleepTime = 0;
                int sum = 0;
                if (waitTime < minWaitTime)
                {
                    sleepTime = waitTime;
                }
                else
                {
                    sleepTime = 100;
                }
                while ((buffer = GetBuffer()) == null)
                {
                    Thread.Sleep(sleepTime);
                    sum += sleepTime;
                    if (sum > waitTime)
                    {
                        break;
                    }
                }
                if(buffer==null)
                {
                    //最后再获取一次
                    buffer = GetBuffer();
                    if(buffer==null)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
        }


        /// <summary>
        /// 获取一组缓存
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public virtual List<BaseBuffer<T>> GetMemoryMaps(int num)
        {
            List<BaseBuffer<T>> list = new List<BaseBuffer<T>>(num);
            for(int i=0;i<num;i++)
            {
                list.Add(GetBuffer());
            }
            return list;
        }
        /// <summary>
        /// 缓存释放
        /// </summary>
        /// <param name="client"></param>
        public virtual void Free(BaseBuffer<T> buffer)
        {
            lock (lock_obj)
            {
                useNum--;
                buffers.Push(buffer);
            }
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void Clear()
        {
            lock (lock_obj)
            {

                while (buffers.Count > 0)
                {
                    BaseBuffer<T> buffer = buffers.Pop();
                    buffer.Dispose();
                }
                buffers.Clear();
            }
        }

        /// <summary>
        /// 缓存大小
        /// </summary>
        public int BufferSize { get { int num = 0; lock (lock_obj) { num = buffers.Count; };return num; } }
    }
}
