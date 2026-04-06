using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RoclandAccesoControl.Web.Pages.Admin;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class GuardiasModel : PageModel
{
    public void OnGet() { }
}