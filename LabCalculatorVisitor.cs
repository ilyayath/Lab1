using Lab1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabCalculator
{
    class LabCalculatorVisitor : LabCalculatorBaseVisitor<double>
    {

        Dictionary<string, double> tableIdentifier = new Dictionary<string, double>();

        public override double VisitCompileUnit(LabCalculatorParser.CompileUnitContext context)
        {
            return Visit(context.expression());
        }

        public override double VisitNumberExpr(LabCalculatorParser.NumberExprContext context)
        {
            var result = double.Parse(context.GetText());
            Debug.WriteLine(result);
            return result;
        }

        public override double VisitIdentifierExpr(LabCalculatorParser.IdentifierExprContext context)
        {
            var result = context.GetText();
            double value;

            
            if (tableIdentifier.TryGetValue(result, out value))
            {
                return value;
            }
            else
            {
                return 0.0; 
            }
        }

        public override double VisitParenthesizedExpr(LabCalculatorParser.ParenthesizedExprContext context)
        {
            return Visit(context.expression());
        }

        public override double VisitExponentialExpr(LabCalculatorParser.ExponentialExprContext context)
        {
            var left = WalkLeft(context);
            var right = WalkRight(context);

            Debug.WriteLine("{0} ^ {1}", left, right);
            return System.Math.Pow(left, right);
        }

        public override double VisitAdditiveExpr(LabCalculatorParser.AdditiveExprContext context)
        {
            var left = WalkLeft(context);
            var right = WalkRight(context);

            if (context.operatorToken.Type == LabCalculatorLexer.ADD)
            {
                Debug.WriteLine("{0} + {1}", left, right);
                return left + right;
            }
            else // LabCalculatorLexer.SUBTRACT
            {
                Debug.WriteLine("{0} - {1}", left, right);
                return left - right;
            }
        }
        private bool hasShownDivisionByZeroAlert = false;
        public override double VisitMultiplicativeExpr(LabCalculatorParser.MultiplicativeExprContext context)
        {
            var left = WalkLeft(context);
            var right = WalkRight(context);

            if (context.operatorToken.Type == LabCalculatorLexer.MULTIPLY)
            {
                Debug.WriteLine("{0} * {1}", left, right);
                return left * right;
            }
            else 
            {
                if (right == 0)
                {
                    if (!hasShownDivisionByZeroAlert)
                    {
                        Application.Current.MainPage.DisplayAlert("Помилка", "Сьогодні на нуль не ділимо!", "ОК");
                        hasShownDivisionByZeroAlert = true; 
                    }
                    return double.NaN; 
                }

                Debug.WriteLine("{0} / {1}", left, right);
                hasShownDivisionByZeroAlert = false;
                return left / right;
            }

        }

        public override double VisitComparisonExpr(LabCalculatorParser.ComparisonExprContext context)
        {
            var left = WalkLeft(context);
            var right = WalkRight(context);

         
            switch (context.operatorToken.Type)
            {
                case LabCalculatorLexer.LESS:
                    return left < right ? 1 : 0; 
                case LabCalculatorLexer.GREATER:
                    return left > right ? 1 : 0; 
                case LabCalculatorLexer.EQUALS:
                    return left == right ? 1 : 0; 
                default:
                    throw new InvalidOperationException($"Unknown comparison operator: {context.operatorToken.Type}");
            }
        }

        public override double VisitNotExpr(LabCalculatorParser.NotExprContext context)
        {
            var value = Visit(context.expression());

            int intValue = (int)value;
            int result = ~intValue;
            return (double)result;
        }


        public override double VisitModExpr(LabCalculatorParser.ModExprContext context)
        {
            var left = WalkLeft(context);
            var right = WalkRight(context);
            return left % right; 
        }

        public override double VisitDivExpr(LabCalculatorParser.DivExprContext context)
        {
            var left = WalkLeft(context);
            var right = WalkRight(context);
            return Math.Floor(left / right); 
        }

        public override double VisitFunctionCallExpr(LabCalculatorParser.FunctionCallExprContext context)
        {
            string functionName = context.IDENTIFIER().GetText(); 
            List<double> arguments = new List<double>();

            
            if (context.argumentList() != null)
            {
                foreach (var arg in context.argumentList().expression())
                {
                    arguments.Add(Visit(arg)); 
                }
            }

            Debug.WriteLine($"Function: {functionName}, Arguments: {string.Join(", ", arguments)}");

           
            switch (functionName)
            {
                case "max":
                    return arguments.Max();
                case "min":
                    return arguments.Min();
                default:
                    throw new InvalidOperationException($"Unknown function: {functionName}");
            }
        }


        private double WalkLeft(LabCalculatorParser.ExpressionContext context)
        {
            return Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(0));
        }

        private double WalkRight(LabCalculatorParser.ExpressionContext context)
        {
            return Visit(context.GetRuleContext<LabCalculatorParser.ExpressionContext>(1));
        }
    }
}
