using Grid = Microsoft.Maui.Controls.Grid;
using System.Data;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;


namespace MyExcelMAUIApp
{
    public partial class MainPage : ContentPage
    {
        int CountColumn = 15;
        int CountRow = 30;
        private Dictionary<string, Cell> _cells = new Dictionary<string, Cell>();

        public MainPage()
        {
            InitializeComponent();
            CreateGrid();
        }
        private void CreateGrid()
        {
            AddColumnsAndColumnLabels();
            AddRowsAndCellEntries();
        }

        private void AddColumnsAndColumnLabels()
        {

            for (int col = 0; col < CountColumn + 1; col++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                if (col > 0)
                {
                    var label = new Label
                    {
                        Text = GetColumnName(col),
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    };
                    Grid.SetRow(label, 0);
                    Grid.SetColumn(label, col);
                    grid.Children.Add(label);
                }
            }
        }

        private void AddColumnButton_Clicked(object sender, EventArgs e)
        {
            int newColumnIndex = grid.ColumnDefinitions.Count;
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var label = new Label
            {
                Text = GetColumnName(newColumnIndex),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, newColumnIndex);
            grid.Children.Add(label);

            for (int row = 1; row < CountRow + 1; row++)
            {
                var entry = new Entry
                {
                    Text = "",
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                entry.Focused += Entry_Focused;
                entry.Unfocused += Entry_Unfocused;
                Grid.SetRow(entry, row);
                Grid.SetColumn(entry, newColumnIndex);
                grid.Children.Add(entry);
            }

            UpdateRowHeights();
        }

        private void AddRowsAndCellEntries()
        {
            for (int row = 0; row < CountRow; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition());

                var label = new Label
                {
                    Text = (row + 1).ToString(),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, row + 1);
                Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                for (int col = 0; col < CountColumn; col++)
                {
                    var entry = new Entry
                    {
                        Text = "",
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    };
                    entry.Focused += Entry_Focused;
                    entry.Unfocused += Entry_Unfocused;
                    Grid.SetRow(entry, row + 1);
                    Grid.SetColumn(entry, col + 1);
                    grid.Children.Add(entry);
                }
            }

            UpdateColumnWidths();
        }

        private void UpdateRowHeights()
        {
            foreach (var row in grid.RowDefinitions)
            {
                row.Height = new GridLength(1, GridUnitType.Star);
            }
        }

        private void UpdateColumnWidths()
        {
            foreach (var col in grid.ColumnDefinitions)
            {
                col.Width = new GridLength(1, GridUnitType.Star);
            }
        }

        private void Entry_Focused(object sender, FocusEventArgs e)
        {
            var entry = (Entry)sender;
            var (cellRef, _) = GetCellReferenceAndText(entry);

            if (_cells.ContainsKey(cellRef))
            {
                entry.Text = _cells[cellRef].Expression;
            }
        }
        public (string cellRef, string expression) GetCellReferenceAndText(Entry entry)
        {
            var row = Grid.GetRow(entry);
            var col = Grid.GetColumn(entry);
            var cellRef = GetColumnName(col) + row.ToString();
            var expression = entry.Text;
            return (cellRef, expression);
        }

        private async void Entry_Unfocused(object sender, FocusEventArgs e)
        {
            var entry = (Entry)sender;
            var (cellRef, expression) = GetCellReferenceAndText(entry);

            if (_cells.ContainsKey(cellRef))
            {
                _cells[cellRef].Expression = expression;
            }
            else
            {
                _cells[cellRef] = new Cell(expression);
            }

            var uninitializedReferences = GetCellReferencesFromExpression(expression)
                .Where(refCell => !_cells.ContainsKey(refCell))
                .ToList();
            if (uninitializedReferences.Any())
            {
                await DisplayAlert("Неініціалізовані значення", $"Наведені нижче посилання на клітинки неініціалізовані: {string.Join(", ", uninitializedReferences)}. Ініціалізуйте їх перед використанням у обчисленнях.", "OK");
                return;
            }
            if (await CheckCircularDependency(cellRef, expression))
            {
                entry.Text = string.Empty; 
                return;
            }
            await Cell.UpdateCellDependenciesAsync(cellRef, expression, _cells, this, grid);
        }
        private async Task<bool> CheckCircularDependency(string cellRef, string expression)
        {
            var visitedCells = new HashSet<string>();
            var cellReferences = GetCellReferencesFromExpression(expression);

            foreach (var refCell in cellReferences)
            {
                if (await IsCircularReference(cellRef, refCell, _cells, visitedCells))
                {
                    await DisplayAlert("Циклічна залежність", $"Клітинка {cellRef} не може посилатися на клітинку {refCell} через циклічну залежність.", "OK");
                    return true; 
                }
            }

            return false; 
        }
        private async Task<bool> IsCircularReference(string currentCell, string referencedCell, Dictionary<string, Cell> cells, HashSet<string> visitedCells)
        {
            if (visitedCells.Contains(referencedCell))
            {
                return true; 
            }

            visitedCells.Add(referencedCell);

            if (cells.ContainsKey(referencedCell))
            {
                var referencedExpression = cells[referencedCell].Expression;
                var cellReferences = GetCellReferencesFromExpression(referencedExpression);

                foreach (var refCell in cellReferences)
                {
                    if (await IsCircularReference(currentCell, refCell, cells, visitedCells))
                    {
                        return true; 
                    }
                }
            }

            visitedCells.Remove(referencedCell);
            return false; 
        }

        private List<string> GetCellReferencesFromExpression(string expression)
        {
            var cellReferences = new List<string>();
            var regex = new Regex(@"[A-Z]+\d+");
            var matches = regex.Matches(expression);

            foreach (Match match in matches)
            {
                cellReferences.Add(match.Value);
            }
            return cellReferences;
        }

        private async void CalculateButton_Clicked(object sender, EventArgs e)
        {
            foreach (var entry in grid.Children.OfType<Entry>())
            {
                var (cellRef, expression) = GetCellReferenceAndText(entry);
                if (string.IsNullOrWhiteSpace(expression))
                {
                    continue;
                }

                var uninitializedReferences = GetCellReferencesFromExpression(expression)
                    .Where(refCell => !_cells.ContainsKey(refCell))
                    .ToList();

                if (uninitializedReferences.Any())
                {
                    await DisplayAlert("Неініціалізовані значення", $"Наведені нижче посилання на клітинки неініціалізовані: {string.Join(", ", uninitializedReferences)}. Ініціалізуйте їх перед використанням у обчисленнях.", "OK");
                    continue;
                }

                try
                {
                    double result = EvaluateExpression(expression);
                    if (_cells.ContainsKey(cellRef))
                    {
                        _cells[cellRef].Value = result;
                        entry.Text = result.ToString();
                        await Cell.UpdateCellDependenciesAsync(cellRef, expression, _cells, this,grid);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка при обрахунку клітинки {cellRef}: {ex.Message}");
                }
            }
            Cell.UpdateDependentCells(_cells, grid, this);
        }


        private double EvaluateExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return 0;

            var evaluator = new SimpleExpressionEvaluator(_cells);
            return evaluator.Evaluate(expression);
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            var saveFileResult = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Виберіть місце для збереження CSV",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.Android, new[] { ".csv" } },
            { DevicePlatform.iOS, new[] { ".csv" } },
            { DevicePlatform.WinUI, new[] { ".csv" } },
            { DevicePlatform.MacCatalyst, new[] { ".csv" } }
        })
            });

            if (saveFileResult != null)
            {
                string folderPath = Path.GetDirectoryName(saveFileResult.FullPath);
                string fileName = Path.GetFileName(saveFileResult.FullPath);
                string filePath = Path.Combine(folderPath, fileName);

                if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    filePath = Path.ChangeExtension(filePath, ".csv");
                }

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    using (var writer = new StreamWriter(stream))
                    {
                        for (int row = 1; row <= CountRow; row++)
                        {
                            for (int col = 1; col <= CountColumn; col++)
                            {
                                string cellRef = GetColumnName(col) + row;

                                if (_cells.TryGetValue(cellRef, out var cell))
                                {
                                    if (!string.IsNullOrEmpty(cell.Expression))
                                    {
                                        // Якщо вираз містить кому, обгорнути його в лапки
                                        string expression = cell.Expression.Contains(",") ? $"\"{cell.Expression}\"" : cell.Expression;
                                        writer.Write(expression);
                                    }
                                    else if (cell.Value.HasValue)
                                    {
                                        writer.Write(cell.Value.Value.ToString(CultureInfo.InvariantCulture));
                                    }
                                }
                                else
                                {
                                    writer.Write("");
                                }

                                if (col < CountColumn)
                                {
                                    writer.Write(",");
                                }
                            }
                            writer.WriteLine();
                        }
                    }

                    await DisplayAlert("Успіх", $"Таблицю успішно збережено в {filePath}.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Помилка", $"Помилка при збереженні таблиці: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Скасовано", "Збереження таблиці скасовано.", "OK");
            }
        }


        private async void ReadButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Виберіть CSV файл",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { ".csv" } },
                { DevicePlatform.iOS, new[] { ".csv" } },
                { DevicePlatform.WinUI, new[] { ".csv" } },
                { DevicePlatform.MacCatalyst, new[] { ".csv" } }
            })
                });

                if (result != null)
                {
                    var stream = await result.OpenReadAsync();
                    using (var reader = new StreamReader(stream))
                    {
                        string line;
                        int row = 1;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            var values = SplitCsvLine(line);

                            for (int col = 0; col < values.Count; col++)
                            {
                                string cellRef = GetColumnName(col + 1) + row;

                                if (_cells.ContainsKey(cellRef))
                                {
                                    if (double.TryParse(values[col], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                                    {
                                        _cells[cellRef].Value = value;
                                        _cells[cellRef].Expression = null;
                                    }
                                    else
                                    {
                                        _cells[cellRef].Expression = values[col];
                                        _cells[cellRef].Value = null;
                                    }
                                }
                                else
                                {
                                    if (double.TryParse(values[col], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                                    {
                                        _cells[cellRef] = new Cell(value.ToString());
                                    }
                                    else
                                    {
                                        _cells[cellRef] = new Cell(values[col]);
                                    }
                                }

                                var entry = grid.Children.OfType<Entry>()
                                    .FirstOrDefault(e => Grid.GetRow(e) == row && Grid.GetColumn(e) == (col + 1));
                                if (entry != null)
                                {
                                    entry.Text = values[col];
                                }
                            }
                            row++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", "Сталася помилка: " + ex.Message, "OK");
            }
        }

        // Метод для поділу CSV-рядка з урахуванням ком і лапок
        private List<string> SplitCsvLine(string line)
        {
            var values = new List<string>();
            var sb = new StringBuilder();
            bool insideQuotes = false;

            foreach (var ch in line)
            {
                if (ch == '\"')
                {
                    insideQuotes = !insideQuotes; // Змінюємо стан всередині лапок
                }
                else if (ch == ',' && !insideQuotes)
                {
                    values.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(ch);
                }
            }

            if (sb.Length > 0)
            {
                values.Add(sb.ToString().Trim());
            }

            return values;
        }

        private async void ExitButton_Clicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні");
            if (answer)
            {
                System.Environment.Exit(0);
            }
        }

        private async void HelpButton_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Довідка", "Лабораторна робота 1. Студента Яценка Іллі", "OK");
        }
        private async void DeleteRowButton_Clicked(object sender, EventArgs e)
        {
            string input = await DisplayPromptAsync("Видалення рядка", "Введіть номер рядка для видалення (1, 2, ...):");
            if (string.IsNullOrEmpty(input))
            {
                return;
            }
            if (!int.TryParse(input, out int rowIndex) || rowIndex < 1 || rowIndex > CountRow)
            {
                await DisplayAlert("Помилка", "Неправильний номер рядка.", "OK");
                return;
            }

            rowIndex--;

            if (IsRowReferenced(rowIndex))
            {
                await DisplayAlert("Помилка", "Неможливо видалити рядок, оскільки його клітинки використовуються в інших клітинках.", "OK");
                return;
            }

            grid.RowDefinitions.RemoveAt(rowIndex + 1);

            List<string> cellReferencesToRemove = new List<string>();
            for (int col = 0; col < CountColumn; col++)
            {
                string cellRef = GetColumnName(col) + (rowIndex + 1);
                cellReferencesToRemove.Add(cellRef);
            }

            var childrenToRemove = grid.Children.Where(c => grid.GetRow(c) == rowIndex + 1).ToList();
            foreach (var child in childrenToRemove)
            {
                grid.Children.Remove(child);
            }
            foreach (var cellRef in cellReferencesToRemove)
            {
                if (_cells.ContainsKey(cellRef))
                {
                    _cells.Remove(cellRef);
                }
            }

            foreach (var child in grid.Children.OfType<View>())
            {
                int currentRow = Grid.GetRow(child);
                if (currentRow > rowIndex + 1)
                {
                    Grid.SetRow(child, currentRow - 1);
                }
            }
            CountRow--;
            UpdateRowHeaders();
        }

        private void UpdateRowHeaders()
        {
            for (int row = 0; row < CountRow; row++)
            {
                var label = grid.Children.OfType<Label>().FirstOrDefault(l => Grid.GetColumn(l) == 0 && Grid.GetRow(l) == row + 1); // Adjusted to start from row 1
                if (label != null)
                {
                    label.Text = (row + 1).ToString();
                }
            }
        }
        private async void DeleteColumnButton_Clicked(object sender, EventArgs e)
        {
            string input = await DisplayPromptAsync("Видалити стовпець", "Введіть букву стовпця для видалення (A, B, ...):");
            if (string.IsNullOrEmpty(input))
            {
                return;
            }
            int colIndex = GetColumnIndex(input.ToUpper());

            if (colIndex < 0 || colIndex >= CountColumn)
            {
                await DisplayAlert("Помилка", "Недійсна буква стовпця.", "OK");
                return;
            }
            if (IsColumnReferenced(colIndex))
            {
                await DisplayAlert("Помилка", "Неможливо видалити стовпець, оскільки його клітинки використовуються в інших клітинках.", "OK");
                return;
            }

            for (int row = 1; row <= CountRow; row++)
            {
                string cellRef = GetColumnName(colIndex) + row;
                _cells.Remove(cellRef);
            }

            var childrenToRemove = grid.Children.Where(c => grid.GetColumn(c) == colIndex).ToList();
            foreach (var child in childrenToRemove)
            {
                grid.Children.Remove(child);
            }

            grid.ColumnDefinitions.RemoveAt(colIndex);

            foreach (var child in grid.Children.OfType<View>())
            {
                int currentCol = Grid.GetColumn(child);
                if (currentCol > colIndex)
                {
                    Grid.SetColumn(child, currentCol - 1);
                }
            }

            CountColumn--;
            UpdateColumnHeaders();
        }

        private void UpdateColumnHeaders()
        {
            for (int col = 0; col < CountColumn; col++)
            {
                var label = grid.Children.OfType<Label>().FirstOrDefault(l => Grid.GetRow(l) == 0 && Grid.GetColumn(l) == col);
                if (label != null)
                {
                    label.Text = GetColumnName(col);
                }
            }
        }

        private void AddRowButton_Clicked(object sender, EventArgs e)
        {

            int newRow = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition());


            var label = new Label
            {
                Text = newRow.ToString(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,

            };
            Grid.SetRow(label, newRow);
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);


            for (int col = 1; col < CountColumn; col++)
            {
                var entry = new Entry
                {
                    Text = "",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                };
                entry.Unfocused += Entry_Unfocused;
                Grid.SetRow(entry, newRow);
                Grid.SetColumn(entry, col);
                grid.Children.Add(entry);
            }
        }
        private int GetColumnIndex(string column)
        {
            return Cell.GetColumnIndex(column);
        }

        private string GetColumnName(int colIndex)
        {
            return Cell.GetColumnName(colIndex);
        }
        private bool IsRowReferenced(int rowIndex)
        {
            return Cell.IsRowReferenced(rowIndex, _cells);
        }
        private bool IsColumnReferenced(int colIndex)
        {
            return Cell.IsColumnReferenced(colIndex, _cells);
        }
    }
}
