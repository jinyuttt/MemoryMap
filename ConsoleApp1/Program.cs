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
            MemoryMapStream stream = new MemoryMapStream();
            stream.ReadAllSegmentFileAsync(@"F:\allfiles\M.avi");
            Thread thread = new Thread(() =>
            {
                while(!stream.IsComplete)
                {
                    while (true)
                    {
                       var buf = stream.GetMemoryMapBuffer();
                        if (buf != null)
                        {
                            stream.FileStreamAppendFile(@"F:\allfiles\copy\M.avi", buf);
                            buf.Offset = 0;
                            buf.Size = 0;
                            stream.FreeBuffer(buf);
                        }
                        else
                        {
                            Thread.Sleep(500);
                        }
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start();

            Console.Read();

        }

    }
}
