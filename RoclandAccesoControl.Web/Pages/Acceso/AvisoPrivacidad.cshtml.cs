using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RoclandAccesoControl.Web.Pages.Acceso
{
    public class AvisoPrivacidadModel : PageModel
    {
        private readonly ILogger<AvisoPrivacidadModel> _logger;

        public AvisoPrivacidadModel(ILogger<AvisoPrivacidadModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Se ha consultado el Aviso de Privacidad.");
        }
    }
}
