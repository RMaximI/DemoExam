using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Npgsql;
using BetterDemoExam;
using System.Windows.Data;

namespace DemoExam
{
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public ObservableCollection<Partner> Partners { get; set; }

		private Partner _selectedPartner;
		public Partner SelectedPartner
		{
			get => _selectedPartner;
			set
			{
				_selectedPartner = value;
				Console.WriteLine($"Выбран партнёр: {_selectedPartner.Name}");
				OnPropertyChanged();
			}
		}

		public MainWindow()
		{
			DbConnectionManager.Initialize();

			InitializeComponent();
			Partners = new ObservableCollection<Partner>();
			DataContext = this;
			LoadData();
		}

		private void LoadData()
		{
			Partners.Clear(); // Очистка списка перед загрузкой новых данных

			try
			{
				using var command = DbConnectionManager.Command(
					@"
					SELECT 
						p.partner_type || ' | ' || p.partner_name AS Name,
						p.director_name AS Director,
						p.partner_phone AS Phone,
						p.partner_rating AS Rating,
						p.partner_inn AS Inn,
						COALESCE(SUM(pp.product_quantity), 0) AS TotalQuantity
					FROM Partners p
					LEFT JOIN partner_products pp ON p.partner_inn = pp.partner_inn
					GROUP BY p.partner_inn, p.partner_type, p.partner_name, p.director_name, p.partner_phone, p.partner_rating
					"
				);


				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					Partners.Add(new Partner
					{
						Name = reader["Name"].ToString()!,
						Director = reader["Director"].ToString()!,
						Phone = "+7 " + reader["Phone"].ToString()!,
						Rating = reader["Rating"].ToString(),
						Discount = CalculateDiscount(Convert.ToInt64(reader["TotalQuantity"])),
						Inn = reader["Inn"].ToString()!
					});
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
			}
		}

		private string CalculateDiscount(long totalQuantity)
		{
			try
			{
				if (totalQuantity <= 10000) return "0%";
				if (totalQuantity <= 50000) return "5%";
				if (totalQuantity <= 300000) return "10%";
				return "15%";
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка расчета скидки: {ex.Message}");
				return "0%";
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void ListViewScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			ScrollViewer scv = (ScrollViewer)sender;
			scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
			e.Handled = true;
		}

		private void addPartner_Button_Click(object sender, RoutedEventArgs e)
		{
			AddPartner addPartner = new AddPartner();
			addPartner.OnDataAdded += LoadData; // Подписка на событие обновления данных
			addPartner.ShowDialog(); // Ждём закрытия окна перед обновлением данных
		}



		private void editButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedPartner != null)
			{
				var editPartner = new EditPartner(SelectedPartner.Inn);
				editPartner.OnDataUpdated += LoadData; // Подписка на событие обновления данных
				editPartner.ShowDialog(); // Ждём закрытия окна перед обновлением данных
			}
			else
			{
				MessageBox.Show("Партнер не выбран!");
			}
		}

		private void partnerHistory_Button_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedPartner != null)
			{
				var historyPartner = new HistoryWindow(SelectedPartner.Inn);
				historyPartner.ShowDialog();
			}
			else
			{
				MessageBox.Show("Партнер не выбран!");
			}
		}

		private void personalSale_Button_Click(object sender, RoutedEventArgs e)
		{
			new PersonalSaleWindow().ShowDialog();
		}



        private void deletePartner_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPartner != null)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить {SelectedPartner.Name}?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var command = DbConnectionManager.Command(
                            "DELETE FROM Partners WHERE partner_inn = @Inn"
                        );
                        command.Parameters.AddWithValue("@Inn", SelectedPartner.Inn);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Partners.Remove(SelectedPartner);
                            MessageBox.Show("Партнер успешно удален!");
                        }
                        else
                        {
                            MessageBox.Show("Ошибка: партнер не найден в базе данных.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении партнера: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите партнера для удаления!");
            }
        }

    }

    public class Partner
	{
		public string Name { get; set; }
		public string Director { get; set; }
		public string Phone { get; set; }
		public string Rating { get; set; }
		public string Discount { get; set; }
		public string Inn { get; set; }
	}
}
