using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BetterDemoExam;
using Npgsql;

namespace DemoExam
{
	public partial class HistoryWindow : Window
	{
		public HistoryWindow(string currentInn)
		{
			InitializeComponent();
			LoadSalesHistory(currentInn);
		}

		private void LoadSalesHistory(string currentInn)
		{
			try
			{
				using var adapter = DbConnectionManager.DataAdapter($@"
					SELECT
						pr.product_name AS ""Наименование"",
						pp.product_quantity AS ""Количество"",
						pp.sale_date AS ""Дата""
					FROM public.partner_products pp
					JOIN public.products pr ON pp.product_article = pr.product_article
					WHERE pp.partner_inn = '{currentInn}'
					ORDER BY pp.sale_date DESC;"
				);

				DataTable dataTable = new DataTable();
				adapter.Fill(dataTable);

				sale_DataGrid.ItemsSource = dataTable.DefaultView;
				DrawSalesChart(dataTable);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void DrawSalesChart(DataTable dataTable)
		{
			salesChart.Children.Clear();

			if (dataTable.Rows.Count == 0) return;

			double width = salesChart.ActualWidth > 0 ? salesChart.ActualWidth : 600;
			double height = salesChart.ActualHeight > 0 ? salesChart.ActualHeight : 300;
			double padding = 40;

			// Группируем данные по дате и суммируем количество продаж
			var salesData = dataTable.AsEnumerable()
				.GroupBy(row => Convert.ToDateTime(row["Дата"]).Date)
				.Select(group => new
				{
					Date = group.Key,
					Quantity = group.Sum(row => Convert.ToInt32(row["Количество"]))
				})
				.OrderBy(item => item.Date)
				.ToList();

			if (salesData.Count == 0) return;

			// Находим диапазоны значений
			DateTime minDate = salesData.First().Date;
			DateTime maxDate = salesData.Last().Date;
			int minQuantity = salesData.Min(s => s.Quantity);
			int maxQuantity = salesData.Max(s => s.Quantity);

			double xScale = (width - 2 * padding) / (maxDate.Subtract(minDate).TotalDays + 1);
			double yScale = (height - 2 * padding) / (maxQuantity - minQuantity + 1);

			Polyline polyline = new Polyline
			{
				Stroke = Brushes.Blue,
				StrokeThickness = 2
			};

			// Рисуем точки и соединяем их линией
			foreach (var sale in salesData)
			{
				double x = padding + (sale.Date.Subtract(minDate).TotalDays * xScale);
				double y = height - padding - ((sale.Quantity - minQuantity) * yScale);

				// Добавляем точку на линию
				polyline.Points.Add(new System.Windows.Point(x, y));

				// Добавляем кружок для точки
				Ellipse ellipse = new Ellipse
				{
					Width = 6,
					Height = 6,
					Fill = Brushes.Red,
					Stroke = Brushes.Black,
					StrokeThickness = 1
				};

				Canvas.SetLeft(ellipse, x - 3);
				Canvas.SetTop(ellipse, y - 3);
				salesChart.Children.Add(ellipse);
			}

			salesChart.Children.Add(polyline);

			// Рисуем оси
			Line xAxis = new Line
			{
				X1 = padding,
				Y1 = height - padding,
				X2 = width - padding,
				Y2 = height - padding,
				Stroke = Brushes.Black,
				StrokeThickness = 2
			};

			Line yAxis = new Line
			{
				X1 = padding,
				Y1 = padding,
				X2 = padding,
				Y2 = height - padding,
				Stroke = Brushes.Black,
				StrokeThickness = 2
			};

			salesChart.Children.Add(xAxis);
			salesChart.Children.Add(yAxis);

			// Добавляем подписи к осям
			for (int i = 0; i < salesData.Count; i++)
			{
				var sale = salesData[i];

				double x = padding + (sale.Date.Subtract(minDate).TotalDays * xScale);
				double y = height - padding;

				TextBlock textBlock = new TextBlock
				{
					Text = sale.Date.ToString("dd.MM"),
					FontSize = 10
				};

				Canvas.SetLeft(textBlock, x - 10);
				Canvas.SetTop(textBlock, y + 5);
				salesChart.Children.Add(textBlock);
			}

			for (int i = 0; i < 5; i++)
			{
				int quantity = minQuantity + (maxQuantity - minQuantity) * i / 4;
				double y = height - padding - ((quantity - minQuantity) * yScale);

				TextBlock textBlock = new TextBlock
				{
					Text = quantity.ToString(),
					FontSize = 10
				};

				Canvas.SetLeft(textBlock, padding - 30);
				Canvas.SetTop(textBlock, y - 5);
				salesChart.Children.Add(textBlock);
			}
		}
		private void back_button_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}