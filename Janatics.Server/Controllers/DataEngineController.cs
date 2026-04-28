namespace Janatics.Server.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Janatics.DataEngine;
    using System.Linq;
    using Microsoft.AspNetCore.Authorization;

    [ApiController]
    [Route("api/[controller]")]
    public class DataEngineController : ControllerBase
    {
        private readonly IDataEngineService _svc;

        public DataEngineController(IDataEngineService svc)
        {
            _svc = svc;
        }

        [HttpGet("profiles")]
        public IActionResult GetProfiles()
        {
            return Ok(_svc.GetProfiles());
        }

        [HttpGet("profiles/{profileId}/entities")]
        public IActionResult GetEntities(string profileId)
        {
            return Ok(_svc.GetEntities(profileId));
        }

        [HttpGet("entities/{entityId}/columns")]
        public IActionResult GetColumns(string entityId)
        {
            return Ok(_svc.GetColumns(entityId));
        }

        [HttpGet("profiles/{profileId}/discover")]
        [Authorize(Roles = "Admin")]
        public IActionResult Discover(string profileId)
        {
            return Ok(_svc.DiscoverTables(profileId));
        }
    }
}
