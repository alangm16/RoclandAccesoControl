using RoclandAccesoControl.Mobile.Services;

namespace RoclandAccesoControl.Mobile;

public partial class App : Application
{
    private readonly AuthStateService _auth;

    public App(AuthStateService auth)
    {
        InitializeComponent();
        _auth = auth;

        // MainPage DEBE asignarse en el constructor
        MainPage = new AppShell();
    }

    protected override async void OnStart()
    {
        base.OnStart();

        try
        {
            var sesionRestaurada = await _auth.RestaurarSesionAsync();
            await Shell.Current.GoToAsync(sesionRestaurada ? "//Solicitudes" : "//Login");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync(
                "Error de inicio",
                $"{ex.GetType().Name}\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                "OK");
        }
    }
}