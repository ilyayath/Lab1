using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabCalculator
{
    public static class Calculator
    {
        public static double Evaluate(string expression)
        {
            try
            {
                var lexer = new Lab1.LabCalculatorLexer(new AntlrInputStream(expression));
                lexer.RemoveErrorListeners();
                lexer.AddErrorListener(new ThrowExceptionErrorListener());

                var tokens = new CommonTokenStream(lexer);
                var parser = new Lab1.LabCalculatorParser(tokens);

                var tree = parser.compileUnit();

                var visitor = new LabCalculatorVisitor();

                return visitor.Visit(tree);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при обчислені виразу: {ex.Message}");
                throw;
            }
        }
    }
}
