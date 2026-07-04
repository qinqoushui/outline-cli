using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Windows.Input;
using OutlineUi.Models;
using OutlineUi.Services;

namespace OutlineUi.ViewModels;

public class ConfigViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private IOutlineApiService? _apiService;
    private Views.ConfigDialog? _dialog;

    private string _apiUrl = string.Empty;
    public string ApiUrl
    {
        get => _apiUrl;
        set => SetProperty(ref _apiUrl, value);
    }

    private string _apiToken = string.Empty;
    public string ApiToken
    {
        get => _apiToken;
        set => SetProperty(ref _apiToken, value);
    }

    private bool _isTesting;
    public bool IsTesting
    {
        get => _isTesting;
        set => SetProperty(ref _isTesting, value);
    }

    private string _testResult = string.Empty;
    public string TestResult
    {
        get => _testResult;
        set => SetProperty(ref _testResult, value);
    }

    private bool _isTestPassed;
    public bool IsTestPassed
    {
        get => _isTestPassed;
        set
        {
            if (SetProperty(ref _isTestPassed, value))
            {
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand TestConnectionCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ConfigViewModel(ConfigService configService)
    {
        _configService = configService;

        var config = _configService.Load();
        ApiUrl = config.ApiUrl;
        ApiToken = config.ApiToken;

        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => IsTestPassed);
        CancelCommand = new RelayCommand(Close);
    }

    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        IsTestPassed = false;
        TestResult = string.Empty;

        try
        {
            _apiService = new OutlineApiService(ApiUrl, ApiToken);
            await _apiService.GetCollectionsAsync();
            TestResult = "✓ 连接成功！可以保存配置";
            IsTestPassed = true;
        }
        catch (Exception ex)
        {
            TestResult = $"✗ 连接失败: {ex.Message}";
            IsTestPassed = false;
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task SaveAsync()
    {
        var config = new AppConfig
        {
            ApiUrl = ApiUrl,
            ApiToken = ApiToken
        };

        _configService.Save(config);
        Close();
        await Task.CompletedTask;
    }

    public void Close()
    {
        _dialog?.Close();
    }

    public async Task ShowAsync()
    {
        _dialog = new Views.ConfigDialog
        {
            DataContext = this
        };
        var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        if (window != null)
            await _dialog.ShowDialog(window);
    }
}
