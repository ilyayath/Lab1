using System.Text.RegularExpressions;
using Grid = Microsoft.Maui.Controls.Grid;
using MyExcelMAUIApp;

public class Cell
{
    public string Expression { get; set; }
    public double? Value { get; set; }
    public List<string> DependentCells { get; set; } = new List<string>();

    private static HashSet<string> calculatedCells = new HashSet<string>();

    public Cell(string expression)
    {
        Expression = expression;
        Value = null;
    }
    public Cell(double value)
    {
        Value = value;
        Expression = null;
    }

    public static int GetColumnIndex(string columnName)
    {
        int index = 0;
        for (int i = 0; i < columnName.Length; i++)
        {
            index *= 26;
            index += (columnName[i] - 'A' + 1);
        }
        return index;
    }

    public static string GetColumnName(int colIndex)
    {
        int dividend = colIndex;
        string columnName = string.Empty;
        while (dividend > 0)
        {
            int modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }
        return columnName;
    }

    public static bool IsRowReferenced(int rowIndex, Dictionary<string, Cell> cells)
    {
        foreach (var cell in cells.Values)
        {
            if (cell.Expression.Contains((rowIndex + 1).ToString()))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsColumnReferenced(int colIndex, Dictionary<string, Cell> cells)
    {
        string columnLetter = GetColumnName(colIndex);
        foreach (var cell in cells.Values)
        {
            if (cell.Expression.Contains(columnLetter))
            {
                return true;
            }
        }
        return false;
    }

    public static List<string> ExtractCellReferences(string expression)
    {
        var references = new List<string>();
        var matches = Regex.Matches(expression, @"[A-Z]+\d+");
        foreach (Match match in matches)
        {
            references.Add(match.Value);
        }
        return references;
    }

    public static async Task UpdateCellDependenciesAsync(string cellRef, string expression, Dictionary<string, Cell> cells, MainPage mainPage, Grid grid)
    {
        var cell = cells[cellRef];
        var references = ExtractCellReferences(expression);

        foreach (var refCell in references)
        {
            if (refCell == cellRef || IsCircularReference(cellRef, refCell, cells))
            {
                await mainPage.DisplayAlert("Помилка", $"Клітинка {cellRef} не може містити циклічні посилання або посилатися на похідні клітинки.", "ОК");
                cell.Expression = string.Empty;
                cell.Value = double.NaN;
                return;
            }
        }

        foreach (var dependentCell in cell.DependentCells)
        {
            if (cells.ContainsKey(dependentCell))
            {
                cells[dependentCell].DependentCells.Remove(cellRef);
            }
        }
        cell.DependentCells.Clear();

        foreach (var refCell in references)
        {
            if (cells.ContainsKey(refCell))
            {
                cells[refCell].DependentCells.Add(cellRef);
            }
        }

        UpdateDependentCells(cells, grid, mainPage);
    }

    private static bool IsCircularReference(string startCellRef, string currentRef, Dictionary<string, Cell> cells)
    {
        if (currentRef == startCellRef) return true;
        if (!cells.ContainsKey(currentRef)) return false;

        var currentCell = cells[currentRef];
        foreach (var dependent in currentCell.DependentCells)
        {
            if (IsCircularReference(startCellRef, dependent, cells)) return true;
        }
        return false;
    }

    public static void UpdateDependentCells(Dictionary<string, Cell> cells, Grid grid, MainPage mainPage)
    {
        var sortedCells = TopologicalSortCells(cells);
        var processedCells = new HashSet<string>();

        foreach (var cellRef in sortedCells)
        {
            if (!processedCells.Contains(cellRef))
            {
                UpdateCellValue(cellRef, cells, grid, mainPage);
                processedCells.Add(cellRef);
            }
        }
    }

    private static List<string> TopologicalSortCells(Dictionary<string, Cell> cells)
    {
        var visited = new HashSet<string>();
        var result = new List<string>();

        foreach (var cell in cells.Keys)
        {
            if (!visited.Contains(cell))
            {
                TopologicalSortVisit(cell, cells, visited, result);
            }
        }

        result.Reverse();
        return result;
    }

    private static void TopologicalSortVisit(string cellRef, Dictionary<string, Cell> cells, HashSet<string> visited, List<string> result)
    {
        visited.Add(cellRef);

        foreach (var dependentCellRef in cells[cellRef].DependentCells)
        {
            if (!visited.Contains(dependentCellRef))
            {
                TopologicalSortVisit(dependentCellRef, cells, visited, result);
            }
        }

        result.Add(cellRef);
    }

    public static void UpdateCellValue(string cellRef, Dictionary<string, Cell> cells, Grid grid, MainPage mainPage)
    {
        if (!cells.ContainsKey(cellRef)) return;

        var cell = cells[cellRef];
        if (!string.IsNullOrWhiteSpace(cell.Expression))
        {
            cell.Value = EvaluateExpression(cell.Expression, cells);
            var entry = grid.Children.OfType<Entry>()
                .FirstOrDefault(e => mainPage.GetCellReferenceAndText(e).cellRef == cellRef);
            if (entry != null)
            {
                entry.Text = cell.Value.ToString();
            }
        }
    }

    public static double EvaluateExpression(string expression, Dictionary<string, Cell> cells)
    {
        if (string.IsNullOrWhiteSpace(expression)) return 0;
        if (Regex.IsMatch(expression, @"--{2,}"))
        {
            Application.Current.MainPage.DisplayAlert("Помилка", "Неправильний вираз: більше одного мінуса підряд", "ОК");
            return double.NaN;
        }
        try
        {
            var evaluator = new SimpleExpressionEvaluator(cells);
            return evaluator.Evaluate(expression);
        }
        catch (Exception ex)
        {
            Application.Current.MainPage.DisplayAlert("Помилка", $"Неправильний вираз в клітинці: {ex.Message}", "ОК");
            return double.NaN;
        }
    }
}
    