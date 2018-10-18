using MemoryMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime strart = DateTime.Now;
            MemoryMapStream stream = new MemoryMapStream();
            Task.Factory.StartNew(() => {
                // stream.ReadAllSegmentFileAsync(@"F:\allfiles\M.avi");
                stream.FileRead(@"F:\allfiles\M.avi");
            });
            int num = 0;
            Thread thread = new Thread(() =>
            {
                stream.FilePath = @"F:\allfiles\copy\M.avi";
                while (true)
                {
                    var buf = stream.GetMemoryMapBuffer();
                    if (buf != null)
                    {
                     
                        stream.FileWrite(buf);
                       // stream.MemoryMapAppendFile(@"F:\allfiles\copy\M.avi", buf);
                        buf.OffSet = 0;
                        buf.Size = 0;
                        stream.FreeBuffer(buf);
                        num++;
                    }
                    else
                    {
                        if(stream.IsAllComplete)
                        {
                            Console.WriteLine(num);
                            stream.FileFlush();
                            break;
                        }
                        Thread.Sleep(500);
                    }
                }
                Console.WriteLine((DateTime.Now - strart).TotalMilliseconds);
                  
            });
            thread.IsBackground = true;
            thread.Start();

            Console.Read();

        }

    }
}
