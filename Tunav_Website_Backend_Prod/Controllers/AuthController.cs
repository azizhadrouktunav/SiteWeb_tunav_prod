using Microsoft.AspNetCore.Mvc;

namespace tunav_backend.Controllers;

public class AuthController : Controller
{
    [HttpGet("/auth/login")]
    public IActionResult Login() => View();
}