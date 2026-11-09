using System.Windows;
using AutoPartsManagement.Desktop.Infrastructure;

namespace AutoPartsManagement.Desktop.Views;

public partial class LoginWindow : Window
{
    private bool _ready;

    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await ConnectAsync();

    private async void Retry_Click(object sender, RoutedEventArgs e) => await ConnectAsync();

    /// <summary>Kết nối database, tạo bảng + dữ liệu mẫu nếu chưa có (lần đầu có thể mất vài giây).</summary>
    private async Task ConnectAsync()
    {
        HideError();
        SetBusy(true, "Đang kết nối cơ sở dữ liệu...");
        try
        {
            await AppHost.InitializeDatabaseAsync();
            _ready = true;
            SetBusy(false, null);
            LoginUser.Focus();
        }
        catch (Exception ex)
        {
            SetBusy(false, null);
            ShowError("Không kết nối được cơ sở dữ liệu.\n" + ex.GetBaseException().Message +
                      "\n\nHãy kiểm tra SQL Server LocalDB và chuỗi kết nối trong appsettings.json.", retry: true);
        }
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        HideError();
        var userName = LoginUser.Text.Trim();
        var password = LoginPassword.Password;
        if (userName.Length == 0 || password.Length == 0)
        {
            ShowError("Vui lòng nhập tên đăng nhập và mật khẩu.");
            return;
        }

        SetBusy(true, "Đang đăng nhập...");
        try
        {
            var error = await AuthService.LoginAsync(userName, password);
            if (error != null)
            {
                ShowError(error);
                LoginPassword.Clear();
                LoginPassword.Focus();
                return;
            }
            OpenMainWindow();
        }
        catch (Exception ex)
        {
            ShowError("Lỗi khi đăng nhập: " + ex.GetBaseException().Message);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        HideError();
        var userName = RegUser.Text.Trim();
        if (userName.Length < 3)
        {
            ShowError("Tên đăng nhập phải có ít nhất 3 ký tự.");
            return;
        }
        if (RegPassword.Password != RegConfirm.Password)
        {
            ShowError("Mật khẩu nhập lại không khớp.");
            return;
        }

        SetBusy(true, "Đang tạo tài khoản...");
        try
        {
            var error = await AuthService.RegisterAsync(userName, RegPassword.Password);
            if (error != null)
            {
                ShowError(error);
                return;
            }
            OpenMainWindow();
        }
        catch (Exception ex)
        {
            ShowError("Lỗi khi đăng ký: " + ex.GetBaseException().Message);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    private void OpenMainWindow()
    {
        var main = new MainWindow();
        Application.Current.MainWindow = main;
        main.Show();
        Close();
    }

    private void ShowRegister_Click(object sender, RoutedEventArgs e) => SwitchMode(register: true);

    private void ShowLogin_Click(object sender, RoutedEventArgs e) => SwitchMode(register: false);

    private void SwitchMode(bool register)
    {
        HideError();
        LoginPanel.Visibility = register ? Visibility.Collapsed : Visibility.Visible;
        RegisterPanel.Visibility = register ? Visibility.Visible : Visibility.Collapsed;
        LoginButton.IsDefault = !register;      // phím Enter bấm đúng nút đang hiện
        RegisterButton.IsDefault = register;
        if (register) RegUser.Focus(); else LoginUser.Focus();
    }

    private void SetBusy(bool busy, string? status)
    {
        LoginButton.IsEnabled = !busy && _ready;
        RegisterButton.IsEnabled = !busy && _ready;
        StatusText.Text = status ?? "";
    }

    private void ShowError(string message, bool retry = false)
    {
        ErrorText.Text = message;
        RetryButton.Visibility = retry ? Visibility.Visible : Visibility.Collapsed;
        ErrorBox.Visibility = Visibility.Visible;
    }

    private void HideError() => ErrorBox.Visibility = Visibility.Collapsed;
}
