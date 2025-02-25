using WeasyPrint.Wrapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace WeasyPrint.Wrapper.Program
{
    public class ConsoleTraceWriter : ITraceWriter
    {
        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        public void Verbose(string message)
        {
            
        }
    }
}
