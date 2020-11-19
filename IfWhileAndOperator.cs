using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dsl;

namespace TestDsl
{
    internal sealed class IfExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            object v = 0;
            for (int ix = 0; ix < m_Clauses.Count; ++ix) {
                var clause = m_Clauses[ix];
                if (null != clause.Condition) {
                    var condVal = clause.Condition.Calc();
                    if (CastTo<long>(condVal) != 0) {
                        for (int index = 0; index < clause.Expressions.Count; ++index) {
                            v = clause.Expressions[index].Calc();
                        }
                        break;
                    }
                }
                else if (ix == m_Clauses.Count - 1) {
                    for (int index = 0; index < clause.Expressions.Count; ++index) {
                        v = clause.Expressions[index].Calc();
                    }
                    break;
                }
            }
            return v;
        }
        protected override bool Load(Dsl.FunctionData funcData)
        {
            if (funcData.IsHighOrder) {
                Dsl.ISyntaxComponent cond = funcData.LowerOrderFunction.GetParam(0);
                IfExp.Clause item = new IfExp.Clause();
                item.Condition = Interpreter.Load(cond);
                for (int ix = 0; ix < funcData.GetParamNum(); ++ix) {
                    IExpression subExp = Interpreter.Load(funcData.GetParam(ix));
                    item.Expressions.Add(subExp);
                }
                m_Clauses.Add(item);
            }
            else {
                //error
                Interpreter.Log("Interpreter error, {0} line {1}", funcData.ToScriptString(false), funcData.GetLine());
            }
            return true;
        }
        protected override bool Load(Dsl.StatementData statementData)
        {
            //简化语法if(exp) func(args);语法的处理
            int funcNum = statementData.GetFunctionNum();
            if (funcNum == 2) {
                var first = statementData.First;
                var second = statementData.Second;
                var firstId = first.GetId();
                var secondId = second.GetId();
                if (firstId == "if" && !first.HaveStatement() && !first.HaveExternScript() &&
                        !string.IsNullOrEmpty(secondId) && !second.HaveStatement() && !second.HaveExternScript()) {
                    IfExp.Clause item = new IfExp.Clause();
                    if (first.GetParamNum() > 0) {
                        Dsl.ISyntaxComponent cond = first.GetParam(0);
                        item.Condition = Interpreter.Load(cond);
                    }
                    else {
                        //error
                        Interpreter.Log("Interpreter error, {0} line {1}", first.ToScriptString(false), first.GetLine());
                    }
                    IExpression subExp = Interpreter.Load(second);
                    item.Expressions.Add(subExp);
                    m_Clauses.Add(item);
                    return true;
                }
            }
            //标准if语句的处理
            foreach (var fData in statementData.Functions) {
                if (fData.GetId() == "if" || fData.GetId() == "elseif") {
                    IfExp.Clause item = new IfExp.Clause();
                    if (fData.IsHighOrder && fData.LowerOrderFunction.GetParamNum() > 0) {
                        Dsl.ISyntaxComponent cond = fData.LowerOrderFunction.GetParam(0);
                        item.Condition = Interpreter.Load(cond);
                    }
                    else {
                        //error
                        Interpreter.Log("Interpreter error, {0} line {1}", fData.ToScriptString(false), fData.GetLine());
                    }
                    for (int ix = 0; ix < fData.GetParamNum(); ++ix) {
                        IExpression subExp = Interpreter.Load(fData.GetParam(ix));
                        item.Expressions.Add(subExp);
                    }
                    m_Clauses.Add(item);
                }
                else if (fData.GetId() == "else") {
                    if (fData != statementData.Last) {
                        //error
                        Interpreter.Log("Interpreter error, {0} line {1}", fData.ToScriptString(false), fData.GetLine());
                    }
                    else {
                        IfExp.Clause item = new IfExp.Clause();
                        for (int ix = 0; ix < fData.GetParamNum(); ++ix) {
                            IExpression subExp = Interpreter.Load(fData.GetParam(ix));
                            item.Expressions.Add(subExp);
                        }
                        m_Clauses.Add(item);
                    }
                }
                else {
                    //error
                    Interpreter.Log("Interpreter error, {0} line {1}", fData.ToScriptString(false), fData.GetLine());
                }
            }
            return true;
        }

        private sealed class Clause
        {
            internal IExpression Condition;
            internal List<IExpression> Expressions = new List<IExpression>();
        }

        private List<Clause> m_Clauses = new List<Clause>();
    }
    internal sealed class WhileExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            object v = 0;
            for (; ; ) {
                var condVal = m_Condition.Calc();
                if (CastTo<long>(condVal) != 0) {
                    for (int index = 0; index < m_Expressions.Count; ++index) {
                        v = m_Expressions[index].Calc();
                    }
                }
                else {
                    break;
                }
            }
            return v;
        }
        protected override bool Load(Dsl.FunctionData funcData)
        {
            if (funcData.IsHighOrder) {
                Dsl.ISyntaxComponent cond = funcData.LowerOrderFunction.GetParam(0);
                m_Condition = Interpreter.Load(cond);
                for (int ix = 0; ix < funcData.GetParamNum(); ++ix) {
                    IExpression subExp = Interpreter.Load(funcData.GetParam(ix));
                    m_Expressions.Add(subExp);
                }
            }
            else {
                //error
                Interpreter.Log("Interpreter error, {0} line {1}", funcData.ToScriptString(false), funcData.GetLine());
            }
            return true;
        }
        protected override bool Load(Dsl.StatementData statementData)
        {
            //简化语法while(exp) func(args);语法的处理
            if (statementData.GetFunctionNum() == 2) {
                var first = statementData.First;
                var second = statementData.Second;
                var firstId = first.GetId();
                var secondId = second.GetId();
                if (firstId == "while" && !first.HaveStatement() && !first.HaveExternScript() &&
                        !string.IsNullOrEmpty(secondId) && !second.HaveStatement() && !second.HaveExternScript()) {
                    if (first.GetParamNum() > 0) {
                        Dsl.ISyntaxComponent cond = first.GetParam(0);
                        m_Condition = Interpreter.Load(cond);
                    }
                    else {
                        //error
                        Interpreter.Log("Interpreter error, {0} line {1}", first.ToScriptString(false), first.GetLine());
                    }
                    IExpression subExp = Interpreter.Load(second);
                    m_Expressions.Add(subExp);
                    return true;
                }
            }
            return false;
        }

        private IExpression m_Condition;
        private List<IExpression> m_Expressions = new List<IExpression>();
    }
    internal sealed class AddExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            var v1 = m_Op1.Calc();
            var v2 = m_Op2.Calc();
            object v;
            if (v1 is string || v2 is string) {
                v = v1.ToString() + v2.ToString();
            }
            else {
                v = CastTo<double>(v1) + CastTo<double>(v2);
            }
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class SubExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            var v1 = m_Op1.Calc();
            var v2 = m_Op2.Calc();
            object v = CastTo<double>(v1) - CastTo<double>(v2);
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class MulExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            var v1 = m_Op1.Calc();
            var v2 = m_Op2.Calc();
            object v = CastTo<double>(v1) * CastTo<double>(v2);
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class DivExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            var v1 = m_Op1.Calc();
            var v2 = m_Op2.Calc();
            object v = CastTo<double>(v1) / CastTo<double>(v2);
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class ModExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            var v1 = m_Op1.Calc();
            var v2 = m_Op2.Calc();
            object v = CastTo<double>(v1) % CastTo<double>(v2);
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class GreatExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            double v1 = CastTo<double>(m_Op1.Calc());
            double v2 = CastTo<double>(m_Op2.Calc());
            object v = v1 > v2 ? 1 : 0;
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class GreatEqualExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            double v1 = CastTo<double>(m_Op1.Calc());
            double v2 = CastTo<double>(m_Op2.Calc());
            object v = v1 >= v2 ? 1 : 0;
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class LessExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            double v1 = CastTo<double>(m_Op1.Calc());
            double v2 = CastTo<double>(m_Op2.Calc());
            object v = v1 < v2 ? 1 : 0;
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class LessEqualExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            double v1 = CastTo<double>(m_Op1.Calc());
            double v2 = CastTo<double>(m_Op2.Calc());
            object v = v1 <= v2 ? 1 : 0;
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class EqualExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            var v1 = m_Op1.Calc();
            var v2 = m_Op2.Calc();
            object v = v1.ToString() == v2.ToString() ? 1 : 0;
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class NotEqualExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            var v1 = m_Op1.Calc();
            var v2 = m_Op2.Calc();
            object v = v1.ToString() != v2.ToString() ? 1 : 0;
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class AndExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            long v1 = CastTo<long>(m_Op1.Calc());
            long v2 = 0;
            object v = v1 != 0 && (v2 = CastTo<long>(m_Op2.Calc())) != 0 ? 1 : 0;
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class OrExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            long v1 = CastTo<long>(m_Op1.Calc());
            long v2 = 0;
            object v = v1 != 0 || (v2 = CastTo<long>(m_Op2.Calc())) != 0 ? 1 : 0;
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op1 = exps[0];
            m_Op2 = exps[1];
            return true;
        }

        private IExpression m_Op1;
        private IExpression m_Op2;
    }
    internal sealed class NotExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            long val = CastTo<long>(m_Op.Calc());
            object v = val == 0 ? 1 : 0;
            return v;
        }
        protected override bool Load(IList<IExpression> exps)
        {
            m_Op = exps[0];
            return true;
        }

        private IExpression m_Op;
    }
    internal sealed class CondExp : AbstractExpression
    {
        protected override object DoCalc()
        {
            object v1 = m_Op1.Calc();
            object v2 = null;
            object v3 = null;
            object v = CastTo<long>(v1) != 0 ? v2 = m_Op2.Calc() : v3 = m_Op3.Calc();
            return v;
        }
        protected override bool Load(Dsl.StatementData statementData)
        {
            Dsl.FunctionData funcData1 = statementData.First;
            Dsl.FunctionData funcData2 = statementData.Second;
            if (funcData1.IsHighOrder && funcData1.HaveLowerOrderParam() && funcData2.GetId() == ":" && funcData2.HaveParamOrStatement()) {
                Dsl.ISyntaxComponent cond = funcData1.LowerOrderFunction.GetParam(0);
                Dsl.ISyntaxComponent op1 = funcData1.GetParam(0);
                Dsl.ISyntaxComponent op2 = funcData2.GetParam(0);
                m_Op1 = Interpreter.Load(cond);
                m_Op2 = Interpreter.Load(op1);
                m_Op3 = Interpreter.Load(op2);
            }
            else {
                //error
                Interpreter.Log("Interpreter error, {0} line {1}", statementData.ToScriptString(false), statementData.GetLine());
            }
            return true;
        }

        private IExpression m_Op1 = null;
        private IExpression m_Op2 = null;
        private IExpression m_Op3 = null;
    }
}
