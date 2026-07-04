using System;
using AtomUI.Controls;
using AtomUI.Desktop.Controls;
using Avalonia.Controls;

namespace OutlineUi.Services;

public class NotificationService : INotificationService
{
    private readonly WindowMessageManager? _messageManager;

    public NotificationService(TopLevel? topLevel)
    {
        if (topLevel != null)
        {
            _messageManager = new WindowMessageManager(topLevel)
            {
                MaxItems = 5
            };
        }
    }

    public void ShowInfo(string message)
    {
        Show(message, MessageType.Information);
    }

    public void ShowSuccess(string message)
    {
        Show(message, MessageType.Success);
    }

    public void ShowWarning(string message)
    {
        Show(message, MessageType.Warning);
    }

    public void ShowError(string message)
    {
        Show(message, MessageType.Error);
    }

    public void ShowLoading(string message)
    {
        Show(message, MessageType.Loading);
    }

    public void Show(string message, MessageType type = MessageType.Information, TimeSpan? expiration = null)
    {
        if (_messageManager == null)
        {
            Console.WriteLine($"[Notification] {type}: {message}");
            return;
        }

        var atomType = type switch
        {
            MessageType.Success =>  MessageType.Success,
            MessageType.Warning =>  MessageType.Warning,
            MessageType.Error => MessageType.Error,
            MessageType.Loading => MessageType.Loading,
            _ => MessageType.Information
        };

        var messageOptions = new Message(
            type: atomType,
            content: message
        );

        if (expiration.HasValue)
        {
            messageOptions.Expiration = expiration.Value;
        }

        _messageManager.Show(messageOptions);
    }
}