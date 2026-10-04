using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsightFlow.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        // =====================================================
        // PROTECTED ENDPOINT
        // GET: api/Test/protected
        // =====================================================
        [Authorize]
        [HttpGet("protected")]
        public IActionResult Protected()
        {
            var employeeId = User.FindFirst("employeeId")?.Value;
            var fullName = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var department = User.FindFirst("department")?.Value;

            return Ok(new
            {
                success = true,
                message = "JWT authentication is working.",
                employee = new
                {
                    employeeId,
                    fullName,
                    role,
                    department
                }
            });
        }

        // =====================================================
        // ADMIN ONLY ENDPOINT
        // GET: api/Test/admin
        // =====================================================
        [Authorize(Roles = "Administrator")]
        [HttpGet("admin")]
        public IActionResult AdminOnly()
        {
            return Ok(new
            {
                success = true,
                message = "Administrator authorization is working."
            });
        }
    }
}