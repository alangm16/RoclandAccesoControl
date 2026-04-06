namespace RoclandAccesoControl.Web.Services.Interfaces;

public interface IFcmService
{
    Task EnviarAsync(string deviceToken, string titulo, string cuerpo,
        Dictionary<string, string>? data = null);
}