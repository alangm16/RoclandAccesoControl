using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandAccesoControl.Mobile.Services;
using RoclandAccesoControl.Mobile.Views;

namespace RoclandAccesoControl.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    [ObservableProperty] private string _usuario = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private bool _hayError;

    public LoginViewModel(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
        Titulo = "Guardia — Acceso";
    }

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            MostrarError("Ingresa usuario y contraseña.");
            return;
        }

        EstaCargando = true;
        HayError = false;

        try
        {
            var result = await _api.LoginAsync(Usuario, Password);

            if (result is null)
            {
                MostrarError("Usuario o contraseña incorrectos.");
                return;
            }

            _auth.GuardarSesion(result.Token, result.Nombre, result.Id);

            // Navegar al shell principal
            await Shell.Current.GoToAsync("//Solicitudes");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error de conexión: {ex.Message}\nInner: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}";
            System.Diagnostics.Debug.WriteLine(errorMsg);  // ← se verá en la salida de VS
            MostrarError(errorMsg);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void MostrarError(string msg)
    {
        MensajeError = msg;
        HayError = true;
    }
}