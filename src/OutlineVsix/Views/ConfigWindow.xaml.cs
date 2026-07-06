using System.Windows;
using OutlineVsix.Services;

namespace OutlineVsix.Views;

public partial class ConfigWindow : Window
{
    private readonly ConfigService? _configService;

    public ConfigWindow()
    {
        InitializeComponent();
    }

    public ConfigWindow(ConfigService configService) : this()
    {
        _configService = configService;
        var config = configService.Load();
        ApiUrlBox.Text = config.ApiUrl;
        ApiTokenBox.Text = config.ApiToken;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (_configService != null)
        {
            _configService.Save(new Models.AppConfig
            {
                ApiUrl = ApiUrlBox.Text.Trim(),
                ApiToken = ApiTokenBox.Text.Trim()
            });
        }
        DialogResult = true;
    }
}
