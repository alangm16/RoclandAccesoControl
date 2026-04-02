using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace RoclandAccesoControl.Web.Pages.Acceso
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Registra el acceso a la página principal de selección
            _logger.LogInformation("Se ha accedido a la página de selección de registro de acceso.");
        }
    }
}