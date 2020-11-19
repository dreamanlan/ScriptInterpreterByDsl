using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dsl;

namespace TestDsl
{
    internal sealed class ArgGet : AbstractExpression
    {
        protected override object DoCalc()
        {
            object ret = null;
            var ix = CastTo<int>(m_ArgIndex.Calc());
            var args = Interpreter.GetLocalInfo().Arguments;
            if (ix >= 0 && ix < args.Count) {
                ret = args[ix];
            }
            return ret;
        }
        protected override bool Load(Dsl.FunctionData callData)
        {
            m_ArgIndex = Interpreter.Load(callData.GetParam(0));
            return true;
        }

        private IExpression m_ArgIndex;
    }
    internal sealed class NamedVarSet : AbstractExpression
    {
        protected override object DoCalc()
        {
            object v = m_Val.Calc();
            if (m_VarId.Length > 0) {
                if (m_VarId.StartsWith("$")) {
                    var locals = Interpreter.GetLocalInfo().LocalVariables;
                    locals[m_VarId] = v;
                }
                else {
                    var globals = Interpreter.GetGlobalInfo();
                    globals[m_VarId] = v;
                }
            }
            return v;
        }
        protected override bool Load(Dsl.FunctionData callData)
        {
            Dsl.ISyntaxComponent param1 = callData.GetParam(0);
            Dsl.ISyntaxComponent param2 = callData.GetParam(1);
            m_VarId = param1.GetId();
            m_Val = Interpreter.Load(param2);
            return true;
        }

        private string m_VarId;
        private IExpression m_Val;
    }
    internal sealed class NamedVarGet : AbstractExpression
    {
        protected override object DoCalc()
        {
            object ret = null;
            if (m_VarId.Length > 0) {
                if (m_VarId.StartsWith("$")) {
                    var locals = Interpreter.GetLocalInfo().LocalVariables;
                    locals.TryGetValue(m_VarId, out ret);
                }
                else {
                    var globals = Interpreter.GetGlobalInfo();
                    globals.TryGetValue(m_VarId, out ret);
                }
            }
            return ret;
        }
        protected override bool Load(Dsl.ValueData valData)
        {
            m_VarId = valData.GetId();
            return true;
        }

        private string m_VarId;
    }
    internal class ConstGet : AbstractExpression
    {
        protected override object DoCalc()
        {
            return m_Val;
        }

        protected override bool Load(ValueData valData)
        {
            string id = valData.GetId();
            int idType = valData.GetIdType();
            if (idType == Dsl.ValueData.NUM_TOKEN) {
                if (id.StartsWith("0x")) {
                    long v = long.Parse(id.Substring(2), System.Globalization.NumberStyles.HexNumber);
                    if (v >= int.MinValue && v <= int.MaxValue) {
                        m_Val = (int)v;
                    }
                    else {
                        m_Val = v;
                    }
                }
                else if (id.IndexOf('.') < 0) {
                    long v = long.Parse(id);
                    if (v >= int.MinValue && v <= int.MaxValue) {
                        m_Val = (int)v;
                    }
                    else {
                        m_Val = v;
                    }
                }
                else {
                    double v = double.Parse(id);
                    if (v >= float.MinValue && v <= float.MaxValue) {
                        m_Val = (float)v;
                    }
                    else {
                        m_Val = v;
                    }
                }
            }
            else {
                if (idType == Dsl.ValueData.ID_TOKEN) {
                    if (id == "true")
                        m_Val = true;
                    else if (id == "false")
                        m_Val = false;
                    else
                        m_Val = id;
                }
                else {
                    m_Val = id;
                }
            }
            return true;
        }

        private object m_Val = null;
    }
    internal class FuncDefExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            object r = null;
            foreach (var exp in m_Statements) {
                r = exp.Calc();
            }
            return r;
        }
        protected override bool Load(FunctionData funcData)
        {
            if (null != funcData) {
                foreach (var comp in funcData.Params) {
                    var exp = Interpreter.Load(comp);
                    if (null != exp) {
                        m_Statements.Add(exp);
                    }
                    else {
                        Interpreter.Log("[error] can't load {0}", comp.ToScriptString(false));
                    }
                }
                return true;
            }
            return false;
        }

        private List<IExpression> m_Statements = new List<IExpression>();
    }
    internal class FuncCallExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            ArrayList al = new ArrayList();
            foreach(var arg in m_Args) {
                var o = arg.Calc();
                al.Add(o);
            }
            object r = Interpreter.Call(m_Func, al.ToArray());
            return r;
        }
        protected override bool Load(FunctionData funcData)
        {
            if (funcData.HaveParam()) {
                int num = funcData.GetParamNum();
                for (int ix = 0; ix < num; ++ix) {
                    Dsl.ISyntaxComponent param = funcData.GetParam(ix);
                    m_Args.Add(Interpreter.Load(param));
                }
                return true;
            }
            return false;
        }
        private string m_Func = string.Empty;
        private List<IExpression> m_Args = new List<IExpression>();
    }
}
