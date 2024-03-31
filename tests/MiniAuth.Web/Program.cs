using Microsoft.AspNetCore.Mvc;
using MiniAuth.Configs;
using MiniAuth.Helpers;
using System.Data.SqlClient;
using System.Data.SQLite;

namespace MiniAuth.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(options => options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
            builder.Services.AddControllers();
            //builder.Services.AddSingleton<MiniAuthOptions>(new MiniAuthOptions {ExpirationMinuteTime=12*24*60 });
            builder.Services.AddSingleton<IMiniAuthDB>(
                 new MiniAuthDB<System.Data.SqlClient.SqlConnection>("Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=SSPI;Initial Catalog=miniauth;app=MiniAuth")
            );
            var app = builder.Build();
            app.UseCors("AllowAll");
            app.UseStaticFiles();
            app.UseMiniAuth();
            app.MapControllers();
            app.MapGet("/miniapi/get", () => "Hello MiniAuth!");
            app.Run();
        }
    }

    public class HomeController : Controller
    {
        [HttpGet]
        [HttpPost]
        [Route("/")]
        public ActionResult Home() => Content("This's homepage");
        [Route("/About")]
        public ActionResult About() => Content("This's About");
        [Route("/UserInfo")]
        public ActionResult UserInfo()
        {
            var user = this.GetMiniAuthUser();
            return Json(user);
        }
    }

    [ApiController]
    [Route("[controller]/[action]")]
    public class ProductsController : ControllerBase
    {
        [HttpGet]
        [HttpPost]
        public IEnumerable<dynamic> GetAll() => new[] { new { id = "1", name = "apple" }, new { id = "2", name = "orange" }, };
        [HttpGet]
        public dynamic Get(string id) => new { id = "1", name = "apple" };
        [HttpPost]
        public dynamic Post(string id) => new { data = "demo" };
    }

    [ApiController]
    [Route("[controller]/[action]")]
    public class ProductStockController : ControllerBase
    {
        [HttpGet]
        [HttpPost]
        public IEnumerable<dynamic> GetAll() => new[] { new { id = "1", name = "apple", stock = 100 }, new { id = "2", name = "orange", stock = 200 }, };

        public dynamic Get(string id) => new { id = "1", name = "apple", stock = 100 };
    }

}
