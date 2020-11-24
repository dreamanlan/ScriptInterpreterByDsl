using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Dsl;

namespace TestDsl
{
    public interface IExpression
    {
        bool Load(Dsl.ISyntaxComponent syntax, Interpreter interpreter);
        object Calc();
    }
    public interface IExpressionFactory
    {
        IExpression Create();
    }
    public sealed class ExpressionFactoryHelper<T> : IExpressionFactory where T : IExpression, new()
    {
        public IExpression Create()
        {
            return new T();
        }
    }
    public abstract class AbstractExpression : IExpression
    {
        public object Calc()
        {
            object ret = null;
            try {
                ret = DoCalc();
            }
            catch (Exception ex) {
                var msg = string.Format("calc:[{0}]", ToString());
                throw new Exception(msg, ex);
            }
            return ret;
        }
        public bool Load(Dsl.ISyntaxComponent dsl, Interpreter interpreter)
        {
            m_Interpreter = interpreter;
            m_Dsl = dsl;
            Dsl.ValueData valueData = dsl as Dsl.ValueData;
            if (null != valueData) {
                return Load(valueData);
            }
            else {
                Dsl.FunctionData funcData = dsl as Dsl.FunctionData;
                if (null != funcData) {
                    if (funcData.HaveParam()) {
                        var callData = funcData;
                        bool ret = Load(callData);
                        if (!ret) {
                            int num = callData.GetParamNum();
                            List<IExpression> args = new List<IExpression>();
                            for (int ix = 0; ix < num; ++ix) {
                                Dsl.ISyntaxComponent param = callData.GetParam(ix);
                                args.Add(interpreter.Load(param));
                            }
                            return Load(args);
                        }
                        return ret;
                    }
                    else {
                        return Load(funcData);
                    }
                }
                else {
                    Dsl.StatementData statementData = dsl as Dsl.StatementData;
                    if (null != statementData) {
                        return Load(statementData);
                    }
                }
            }
            return false;
        }
        public override string ToString()
        {
            return string.Format("{0} line:{1}", base.ToString(), m_Dsl.GetLine());
        }
        protected virtual bool Load(Dsl.ValueData valData) { return false; }
        protected virtual bool Load(IList<IExpression> exps) { return false; }
        protected virtual bool Load(Dsl.FunctionData funcData) { return false; }
        protected virtual bool Load(Dsl.StatementData statementData) { return false; }
        protected abstract object DoCalc();

        protected Interpreter Interpreter
        {
            get { return m_Interpreter; }
        }

        private Interpreter m_Interpreter = null;
        private Dsl.ISyntaxComponent m_Dsl = null;
        protected static T CastTo<T>(object obj)
        {
            if (obj is T) {
                return (T)obj;
            }
            else {
                try {
                    return (T)Convert.ChangeType(obj, typeof(T));
                }
                catch {
                    return default(T);
                }
            }
        }
        protected static object CastTo(Type t, object obj)
        {
            if (null == obj)
                return null;
            Type st = obj.GetType();
            if (t.IsAssignableFrom(st) || st.IsSubclassOf(t)) {
                return obj;
            }
            else {
                try {
                    return Convert.ChangeType(obj, t);
                }
                catch {
                    return null;
                }
            }
        }
    }
    public abstract class SimpleExpressionBase : AbstractExpression
    {
        protected override object DoCalc()
        {
            var operands = new List<object>();
            for (int i = 0; i < m_Exps.Count; ++i) {
                var v = m_Exps[i].Calc();
                operands.Add(v);
            }
            var r = OnCalc(operands);
            return r;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Exps = exps;
            return true;
        }
        protected abstract object OnCalc(IList<object> operands);

        private IList<IExpression> m_Exps = null;
    }
    public class Interpreter
    {
        public Dsl.DslLogDelegation OnLog;
        public void Log(string fmt, params object[] args)
        {
            if (null != OnLog) {
                OnLog(string.Format(fmt, args));
            }
        }
        public void Log(object arg)
        {
            if (null != OnLog) {
                OnLog(string.Format("{0}", arg));
            }
        }
        public void Register(string name, IExpressionFactory api)
        {
            m_Apis.Add(name, api);
        }
        public bool Parse(string file)
        {
            Dsl.DslFile dslFile = new DslFile();
            if(dslFile.Load(file, OnLog)) {
                return Parse(dslFile);
            }
            return false;
        }
        public bool Parse(string content, string fileName)
        {
            Dsl.DslFile dslFile = new DslFile();
            if(dslFile.LoadFromString(content, fileName, OnLog)) {
                return Parse(dslFile);
            }
            return false;
        }
        public bool Parse(Dsl.DslFile file)
        {
            foreach (var info in file.DslInfos) {
                var func = info as Dsl.FunctionData;
                if (null == func || !func.IsHighOrder)
                    continue;
                var key = info.GetId();
                var name = func.LowerOrderFunction.GetParamId(0);
                var funcDefExp = new FuncDefExp();
                if (funcDefExp.Load(info, this)) {
                    m_Funcs.Add(name, funcDefExp);
                }
            }
            return true;
        }
        public object Call(string func, params object[] args)
        {
            IExpression funcExp;
            if (m_Funcs.TryGetValue(func, out funcExp)) {
                m_LocalStack.Push(new LocalInfo { Arguments = args });
                var r = funcExp.Calc();
                m_LocalStack.Pop();
                return r;
            }
            else {
                Console.WriteLine("Can't find main proc !");
                return -1;
            }
        }
        public IExpression Load(Dsl.ISyntaxComponent comp)
        {
            Dsl.ValueData valueData = comp as Dsl.ValueData;
            Dsl.FunctionData funcData = null;
            if (null != valueData) {
                int idType = valueData.GetIdType();
                if (idType == Dsl.ValueData.ID_TOKEN) {
                    string id = valueData.GetId();
                    if (id == "true" || id == "false") {
                        ConstGet constExp = new ConstGet();
                        constExp.Load(comp, this);
                        return constExp;
                    }
                    else {
                        NamedVarGet varExp = new NamedVarGet();
                        varExp.Load(comp, this);
                        return varExp;
                    }
                }
                else {
                    ConstGet constExp = new ConstGet();
                    constExp.Load(comp, this);
                    return constExp;
                }
            }
            else {
                funcData = comp as Dsl.FunctionData;
                if (null != funcData && funcData.HaveParam()) {
                    var callData = funcData;
                    string op = callData.GetId();
                    if (op == "=") {//赋值
                        IExpression exp = null;
                        string name = callData.GetParamId(0);
                        exp = new NamedVarSet();
                        if (null != exp) {
                            exp.Load(comp, this);
                        }
                        else {
                            //error
                            Log("Interpreter error, {0} line {1}", callData.ToScriptString(false), callData.GetLine());
                        }
                        return exp;
                    }
                }
            }
            IExpression ret = null;
            string expId = comp.GetId();
            if (null != funcData && !funcData.IsHighOrder && m_Funcs.ContainsKey(expId)) {
                ret = new FuncCallExp();
            }
            else {
                IExpressionFactory factory;
                if(m_Apis.TryGetValue(expId, out factory)) {
                    ret = factory.Create();
                }
            }
            if (null != ret) {
                if (!ret.Load(comp, this)) {
                    //error
                    Log("Interpreter error, {0} line {1}", comp.ToScriptString(false), comp.GetLine());
                }
            }
            else {
                //error
                Log("Interpreter error, {0} line {1}", comp.ToScriptString(false), comp.GetLine());
            }
            return ret;
        }
        public LocalInfo GetLocalInfo()
        {
            if (m_LocalStack.Count > 0)
                return m_LocalStack.Peek();
            else
                throw new Exception("fatal error !!!");
        }
        public Dictionary<string, object> GetGlobalInfo()
        {
            return m_GlobalVariables;
        }
        public Interpreter()
        {
            m_Apis.Add("arg", new ExpressionFactoryHelper<ArgGet>());
            Register("+", new ExpressionFactoryHelper<AddExp>());
            Register("-", new ExpressionFactoryHelper<SubExp>());
            Register("*", new ExpressionFactoryHelper<MulExp>());
            Register("/", new ExpressionFactoryHelper<DivExp>());
            Register("%", new ExpressionFactoryHelper<ModExp>());
            Register(">", new ExpressionFactoryHelper<GreatExp>());
            Register(">=", new ExpressionFactoryHelper<GreatEqualExp>());
            Register("<", new ExpressionFactoryHelper<LessExp>());
            Register("<=", new ExpressionFactoryHelper<LessEqualExp>());
            Register("==", new ExpressionFactoryHelper<EqualExp>());
            Register("!=", new ExpressionFactoryHelper<NotEqualExp>());
            Register("&&", new ExpressionFactoryHelper<AndExp>());
            Register("||", new ExpressionFactoryHelper<OrExp>());
            Register("!", new ExpressionFactoryHelper<NotExp>());
            Register("?", new ExpressionFactoryHelper<CondExp>());
            Register("if", new ExpressionFactoryHelper<IfExp>());
            Register("while", new ExpressionFactoryHelper<WhileExp>());
        }

        public class LocalInfo
        {
            public IList<object> Arguments = null;
            public Dictionary<string, object> LocalVariables = new Dictionary<string, object>();
        }
        private Stack<LocalInfo> m_LocalStack = new Stack<LocalInfo>();
        private Dictionary<string, object> m_GlobalVariables = new Dictionary<string, object>();
        private Dictionary<string, IExpressionFactory> m_Apis = new Dictionary<string, IExpressionFactory>();
        private Dictionary<string, IExpression> m_Funcs = new Dictionary<string, IExpression>();
    }
}
