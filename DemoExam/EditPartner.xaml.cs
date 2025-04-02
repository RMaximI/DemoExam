using System.Text.RegularExpressions;
using System.Windows;
using BetterDemoExam;
using Npgsql;

namespace DemoExam
{
	public partial class EditPartner : Window
	{
		public event Action OnDataUpdated;
		private string partnerId; // Используем partner_id вместо partner_inn

		public EditPartner(string id)
		{
			partnerId = id;

			InitializeComponent();
			LoadPartnerTypes(); // Загружаем фиксированный список типов
			LoadPartnerData();  // Загружаем данные о партнёре
		}

		private void LoadPartnerTypes()
		{
			// Добавляем фиксированные типы организаций в ComboBox
			typeComboBox.Items.Add("ЗАО");
			typeComboBox.Items.Add("ООО");
			typeComboBox.Items.Add("ПАО");
			typeComboBox.Items.Add("ОАО");
			typeComboBox.Items.Add("АО");
		}

		private void LoadPartnerData()
		{
			try
			{
				using var command = DbConnectionManager.Command(@"
					SELECT partner_name, partner_type, partner_rating, partner_address, 
						director_name, partner_phone, partner_email, partner_inn 
					FROM public.partners 
					WHERE partner_inn = @id
				");

				command.Parameters.AddWithValue("@id", partnerId);

				using var reader = command.ExecuteReader();
				if (reader.Read())
				{
					nameTextBox.Text = reader["partner_name"].ToString();
					string partnerType = reader["partner_type"].ToString()!;
					typeComboBox.SelectedItem = typeComboBox.Items.Contains(partnerType) ? partnerType : null; // Выбираем текущий тип

					ratingTextBox.Text = reader["partner_rating"].ToString();
					addressTextBox.Text = reader["partner_address"].ToString();
					directorTextBox.Text = reader["director_name"].ToString();
					phoneTextBox.Text = reader["partner_phone"].ToString();
					emailTextBox.Text = reader["partner_email"].ToString();
				}
				else
				{
					MessageBox.Show("Ошибка: партнёр не найден.");
					this.Close();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
			}
		}

        private string forbiddenPattern = @"[@/\\+]";
        private bool ContainsForbiddenChars(string input)
        {
            return Regex.IsMatch(input, forbiddenPattern);
        }

        private void save_button_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult result = MessageBox.Show(
				"Вы действительно хотите сохранить изменения?",
				"Сохранение",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question
			);

			if (result == MessageBoxResult.No) return;

            if (string.IsNullOrWhiteSpace(nameTextBox.Text) ||
               typeComboBox.SelectedItem == null ||
               string.IsNullOrWhiteSpace(ratingTextBox.Text) ||
               string.IsNullOrWhiteSpace(addressTextBox.Text) ||
               string.IsNullOrWhiteSpace(directorTextBox.Text) ||
               string.IsNullOrWhiteSpace(phoneTextBox.Text) ||
               string.IsNullOrWhiteSpace(emailTextBox.Text))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ContainsForbiddenChars(nameTextBox.Text))
            {
                MessageBox.Show("Название компании не должно содержать символы: @ / \\ +", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ratingTextBox.Text.All(char.IsDigit))
            {
                MessageBox.Show("Рейтинг должен содержать только числа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Проверка валидности рейтинга
            if (!int.TryParse(ratingTextBox.Text, out int rating) || (rating < 0 || rating > 10))
            {
                MessageBox.Show("Рейтинг должен быть числом от 0 до 10!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ContainsForbiddenChars(addressTextBox.Text))
            {
                MessageBox.Show("Адрес не должен содержать символы: @ / \\ +", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            if (ContainsForbiddenChars(directorTextBox.Text))
            {
                MessageBox.Show("ФИО не должно содержать символы: @ / \\ +", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string phoneNumber = phoneTextBox.Text.Trim(); // Убираем пробелы

            // Проверяем, состоит ли номер только из цифр
            if (!phoneNumber.All(char.IsDigit))
            {
                MessageBox.Show("Номер должен содержать только цифры!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем количество цифр после +7 или 8
            if (phoneNumber.Length == 10 || (phoneNumber.Length == 11 && phoneNumber.StartsWith("8")))
            {
                // Номер корректный (он уже 10-значный, значит после +7)
               phoneNumber = phoneNumber.Substring(1);
            }
            else
            {
				MessageBox.Show("Неверный формат телефона!\nВведите 10 цифр (без +7) или 11 цифр, если начали с 8", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
            }

            string email = emailTextBox.Text.Trim(); // Убираем пробелы по краям

            string pattern = @"^[^\s@]+@[^\s@]+\.[rR][uU]$";

            if (!Regex.IsMatch(email, pattern))
            {
				MessageBox.Show("Неверный формат email!\nВведите email в формате user@domain.ru", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
            }

            try
			{
				using var command = DbConnectionManager.Command(@"
					UPDATE public.partners 
					SET partner_name = @name, partner_type = @type, partner_rating = @rating, partner_address = @address, 
						director_name = @director, partner_phone = @phone, partner_email = @email
					WHERE partner_inn = @id
				");
				
				command.Parameters.AddWithValue("@name", nameTextBox.Text);
				command.Parameters.AddWithValue("@type", typeComboBox.SelectedItem?.ToString()!); // Получаем выбранное значение
				command.Parameters.AddWithValue("@rating", Convert.ToInt32(ratingTextBox.Text));
				command.Parameters.AddWithValue("@address", addressTextBox.Text);
				command.Parameters.AddWithValue("@director", directorTextBox.Text);
				command.Parameters.AddWithValue("@phone", phoneTextBox.Text);
				command.Parameters.AddWithValue("@email", emailTextBox.Text);
				command.Parameters.AddWithValue("@id", partnerId); // Используем partner_id

				int rowsAffected = command.ExecuteNonQuery();
				if (rowsAffected > 0)
				{
					MessageBox.Show("Данные успешно обновлены.");
					OnDataUpdated?.Invoke(); // Вызов события обновления
					this.Close();
				}
				else
				{
					MessageBox.Show("Ошибка при обновлении данных.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка сохранения данных: {ex.Message}");
			}
		}

		private void back_button_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
