using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoclandAccesoControl.Mobile.Models;
using RoclandAccesoControl.Mobile.Services;
using RoclandAccesoControl.Mobile.Views;

namespace RoclandAccesoControl.Mobile.ViewModels;

[QueryProperty(nameof(Solicitud), "Solicitud")]
[QueryProperty(nameof(SolicitudIdParam), "id")]
public partial class DetalleSolicitudViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStateService _auth;

    [ObservableProperty] private SolicitudPendiente? _solicitud;
    [ObservableProperty] private string _numeroGafete = string.Empty;
    [ObservableProperty] private bool _accionCompletada;

    public string SolicitudIdParam
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                _ = CargarSolicitudDesdeApiAsync(id);
            }
        }
    }

    public DetalleSolicitudViewModel(ApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
        Titulo = "Detalle de Solicitud";
    }

    private async Task CargarSolicitudDesdeApiAsync(int id)
    {
        EstaCargando = true;
        try
        {
            Solicitud = await _api.ObtenerSolicitudPorIdAsync(id);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", "No se pudo cargar el detalle del visitante.", "OK");
        }
        finally
        {
            EstaCargando = false;
        }
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
                await NavegarAtrasOSolicitudesAsync();
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
                await NavegarAtrasOSolicitudesAsync();
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
    private async Task RegresarAsync() => await NavegarAtrasOSolicitudesAsync();

    // ──── Navegación inteligente hacia atrás ──────────────────────────
    private async Task NavegarAtrasOSolicitudesAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                // Pequeño retardo para asegurar que la UI termine cualquier animación
                await Task.Delay(100);

                var navigationStack = Shell.Current.Navigation.NavigationStack;
                bool hayPaginaAnteriorValida = navigationStack.Count >= 2 &&
                                                navigationStack[^2] is not DetalleSolicitudPage;

                if (hayPaginaAnteriorValida)
                {
                    // Flujo normal: regresar a la página anterior (normalmente SolicitudesPage)
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // Fallback: navegar directamente a la raíz de Solicitudes
                    await Shell.Current.GoToAsync("//Bitacora");
                }
            }
            catch (Exception ex)
            {
                // Si algo falla, último recurso: ir a la raíz de Solicitudes
                System.Diagnostics.Debug.WriteLine($"[NAV Error] {ex.Message}");
                await Shell.Current.GoToAsync("//Bitacora");
            }
        });
    }
}