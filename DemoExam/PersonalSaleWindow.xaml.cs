using System.Windows;
using System.Windows.Media;

namespace DemoExam
{
    /// <summary>
    /// Логика взаимодействия для PersonalSaleWindow.xaml
    /// </summary>

    public partial class PersonalSaleWindow : Window
    {
        public PersonalSaleWindow()
        {
            InitializeComponent();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sale_Label.Foreground = new SolidColorBrush(Color.FromRgb(103, 186, 128));
            sale_Label.Content = CalculateDiscount(Convert.ToInt64(personalSale_TextBox.Text));
        }
        private void back_button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
