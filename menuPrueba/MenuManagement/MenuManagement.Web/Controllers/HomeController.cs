using Microsoft.AspNetCore.Mvc;

namespace MenuManagement.Web.Controllers;

/// Controlador para páginas principales
public class HomeController : Controller
{
    /// Muestra la página de inicio
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }

    /// Muestra política de privacidad
    public IActionResult Privacy()
    {
        return View();
    }
}