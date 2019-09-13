using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dsl;

namespace TestDsl
{
    class Program
    {
        static void Main(string[] args)
        {
            string script = @"function(main)
{
    echo('hello world !');
};";
            Dsl.DslFile file = new Dsl.DslFile();
            if(file.LoadFromString(script, "testscript", msg => { Console.WriteLine(msg); })) {
                Execute(file);
            }
        }
        private static void Execute(Dsl.DslFile file)
        {
            Interpreter interpreter = new Interpreter();
            interpreter.Register("echo", new EchoExp());
            interpreter.Parse(file);
            object v = interpreter.Execute();
            if (null == v) {
                Console.WriteLine("call result: null");
            } else {
                Console.WriteLine("call result: {0}", v);
            }
        }
    }
    class Interpreter
    {
        public void Register(string name, IExpression api)
        {
            m_Apis.Add(name, api);
        }
        public IExpression GetFunc(string name)
        {
            IExpression func;
            if(m_Funcs.TryGetValue(name, out func)) {
                return func;
            }
            if(m_Apis.TryGetValue(name, out func)) {
                return func;
            }
            return null;
        }
        public void Parse(Dsl.DslFile file)
        {
            foreach(var info in file.DslInfos) {
                var key = info.GetId();
                if (key == "function" && info.GetFunctionNum()==1) {
                    var funcData = info.First;
                    var name = funcData.Call.GetParamId(0);
                    var funcExp = new FuncExp();
                    funcExp.Load(this, funcData);
                    m_Funcs.Add(name, funcExp);
                } else {
                    //report error！
                }
            }
        }
        public object Execute()
        {
            IExpression mainFunc;
            if (m_Funcs.TryGetValue("main", out mainFunc)) {
                return mainFunc.Calc(this);
            }
            else {
                Console.WriteLine("Can't find main proc !");
                return -1;
            }
        }

        private Dictionary<string, IExpression> m_Apis = new Dictionary<string, IExpression>();
        private Dictionary<string, IExpression> m_Funcs = new Dictionary<string, IExpression>();
    }
    interface IExpression
    {
        void Load(Interpreter interpreter, Dsl.ISyntaxComponent syntax);
        object Calc(Interpreter interpreter);
    }
    class ConstExp : IExpression
    {
        public object Calc(Interpreter interpreter)
        {
            return m_Value;
        }

        public void Load(Interpreter interpreter, ISyntaxComponent syntax)
        {
            var vd = syntax as Dsl.ValueData;
            if (null != vd) {
                m_Value = vd.GetId();
            }
        }

        private string m_Value = null;
    }
    class EchoExp : IExpression
    {
        public object Calc(Interpreter interpreter)
        {
            if (null != m_Fmt) {
                string fmt = m_Fmt.Calc(interpreter) as string;
                var al = new ArrayList();
                foreach(var exp in m_Args) {
                    al.Add(exp.Calc(interpreter));
                }
                Console.WriteLine(fmt, al.ToArray());
                return true;
            } else {
                return false;
            }
        }

        public void Load(Interpreter interpreter, ISyntaxComponent syntax)
        {
            var cd = syntax as Dsl.CallData;
            if (null != cd && cd.GetParamNum() >= 1) {
                m_Fmt = null;
                m_Args.Clear();
                for (int i = 0; i < cd.GetParamNum(); ++i) {
                    var p = cd.GetParam(i);
                    var pcd = p as Dsl.CallData;
                    if (null != pcd) {
                        string name = pcd.GetId();
                        var proc = interpreter.GetFunc(name);
                        if (null != proc) {
                            proc.Load(interpreter, p);
                            m_Args.Add(proc);
                        } else {
                            //report error!
                        }
                    } else {
                        var pvd = p as Dsl.ValueData;
                        if (null != pvd) {
                            var constExp = new ConstExp();
                            constExp.Load(interpreter, p);
                            if (i == 0) {
                                m_Fmt = constExp;
                            } else {
                                m_Args.Add(constExp);
                            }
                        } else {
                            //report error!
                        }
                    }
                }
            } else {
                //report error!
            }
        }

        private IExpression m_Fmt = null;
        private List<IExpression> m_Args = new List<IExpression>();
    }
    class FuncExp : IExpression
    {
        public object Calc(Interpreter interpreter)
        {
            object ret = null;
            foreach (var exp in m_Statements) {
                ret = exp.Calc(interpreter);
            }
            return ret;
        }
        public void Load(Interpreter interpreter, Dsl.ISyntaxComponent syntax)
        {
            var funcData = syntax as Dsl.FunctionData;
            if (null != funcData) {
                foreach (var comp in funcData.Statements) {
                    var cd = comp as Dsl.CallData;
                    if (null != cd) {
                        var name = cd.GetId();
                        var exp = interpreter.GetFunc(name);
                        if (null != exp) {
                            exp.Load(interpreter, comp);
                            m_Statements.Add(exp);
                        }
                        else {
                            //report error!
                        }
                    }
                    else {
                        //report error！
                    }
                }
            } else {
                //report error!
            }
        }

        private List<IExpression> m_Statements = new List<IExpression>();
    }
}
