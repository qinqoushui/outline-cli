using AtomUI.Desktop.Controls;
using System;

namespace OutlineUi.Services;

public interface INotificationService
{
    void ShowInfo(string message);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
    void ShowLoading(string message);
    void Show(string message, MessageType type = MessageType.Information, TimeSpan? expiration = null);
}

 