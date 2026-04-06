using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RoclandAccesoControl.Web.Pages.Admin;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class DashboardModel : PageModel
{
    public void OnGet() { }
}