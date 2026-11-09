using System.Windows;
using System.Windows.Markup;
using AutoPartsManagement.Desktop.Infrastructure;
using AutoPartsManagement.Desktop.Views;

namespace AutoPartsManagement.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Định dạng ngày, số theo tiếng Việt
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("vi-VN")));

        // Lỗi không lường trước: báo cho người dùng thay vì tắt app
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show("Có lỗi xảy ra:\n" + args.Exception.GetBaseException().Message,
                "3H AUTO", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            AppHost.Build();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không đọc được cấu hình (appsettings.json):\n" + ex.Message,
                "3H AUTO", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        new LoginWindow().Show();
    }
}
