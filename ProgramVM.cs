using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Dsl;

namespace TestDsl
{
    class ProgramVM
    {
#if VM_FLAG_IS_1
        static void Main(string[] args)
        {
            string script =
@"function(main)
{
    echo(1+2*3);
};";
            VirtualMachine vm = new VirtualMachine();
            vm.Register("echo", new EchoApi());
            vm.Compile(script);
            vm.Execute();
        }
#endif
    }
    internal class EchoApi : IApi
    {
        public long Calc(long[] args)
        {
            long ret = 0;
            string prestr = string.Empty;
            foreach (long v in args) {
                Console.Write(prestr);
                Console.Write(v);
                prestr = ", ";
                ret = v;
            }
            Console.WriteLine();
            return ret;
        }
    }
}
