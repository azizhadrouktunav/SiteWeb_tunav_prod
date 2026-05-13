using Microsoft.AspNetCore.Mvc;
using tunav_backend.Models;

namespace tunav_backend.Controllers;

public class BackofficeController : Controller
{
    private readonly AppDbContext _context;

    public BackofficeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("/dashboard")]
    public IActionResult Dashboard() => View();

    [HttpGet("/backoffice/users")]
    public IActionResult Users() => View();

    [HttpGet("/backoffice/roles")]
    public IActionResult Roles() => View();

    [HttpGet("/backoffice/permissions")]
    public IActionResult Permissions() => View();

    [HttpGet("/backoffice/solutions")]
    public IActionResult Solutions() => View();

    [HttpGet("/backoffice/packs")]
    public IActionResult Packs() => View();

    [HttpGet("/backoffice/team")]
    public IActionResult TeamMembers() => View();

    [HttpGet("/backoffice/blogs")]
    public IActionResult Blogs() => View();

    [HttpGet("/backoffice/events")]
    public IActionResult Events() => View();

    [HttpGet("/backoffice/collaborations")]
    public IActionResult Collaborations() => View();

    [HttpGet("/backoffice/partner-requests")]
    public IActionResult PartnerRequests() => View();

    [HttpGet("/backoffice/demo-requests")]
    public IActionResult DemoRequests() => View();

    [HttpGet("/backoffice/custom-pack-requests")]
    public IActionResult CustomPackRequests() => View();

    [HttpGet("/backoffice/formation")]
    public IActionResult Formation() => View();


    [HttpGet("/backoffice/newsletters")]
    public IActionResult Newsletters() => View();


    [HttpGet("/backoffice/jobs")]
    public IActionResult Jobs() => View();



    [HttpGet("/backoffice/contacts")]
    public IActionResult Contacts() => View();


    [HttpGet("/backoffice/clients")]
    public IActionResult Clients() => View();

    [HttpGet("/backoffice/partners")]
    public IActionResult Partners() => View();


    [HttpGet("/backoffice/sectors")]
    public IActionResult Sectors() => View();

    [HttpGet("/backoffice/partner-portal")]
    public IActionResult PartnerPortal() => View();


    [HttpGet("/backoffice/sav-claims")]
    public IActionResult SavClaims() => View();


    [HttpGet("/backoffice/commercial-demands")]
    public IActionResult CommercialDemands() => View();
}