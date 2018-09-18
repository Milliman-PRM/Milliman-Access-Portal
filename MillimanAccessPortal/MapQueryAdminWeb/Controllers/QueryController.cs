using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuditLogLib.Services;
using MapQueryAdminWeb.Pages;
using Microsoft.AspNetCore.Mvc;

namespace MapQueryAdminWeb.Controllers
{
    public class QueryController : Controller
    {
        public IAuditLogger _logger;

        public QueryController (IAuditLogger loggerArg)
        {
            _logger = loggerArg;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(QueryModel model)
        {


            return View();
        }
    }
}