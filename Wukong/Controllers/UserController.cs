using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

using Wukong.Services;
using Wukong.Models;
using Wukong.Repositories;

namespace Wukong.Controllers
{
    [Authorize]
    [Route("api/user")]
    public class UserController : Controller
    {
        private readonly IUserConfigurationRepository userConfigurationRespository;
        private readonly IUserService userService;

        public UserController(IUserConfigurationRepository userConfigurationRespository, IUserService userService)
        {
            this.userConfigurationRespository = userConfigurationRespository;
            this.userService = userService;
        }

        [HttpGet("userinfo")]
        public IActionResult GetUserinfo()
        {
            return new ObjectResult(userService.User);
        }

        [HttpGet("configuration")]
        public async Task<IActionResult> GetConfiguration()
        {
            var configuration = await userConfigurationRespository.GetAsync("wukong", userService.User.Id);
            return new ObjectResult(configuration);
        }

        [HttpPost("configuration")]
        public async Task<IActionResult> SaveConfiguration([FromBody] UserConfigurationData configuration)
        {
            await userConfigurationRespository.AddOrUpdateAsync(
                "wukong",
                userService.User.Id,
                configuration.SyncPlaylists,
                configuration.Cookies);
            return new OkResult();
        }
    }

}