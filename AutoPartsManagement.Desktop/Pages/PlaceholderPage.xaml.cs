using System.Windows.Controls;

namespace AutoPartsManagement.Desktop.Pages;

/// <summary>Trang tạm cho chức năng sẽ làm ở phần sau.</summary>
public partial class PlaceholderPage : UserControl
{
    public PlaceholderPage(string title, string part)
    {
        InitializeComponent();
        TitleText.Text = title;
        HintText.Text = $"Chức năng này đang được hoàn thiện ({part}).";
    }
}
