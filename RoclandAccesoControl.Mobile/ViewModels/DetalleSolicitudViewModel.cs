using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandAccesoControl.Mobile.Models;
using RoclandAccesoControl.Mobile.Services;

namespace RoclandAccesoControl.Mobile.ViewModels;

[QueryProperty(nameof(Solicitud), "Solicitud")]
public partial class DetalleSolicitudViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    [ObservableProperty] private SolicitudPendiente? _solicitud;
    [ObservableProperty] private string _numeroGafete = string.Empty;
    [ObservableProperty] private bool _accionCompletada;

    public DetalleSolicitudViewModel(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
        Titulo = "Detalle de Solicitud";
    }

    [RelayCommand]
    private async Task AprobarAsync()
    {
        if (Solicitud is null) return;

        if (string.IsNullOrWhiteSpace(NumeroGafete))
        {
            await Shell.Current.DisplayAlert("Campo requerido",
                "Ingresa el número de gafete para aprobar.", "OK");
            return;
        }

        var confirmacion = await Shell.Current.DisplayAlert(
            "Confirmar aprobación",
            $"¿Aprobar acceso de {Solicitud.NombrePersona} con gafete #{NumeroGafete}?",
            "Aprobar", "Cancelar");

        if (!confirmacion) return;

        EstaCargando = true;
        try
        {
            var ok = await _api.AprobarAsync(new AprobarRequest
            {
                SolicitudId = Solicitud.SolicitudId,
                GuardiaId = _auth.GuardiaId,
                NumeroGafete = NumeroGafete
            });

            if (ok)
            {
                await Shell.Current.DisplayAlert("✓ Aprobado",
                    $"Acceso aprobado. Entrega el gafete #{NumeroGafete}.", "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "No se pudo aprobar la solicitud.", "OK");
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

    [RelayCommand]
    private async Task RechazarAsync()
    {
        if (Solicitud is null) return;

        var motivo = await Shell.Current.DisplayPromptAsync(
            "Rechazar acceso",
            $"¿Por qué se rechaza el acceso de {Solicitud.NombrePersona}?",
            placeholder: "Motivo (opcional)",
            accept: "Rechazar",
            cancel: "Cancelar");

        if (motivo is null) return; // Canceló

        EstaCargando = true;
        try
        {
            var ok = await _api.RechazarAsync(new RechazarRequest
            {
                SolicitudId = Solicitud.SolicitudId,
                GuardiaId = _auth.GuardiaId,
                Motivo = motivo
            });

            if (ok)
            {
                await Shell.Current.DisplayAlert("✗ Rechazado",
                    "El acceso fue rechazado.", "OK");
                await Shell.Current.GoToAsync("..");
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

    [RelayCommand]
    private async Task RegresarAsync() => await Shell.Current.GoToAsync("..");
}