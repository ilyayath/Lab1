using LabCalculator;

public class SimpleExpressionEvaluator
{
    private Dictionary<string, Cell> _cells;

    public SimpleExpressionEvaluator(Dictionary<string, Cell> cells)
    {
        _cells = cells;
    }
    
    public double Evaluate(string expression)
    {
        foreach (var cell in _cells)
        {
            expression = expression.Replace(cell.Key, cell.Value.Value.ToString());
        }

        try
        {
            return Calculator.Evaluate(expression);
        }
        catch
        {
            throw new Exception("Помилка у виразі");
        }
    }
}