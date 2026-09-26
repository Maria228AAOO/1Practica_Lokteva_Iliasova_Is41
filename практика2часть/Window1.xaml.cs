using System;
using System.IO;
using System.Windows;
using MySql.Data.MySqlClient;

namespace практика2часть
{
    public partial class Window1 : Window
    {
        private int failedAttempts = 0;
        private bool isPuzzleSolved = false;

        private string connectionString = "server=192.168.227.14;port=3306;Database=практика2часть;Uid=user04;Pwd=User04!Pass;";

        public Window1()
        {
            InitializeComponent();
            HttpServer.Start();
            HttpServer.Start(); // Автозапуск API-сервера контрагентов
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string enteredLogin = TxtLogin.Text.Trim();
                string enteredPassword = TxtPassword.Password.Trim();

                if (string.IsNullOrEmpty(enteredLogin) || string.IsNullOrEmpty(enteredPassword))
                {
                    MessageBox.Show("Поля обязательны для заполнения!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Переводим в нижний регистр для безопасной проверки
                bool isAdmin = enteredLogin.ToLower() == "admin" || enteredLogin.ToLower() == "администратор";

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // 🎯 ЗАЩИТА АДМИНА: Если заходит НЕ админ — проверяем его блокировку в базе
                    if (!isAdmin)
                    {
                        string checkBlockQuery = "SELECT is_blocked FROM users WHERE login = @login";
                        using (MySqlCommand checkBlockCmd = new MySqlCommand(checkBlockQuery, connection))
                        {
                            checkBlockCmd.Parameters.AddWithValue("@login", enteredLogin);
                            object blockResult = checkBlockCmd.ExecuteScalar();

                            if (blockResult != null && Convert.ToInt32(blockResult) == 1)
                            {
                                MessageBox.Show($"Пользователь '{enteredLogin}' заблокирован! Обратитесь к администратору.", "Доступ заблокирован", MessageBoxButton.OK, MessageBoxImage.Error);
                                return; // Обычного заблокированного юзера дальше не пускаем
                            }
                        }
                    }

                    // Если были ошибки ввода и пазл всё ещё не собран — требуем капчу (для всех)
                    if (failedAttempts > 0 && !isPuzzleSolved)
                    {
                        failedAttempts++;
                        MessageBox.Show("Сначала соберите капчу-пазл!", "Капча", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Основная проверка пары Логин/Пароль
                    string query = "SELECT role FROM users WHERE login = @login AND password = @password";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", enteredLogin);
                        command.Parameters.AddWithValue("@password", enteredPassword);

                        object result = command.ExecuteScalar();

                        if (result != null)
                        {
                            string userRole = result.ToString();
                            failedAttempts = 0; // Сброс ошибок при успехе
                            MessageBox.Show("Вы успешно авторизовались.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                            if (userRole.ToLower().Contains("админ") || userRole.ToLower().Contains("admin"))
                            {
                                AdminWindow adminWin = new AdminWindow();
                                adminWin.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Добро пожаловать в рабочую область пользователя!", "Главная", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else
                        {
                            // Если пароль неверный — включаем капчу
                            LblCaptcha.Visibility = Visibility.Visible;
                            PnlPuzzle.Visibility = Visibility.Visible;
                            failedAttempts++;

                            // Блокируем в базе только если это НЕ админ! Админа блокировать запрещено
                            if (failedAttempts >= 3 && !isAdmin)
                            {
                                string blockUserQuery = "UPDATE users SET is_blocked = 1 WHERE login = @login";
                                using (MySqlCommand blockCmd = new MySqlCommand(blockUserQuery, connection))
                                {
                                    blockCmd.Parameters.AddWithValue("@login", enteredLogin);
                                    blockCmd.ExecuteNonQuery();
                                }
                                MessageBox.Show($"Пользователь '{enteredLogin}' заблокирован за 3 неудачные попытки входа!", "Блокировка", MessageBoxButton.OK, MessageBoxImage.Stop);
                            }
                            else
                            {
                                MessageBox.Show("Неверный логин или пароль. Проверьте введенные данные.", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка СУБД: {ex.Message}", "Сбой подключения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Логика сборки пазла 2х2
        private void ImgPart_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image clickedImage = sender as System.Windows.Controls.Image;
            if (clickedImage == ImgPart1) { var t = ImgPart1.Source; ImgPart1.Source = ImgPart2.Source; ImgPart2.Source = t; }
            else if (clickedImage == ImgPart2) { var t = ImgPart2.Source; ImgPart2.Source = ImgPart3.Source; ImgPart3.Source = t; }
            else if (clickedImage == ImgPart3) { var t = ImgPart3.Source; ImgPart3.Source = ImgPart4.Source; ImgPart4.Source = t; }
            else if (clickedImage == ImgPart4) { var t = ImgPart4.Source; ImgPart4.Source = ImgPart1.Source; ImgPart1.Source = t; }

            if (CheckIfPuzzleCorrect())
            {
                isPuzzleSolved = true;
                MessageBox.Show("Капча успешно пройдена! Пазл собран правильно.", "Капча", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool CheckIfPuzzleCorrect()
        {
            if (ImgPart1.Source == null || ImgPart2.Source == null || ImgPart3.Source == null || ImgPart4.Source == null) return false;
            return Path.GetFileName(ImgPart1.Source.ToString()) == "1.png" &&
                   Path.GetFileName(ImgPart2.Source.ToString()) == "2.png" &&
                   Path.GetFileName(ImgPart3.Source.ToString()) == "3.png" &&
                   Path.GetFileName(ImgPart4.Source.ToString()) == "4.png";
        }
    }
}
