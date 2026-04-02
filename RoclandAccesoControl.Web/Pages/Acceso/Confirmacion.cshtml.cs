using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RoclandAccesoControl.Web.Pages.Acceso
{
    public class ConfirmacionModel : PageModel
    {
        public void OnGet()
        {
            // El manejo de los datos de confirmación se realiza del lado del cliente
            // utilizando sessionStorage para evitar enviar identificadores o datos
            // sensibles en texto plano a través de los parámetros de la URL.
        }
    }
}