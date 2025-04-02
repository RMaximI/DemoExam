using System.Data.Common;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using BetterDemoExam;
using Npgsql;

namespace DemoExam
{
    public partial class AddPartner : Window
    {
        public event Action OnDataAdded; // Событие для обновления списка в MainWindow

        public AddPartner()
        {
            InitializeComponent();
            LoadPartnerTypes(); // Загружаем фиксированные типы
        }

        private void LoadPartnerTypes()
        {
            // Добавляем фиксированные типы организаций в ComboBox
            typeComboBox.Items.Add("ЗАО");
            typeComboBox.Items.Add("ООО");
            typeComboBox.Items.Add("ПАО");
            typeComboBox.Items.Add("ОАО");
            typeComboBox.SelectedIndex = -1; // Оставляем поле пустым (по умолчанию)
        }

        private string forbiddenPattern = @"[@/\\+]";
        private bool ContainsForbiddenChars(string input)
        {
            return Regex.IsMatch(input, forbiddenPattern);
        }


        private void save_button_Click(object sender, RoutedEventArgs e)
        {
            // Проверка на заполнение всех полей
            if (string.IsNullOrWhiteSpace(nameTextBox.Text) ||
                typeComboBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(ratingTextBox.Text) ||
                string.IsNullOrWhiteSpace(addressTextBox.Text) ||
                string.IsNullOrWhiteSpace(directorTextBox.Text) ||
                string.IsNullOrWhiteSpace(phoneTextBox.Text) ||
                string.IsNullOrWhiteSpace(emailTextBox.Text) ||
                string.IsNullOrWhiteSpace(innTextBox.Text))
            {
                MessageBox.Show("Все поля должны быть заполнены!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ContainsForbiddenChars(nameTextBox.Text))
            {
                MessageBox.Show("Название компании не должно содержать символы: @ / \\ +", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if(!ratingTextBox.Text.All(char.IsDigit))
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

            string innstr = innTextBox.Text.Trim();
            // Проверка ИНН (только цифры, 10 или 12 знаков)
            if (!long.TryParse(innstr, out long inn) || (innTextBox.Text.Length != 10 && innTextBox.Text.Length != 12))
            {
                MessageBox.Show("ИНН должен содержать 10 или 12 цифр!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!innstr.All(char.IsDigit))
            {
                MessageBox.Show("ИНН должен состять только из чисел!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var command = DbConnectionManager.Command(
                    @"INSERT INTO public.partners 
                    (partner_name, partner_type, partner_rating, partner_address, 
                    director_name, partner_phone, partner_email, partner_inn)
                    VALUES (@name, @type, @rating, @address, @director, @phone, @email, @inn)"
                );

                command.Parameters.AddWithValue("@name", nameTextBox.Text);
                command.Parameters.AddWithValue("@type", typeComboBox.SelectedItem.ToString()!); // Выбранный тип
                command.Parameters.AddWithValue("@rating", rating);
                command.Parameters.AddWithValue("@address", addressTextBox.Text);
                command.Parameters.AddWithValue("@director", directorTextBox.Text);
                command.Parameters.AddWithValue("@phone", phoneNumber);
                command.Parameters.AddWithValue("@email", emailTextBox.Text);
                command.Parameters.AddWithValue("@inn", inn);

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    MessageBox.Show("Партнёр успешно добавлен!");
                    OnDataAdded?.Invoke(); // Вызов события обновления списка партнёров
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при добавлении партнёра.");
                }
            }

            catch (Exception ex)
            {
                if (ex.Message.Contains("23505")) // PostgreSQL сообщает об ошибке уникальности
                {
                    MessageBox.Show("Ошибка: ИНН уже существует в базе данных.");
                }
                else
                {
                    MessageBox.Show($"Ошибка сохранения данных: {ex.Message}");
                }
            }
        }

        private void back_button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
