using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandAccesoControl.Mobile.Models;
using RoclandAccesoControl.Mobile.Services;

namespace RoclandAccesoControl.Mobile.ViewModels;

public partial class AccesosActivosViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;
    private readonly SignalRService _signalR;

    [ObservableProperty] private ObservableCollection<AccesoActivo> _activos = [];
    [ObservableProperty] private int _totalDentro;
    [ObservableProperty] private bool _sinActivos = true;

    public AccesosActivosViewModel(ApiService api, AuthStateService auth, SignalRService signalR)
    {
        _api = api;
        _auth = auth;
        _signalR = signalR;
        Titulo = "Dentro ahora";

        _signalR.SalidaRegistrada += OnSalidaRegistrada;
    }

    private void OnSalidaRegistrada(int registroId)
    {
        MainThread.BeginInvokeOnMainThread(() => {
            var item = Activos.FirstOrDefault(a => a.RegistroId == registroId);
            if (item != null)
            {
                Activos.Remove(item);
                TotalDentro = Activos.Count;
                SinActivos = Activos.Count == 0;
            }
        });
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        EstaCargando = true;
        try
        {
            var lista = await _api.ObtenerActivosAsync();
            Activos = new ObservableCollection<AccesoActivo>(lista);
            TotalDentro = lista.Count;
            SinActivos = lista.Count == 0;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task MarcarSalidaAsync(AccesoActivo activo)
    {
        if (activo is null) return;

        var confirmacion = await Shell.Current.DisplayAlert(
            "Confirmar salida",
            $"¿Registrar salida de {activo.NombrePersona}?\nGafete: #{activo.NumeroGafete}",
            "Sí, marcar salida", "Cancelar");

        if (!confirmacion) return;

        EstaCargando = true;
        try
        {
            var ok = await _api.MarcarSalidaAsync(new MarcarSalidaRequest
            {
                RegistroId = activo.RegistroId,
                TipoRegistro = activo.TipoRegistro,
                GuardiaId = _auth.GuardiaId
            });

            if (ok)
            {
                Activos.Remove(activo);
                TotalDentro = Activos.Count;
                SinActivos = Activos.Count == 0;
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "No se pudo registrar la salida.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error de red", ex.Message, "OK");
        }
        finally
        {
            EstaCargando = false;
        }
    }
}