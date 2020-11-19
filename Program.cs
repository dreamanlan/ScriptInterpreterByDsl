using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Dsl;

namespace TestDsl
{
    class Program
    {
#if !VM_FLAG_IS_1
        static void Main(string[] args)
        {
            string script = 
@"function(main)
{
    echo('hello world !');
    $a=1;
    $b=2;
    if($a<$b){
        echo('LT');
    }
    else{
        echo('GE');
    };
};";
            Execute(script);
        }
#endif
        private static void Execute(string code)
        {
            Interpreter interpreter = new Interpreter();
            interpreter.Register("echo", new ExpressionFactoryHelper<EchoExp>());
            if (interpreter.Parse(code, "testscript")) {
                object v = interpreter.Call("main");
                if (null == v) {
                    Console.WriteLine("call result: null");
                }
                else {
                    Console.WriteLine("call result: {0}", v);
                }
            }
            else {
                Console.WriteLine("Parser failed.");
            }
        }
    }
    class EchoExp : SimpleExpressionBase
    {
        protected override object OnCalc(IList<object> operands)
        {
            if(operands.Count>0) {
                string fmt = operands[0] as string;
                if (null != fmt) {
                    var al = new ArrayList();
                    for (int i = 1; i < operands.Count; ++i) {
                        al.Add(operands[i]);
                    }
                    Console.WriteLine(fmt, al.ToArray());
                    return true;
                }
            }
            return false;
        }
    }
}
