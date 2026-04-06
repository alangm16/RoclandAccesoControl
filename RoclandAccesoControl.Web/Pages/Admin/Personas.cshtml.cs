using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RoclandAccesoControl.Web.Pages.Admin;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class PersonasModel : PageModel
{
    public void OnGet() { }
}