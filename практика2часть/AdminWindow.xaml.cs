using System;
using System.Data;
using System.Windows;
using MySql.Data.MySqlClient;

namespace практика2часть
{
    public partial class AdminWindow : Window
    {
        private string connectionString = "server=192.168.227.14;port=3306;Database=практика2часть;Uid=user04;Pwd=User04!Pass;";

        public AdminWindow()
        {
            InitializeComponent();
            LoadUsersToGrid(); // Автоматически загружаем пользователей при открытии окна!
        }

        // 🔥 КНОПКА НАЗАД: Закрывает админку и открывает стартовое окно входа
        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            Window1 loginWindow = new Window1();
            loginWindow.Show();
            this.Close();
        }

        // Метод выгрузки пользователей из MySQL в нашу табличку WPF
        private void LoadUsersToGrid()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT id AS 'ID', login AS 'Логин', role AS 'Роль', is_blocked AS 'Заблокирован' FROM users";
                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    DgUsers.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки таблицы: {ex.Message}", "Сбой", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка: Добавить пользователя
        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = TxtNewLogin.Text.Trim();
                string password = TxtNewPassword.Text.Trim();
                string role = CmbRole.Text;

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Заполните поля!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO users (login, password, role, is_blocked) VALUES (@login, @password, @role, 0)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);
                        command.Parameters.AddWithValue("@role", role);
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Пользователь '{login}' добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtNewLogin.Clear();
                TxtNewPassword.Clear();
                LoadUsersToGrid(); // Перерисовываем таблицу, чтобы новый юзер сразу появился на экране!
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Сбой", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка: Снять блокировку
        private void BtnUnlock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string loginToUnlock = TxtUnlockLogin.Text.Trim();

                if (string.IsNullOrEmpty(loginToUnlock))
                {
                    MessageBox.Show("Введите логин!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE users SET is_blocked = 0 WHERE login = @login";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", loginToUnlock);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Блокировка с '{loginToUnlock}' снята!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadUsersToGrid(); // Обновляем таблицу!
                        }
                        else
                        {
                            MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                TxtUnlockLogin.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Сбой", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
