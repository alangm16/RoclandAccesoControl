using Microsoft.AspNetCore.Mvc.RazorPages;
// using Microsoft.AspNetCore.Authorization; 

namespace RoclandAccesoControl.Web.Pages.Acceso
{
    // Si la visualización e impresión del QR debe estar restringida exclusivamente
    // al personal de la caseta o administradores, puedes habilitar [Authorize].
    public class QRModel : PageModel
    {
        public void OnGet()
        {
            // La vista QR.cshtml consume la API internamente para 
            // incrustar la imagen del código QR sin procesarlo aquí en el PageModel.
        }
    }
}