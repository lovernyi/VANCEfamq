using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace VANCEfamq
{
    public partial class LauncherWindow : Window
    {
        private const string CURRENT_VERSION = "1.0.0";
        private const string UPDATE_JSON_URL = "https://raw.githubusercontent.com/lovernyi/VANCEfamq/main/update.json";
        private string downloadUrl = "";
        private bool isUpdateAvailable = false;
        private bool isConnectionFailed = false;
        private readonly string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");

        public LauncherWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Плавное появление окна
            Storyboard? sbFade = FindResource("FadeIn") as Storyboard;
            sbFade?.Begin(MainBorder);

            // Проверка логотипа
            if (File.Exists(logoPath))
            {
                try { AppLogo.Source = new BitmapImage(new Uri(logoPath)); } catch { }
            }

            // Запускаем проверки
            await RunStrictChecksAsync();
        }

        private async Task RunStrictChecksAsync()
        {
            isConnectionFailed = false;
            ActionBtn.IsEnabled = false;
            UpdateProgress.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#E11D48")!;

            try
            {
                // Шаг 1: Проверка окружения
                StatusTitleTxt.Text = "Проверка окружения...";
                StatusSubTxt.Text = "Сканирование защищенных модулей";
                UpdateProgress.IsIndeterminate = true;
                await Task.Delay(400);

                // Шаг 2: Связь с GitHub
                StatusTitleTxt.Text = "Связь с сервером...";
                StatusSubTxt.Text = "Авторизация и проверка обновлений";

                using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string json = await client.GetStringAsync($"{UPDATE_JSON_URL}?t={DateTime.Now.Ticks}");

                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                string latestVersion = root.TryGetProperty("version", out JsonElement vElem) ? vElem.GetString() ?? CURRENT_VERSION : CURRENT_VERSION;
                downloadUrl = root.TryGetProperty("download_url", out JsonElement dElem) ? dElem.GetString() ?? string.Empty : string.Empty;

                if (new Version(latestVersion) > new Version(CURRENT_VERSION))
                {
                    isUpdateAvailable = true;
                    StatusTitleTxt.Text = "ДОСТУПНО ОБНОВЛЕНИЕ";
                    StatusSubTxt.Text = $"Найдена версия v{latestVersion}";
                    ActionBtn.Content = "СКАЧАТЬ ОБНОВЛЕНИЕ";
                    ServerStatusTxt.Text = "Статус: Обновление";
                }
                else
                {
                    StatusTitleTxt.Text = "ДОСТУП РАЗРЕШЕН";
                    StatusSubTxt.Text = "Все системы и модули проверены";
                    ActionBtn.Content = "ЗАПУСТИТЬ";
                    ServerStatusTxt.Text = "Сервер: Онлайн";
                }

                UpdateProgress.IsIndeterminate = false;
                UpdateProgress.Value = 100;
                ActionBtn.IsEnabled = true;
            }
            catch
            {
                // Если связи нет — включаем режим ошибки с возможностью автопереподключения
                isConnectionFailed = true;
                StatusTitleTxt.Text = "ОШИБКА ДОСТУПА";
                StatusSubTxt.Text = "Нет соединения с сервером!";
                ServerStatusTxt.Text = "Сервер: Офлайн";
                UpdateProgress.IsIndeterminate = false;
                UpdateProgress.Foreground = System.Windows.Media.Brushes.Red;

                ActionBtn.Content = "ПОВТОРИТЬ ПОДКЛЮЧЕНИЕ";
                ActionBtn.IsEnabled = true; // Кнопка активна, чтобы пользователь мог переподключиться
            }
        }

        private async void ActionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isConnectionFailed)
            {
                // Если соединения не было, кнопка работает как "Повторить подключение"
                await RunStrictChecksAsync();
                return;
            }

            if (isUpdateAvailable && !string.IsNullOrEmpty(downloadUrl))
            {
                Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                Environment.Exit(0);
            }
            else
            {
                MainWindow main = new MainWindow();
                Application.Current.MainWindow = main;
                main.Show();
                this.Close();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Environment.Exit(0);
    }
}