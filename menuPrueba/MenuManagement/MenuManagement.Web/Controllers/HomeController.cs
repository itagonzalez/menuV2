using Microsoft.AspNetCore.Mvc;

namespace MenuManagement.Web.Controllers;

/// Controlador para p�ginas principales
public class HomeController : Controller
{
    /// Muestra la p�gina de inicio
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }

    /// Muestra pol�tica de privacidad
    public IActionResult Privacy()
    {
        return View();
    }
}