using System.Windows;
using AgroTrack.Services.Interfaces;

namespace AgroTrack.Services.Implementations;

public class DialogService : IDialogService
{
    public void ShowInformation(string message, string title = "Informasi")
    {
        if (Application.Current != null && Application.Current.MainWindow != null)
        {
            MessageBox.Show(Application.Current.MainWindow, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public void ShowWarning(string message, string title = "Peringatan")
    {
        if (Application.Current != null && Application.Current.MainWindow != null)
        {
            MessageBox.Show(Application.Current.MainWindow, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public void ShowError(string message, string title = "Kesalahan")
    {
        if (Application.Current != null && Application.Current.MainWindow != null)
        {
            MessageBox.Show(Application.Current.MainWindow, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public bool ShowConfirmation(string message, string title = "Konfirmasi")
    {
        MessageBoxResult result;
        if (Application.Current != null && Application.Current.MainWindow != null)
        {
            result = MessageBox.Show(Application.Current.MainWindow, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        }
        else
        {
            result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        return result == MessageBoxResult.Yes;
    }
}
