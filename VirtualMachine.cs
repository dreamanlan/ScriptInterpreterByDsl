using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestDsl
{
    internal interface IApi
    {
        long Calc(long[] args);
    }
    internal enum InsEnum : int
    {
        ARG = 0,
        VARSET,
        VAR,
        NEG,
        ADD,
        SUB,
        MUL,
        DIV,
        MOD,
        AND,
        OR,
        NOT,
        GT,
        GE,
        EQ,
        NE,
        LE,
        LT,
        LSHIFT,
        RSHIFT,
        BITAND,
        BITOR,
        BITXOR,
        BITNOT,
        PUSH,
        CALL,
        NUM
    }
    internal class Instruction
    {
        internal InsEnum Opcode;
        internal long Operand;

        internal Instruction(InsEnum op)
        {
            Opcode = op;
            Operand = 0;
        }
        internal Instruction(InsEnum op, long v)
        {
            Opcode = op;
            Operand = v;
        }
        internal Instruction(InsEnum op, int high32, int low32)
        {
            Opcode = op;
            long h = high32;
            long l = low32;
            Operand = (h << 32) + l;
        }
        internal void SetOperand(int high32, int low32)
        {
            long h = high32;
            long l = low32;
            Operand = (h << 32) + l;
        }
        internal int GetOperandHigh32()
        {
            return (int)(Operand >> 32);
        }
        internal int GetOperandLow32()
        {
            return (int)(Operand & 0xffffffff);
        }
    }
    internal sealed class VirtualMachine
    {
        internal void Register(string apiName, IApi api)
        {
            int id = GetApiOrProcId(apiName);
            m_Apis[id] = api;
        }
        internal bool Compile(string txt)
        {
            bool ret = false;
            if (!txt.EndsWith(";"))
                txt = txt + ";";
            var err = new StringBuilder();
            Dsl.DslFile file = new Dsl.DslFile();
            if (file.LoadFromString(txt, "expression", (msg) => { err.AppendLine(msg); })) {
                int count = file.DslInfos.Count;
                for (int i = 0; i < count; ++i) {
                    var func = file.DslInfos[i] as Dsl.FunctionData;
                    if (null != func && func.IsHighOrder) {
                        var codes = new List<Instruction>();
                        string name = func.LowerOrderFunction.GetParamId(0);
                        CompileSyntaxComponent(func, codes, err);
                        int proc = GetApiOrProcId(name);
                        m_Procs[proc] = codes;
                    }
                }
            }
            if (err.Length <= 0)
                ret = true;
            else
                Console.WriteLine(err.ToString());
            return ret;
        }
        internal long Execute()
        {
            m_Stack.Clear();
            int proc = GetApiOrProcId("main");
            return Call(proc, new long[0]);
        }
        private long Call(int proc, long[] args)
        {
            List<Instruction> codes;
            if(!m_Procs.TryGetValue(proc, out codes)) {
                return -1;
            }
            for (int i = 0; i < codes.Count; ++i) {
                Instruction ins = codes[i];
                switch (ins.Opcode) {
                    case InsEnum.ARG: {
                            int index = (int)ins.Operand;
                            long ret = 0;
                            if (index >= 0 && index < args.Length) {
                                ret = args[index];
                            }
                            m_Stack.Push(ret);
                        }
                        break;
                    case InsEnum.VARSET: {
                            long op2 = m_Stack.Pop();
                            int id = (int)ins.Operand;
                            m_Variables[id] = op2;
                            m_Stack.Push(op2);
                        }
                        break;
                    case InsEnum.VAR: {
                            int id = (int)ins.Operand;
                            long ret;
                            if (!m_Variables.TryGetValue(id, out ret)) {
                                ret = 0;
                            }
                            m_Stack.Push(ret);
                        }
                        break;
                    case InsEnum.NEG: {
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(-op1);
                        }
                        break;
                    case InsEnum.ADD: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 + op2);
                        }
                        break;
                    case InsEnum.SUB: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 - op2);
                        }
                        break;
                    case InsEnum.MUL: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 * op2);
                        }
                        break;
                    case InsEnum.DIV: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 / op2);
                        }
                        break;
                    case InsEnum.MOD: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 % op2);
                        }
                        break;
                    case InsEnum.AND: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push((op1 != 0 && op2 != 0) ? 1 : 0);
                        }
                        break;
                    case InsEnum.OR: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push((op1 != 0 || op2 != 0) ? 1 : 0);
                        }
                        break;
                    case InsEnum.NOT: {
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 == 0 ? 1 : 0);
                        }
                        break;
                    case InsEnum.GT: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push((op1 > op2) ? 1 : 0);
                        }
                        break;
                    case InsEnum.GE: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push((op1 >= op2) ? 1 : 0);
                        }
                        break;
                    case InsEnum.EQ: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push((op1 == op2) ? 1 : 0);
                        }
                        break;
                    case InsEnum.NE: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push((op1 != op2) ? 1 : 0);
                        }
                        break;
                    case InsEnum.LE: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push((op1 <= op2) ? 1 : 0);
                        }
                        break;
                    case InsEnum.LT: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push((op1 < op2) ? 1 : 0);
                        }
                        break;
                    case InsEnum.LSHIFT: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 << (int)op2);
                        }
                        break;
                    case InsEnum.RSHIFT: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 >> (int)op2);
                        }
                        break;
                    case InsEnum.BITAND: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 & op2);
                        }
                        break;
                    case InsEnum.BITOR: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 | op2);
                        }
                        break;
                    case InsEnum.BITXOR: {
                            long op2 = m_Stack.Pop();
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(op1 ^ op2);
                        }
                        break;
                    case InsEnum.BITNOT: {
                            long op1 = m_Stack.Pop();
                            m_Stack.Push(~op1);
                        }
                        break;
                    case InsEnum.PUSH:
                        m_Stack.Push(ins.Operand);
                        break;
                    case InsEnum.CALL: {
                            int id = ins.GetOperandHigh32();
                            int num = ins.GetOperandLow32();
                            long[] apiArgs = new long[num];
                            for (int ix = num - 1; ix >= 0; --ix) {
                                apiArgs[ix] = m_Stack.Pop();
                            }
                            long ret = 0;
                            IApi api;
                            if (m_Apis.TryGetValue(id, out api)) {
                                ret = api.Calc(apiArgs);
                            }
                            m_Stack.Push(ret);
                        }
                        break;
                }
            }
            return m_Stack.Pop();
        }

        private void CompileSyntaxComponent(Dsl.ISyntaxComponent comp, List<Instruction> codes, StringBuilder err)
        {
            var funcData = comp as Dsl.FunctionData;
            if (null != funcData) {
                if (funcData.HaveStatement()) {
                    foreach(var p in funcData.Params) {
                        CompileSyntaxComponent(p, codes, err);
                    }
                }
                else if (funcData.HaveParam()) {
                    var callData = funcData;
                    if (!callData.HaveId()) {
                        Dsl.ISyntaxComponent param = callData.GetParam(0);
                        CompileSyntaxComponent(param, codes, err);
                    }
                    else {
                        string op = callData.GetId();
                        if (op == "=") {//赋值
                            Dsl.ValueData param1 = callData.GetParam(0) as Dsl.ValueData;
                            if (null != param1) {
                                Dsl.ISyntaxComponent param2 = callData.GetParam(1);
                                CompileSyntaxComponent(param2, codes, err);
                                if (param1.GetIdType() == Dsl.ValueData.ID_TOKEN) {
                                    string name = param1.GetId();
                                    int id = GetVarId(name);
                                    codes.Add(new Instruction(InsEnum.VARSET, id));
                                }
                                else {
                                    err.AppendFormat("operator = illegal, left operand must be a var, code:{0}, line:{1}", callData.ToScriptString(false), callData.GetLine());
                                    err.AppendLine();
                                }
                            }
                            else {
                                err.AppendFormat("operator = illegal, left operand must be a var, code:{0}, line:{1}", callData.ToScriptString(false), callData.GetLine());
                                err.AppendLine();
                            }
                        }
                        else if (op == "arg") {//读参数
                            int id = int.Parse(callData.GetParamId(0));
                            codes.Add(new Instruction(InsEnum.ARG, id));
                        }
                        else {
                            int num = callData.GetParamNum();
                            for (int i = 0; i < num; ++i) {
                                Dsl.ISyntaxComponent param = callData.GetParam(i);
                                CompileSyntaxComponent(param, codes, err);
                            }
                            if (callData.GetParamClass() == (int)Dsl.FunctionData.ParamClassEnum.PARAM_CLASS_OPERATOR) {
                                if (num == 2 && op != "!" && op != "~" || num == 1 && (op == "+" || op == "-" || op == "!" || op == "~")) {
                                    if (op == "+") {
                                        if (num == 2) {
                                            codes.Add(new Instruction(InsEnum.ADD));
                                        }
                                    }
                                    else if (op == "-") {
                                        if (num == 2) {
                                            codes.Add(new Instruction(InsEnum.SUB));
                                        }
                                        else {
                                            codes.Add(new Instruction(InsEnum.NEG));
                                        }
                                    }
                                    else if (op == "*") {
                                        codes.Add(new Instruction(InsEnum.MUL));
                                    }
                                    else if (op == "/") {
                                        codes.Add(new Instruction(InsEnum.DIV));
                                    }
                                    else if (op == "%") {
                                        codes.Add(new Instruction(InsEnum.MOD));
                                    }
                                    else if (op == "&&") {
                                        codes.Add(new Instruction(InsEnum.AND));
                                    }
                                    else if (op == "||") {
                                        codes.Add(new Instruction(InsEnum.OR));
                                    }
                                    else if (op == "!") {
                                        codes.Add(new Instruction(InsEnum.NOT));
                                    }
                                    else if (op == ">") {
                                        codes.Add(new Instruction(InsEnum.GT));
                                    }
                                    else if (op == ">=") {
                                        codes.Add(new Instruction(InsEnum.GE));
                                    }
                                    else if (op == "==") {
                                        codes.Add(new Instruction(InsEnum.EQ));
                                    }
                                    else if (op == "!=") {
                                        codes.Add(new Instruction(InsEnum.NE));
                                    }
                                    else if (op == "<=") {
                                        codes.Add(new Instruction(InsEnum.LE));
                                    }
                                    else if (op == "<") {
                                        codes.Add(new Instruction(InsEnum.LT));
                                    }
                                    else if (op == "<<") {
                                        codes.Add(new Instruction(InsEnum.LSHIFT));
                                    }
                                    else if (op == ">>") {
                                        codes.Add(new Instruction(InsEnum.RSHIFT));
                                    }
                                    else if (op == "&") {
                                        codes.Add(new Instruction(InsEnum.BITAND));
                                    }
                                    else if (op == "|") {
                                        codes.Add(new Instruction(InsEnum.BITOR));
                                    }
                                    else if (op == "^") {
                                        codes.Add(new Instruction(InsEnum.BITXOR));
                                    }
                                    else if (op == "~") {
                                        codes.Add(new Instruction(InsEnum.BITNOT));
                                    }
                                    else {
                                        err.AppendFormat("operator '{0}' illegal, code:{1}, line:{2}", op, callData.ToScriptString(false), callData.GetLine());
                                        err.AppendLine();
                                    }
                                }
                                else {
                                    err.AppendFormat("operator '{0}' arg num {1} illegal, code:{2}, line:{3}", op, num, callData.ToScriptString(false), callData.GetLine());
                                    err.AppendLine();
                                }
                            }
                            else {
                                int procId = GetApiOrProcId(op);
                                codes.Add(new Instruction(InsEnum.CALL, procId, num));
                            }
                        }
                    }
                }
            }
            else {
                Dsl.ValueData valueData = comp as Dsl.ValueData;
                if (null != valueData) {
                    if (valueData.GetIdType() == Dsl.ValueData.ID_TOKEN) {
                        //变量
                        string name = valueData.GetId();
                        int id = GetVarId(name);
                        codes.Add(new Instruction(InsEnum.VAR, id));
                    }
                    else if (valueData.GetIdType() == Dsl.ValueData.NUM_TOKEN) {
                        //普通常量
                        try {
                            long val = long.Parse(valueData.GetId());
                            codes.Add(new Instruction(InsEnum.PUSH, val));
                        }
                        catch {
                            err.AppendFormat("const must be integer, code:{0}, line:{1}", comp.ToScriptString(false), comp.GetLine());
                            err.AppendLine();
                        }
                    }
                }
            }
        }
        private int GetVarId(string name)
        {
            int id;
            if (!m_VarMap.TryGetValue(name, out id)) {
                id = m_NextVarId++;
                m_VarMap.Add(name, id);
            }
            return id;
        }
        private int GetApiOrProcId(string name)
        {
            int id;
            if (!m_ApiOrProcMap.TryGetValue(name, out id)) {
                id = m_NextApiOrProcId++;
                m_ApiOrProcMap.Add(name, id);
            }
            return id;
        }
        
        private Stack<long> m_Stack = new Stack<long>();

        private Dictionary<int, long> m_Variables = new Dictionary<int, long>();
        private Dictionary<int, List<Instruction>> m_Procs = new Dictionary<int, List<Instruction>>();
        private Dictionary<int, IApi> m_Apis = new Dictionary<int, IApi>();

        private Dictionary<string, int> m_VarMap = new Dictionary<string, int>();
        private Dictionary<string, int> m_ApiOrProcMap = new Dictionary<string, int>();
        private int m_NextVarId = 0;
        private int m_NextApiOrProcId = 0;
    }
}
