namespace AgroTrack.Services.Interfaces;

public interface IDialogService
{
    void ShowInformation(string message, string title = "Informasi");
    void ShowWarning(string message, string title = "Peringatan");
    void ShowError(string message, string title = "Kesalahan");
    bool ShowConfirmation(string message, string title = "Konfirmasi");
}
