﻿using DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model;
using System.Diagnostics;
using System.Net;
//using System.Web.Mvc;
using Web.Mvc.Models;

namespace Web.Mvc.Controllers
{
    public class DoctorController : Controller
    {
        private readonly ILogger<DoctorController> _logger;
        private readonly IConfiguration _config;

        public DoctorController(ILogger<DoctorController> logger, IConfiguration config)
        {
            _logger = logger;
            _config=config;
        }

        [HttpGet]        
        public IActionResult Index()
        {
            Doctor doctor = new Doctor();
            return View(doctor);
        }
        
        [HttpPost]
        public IActionResult Index([FromBody] Doctor doctor)
        {
            try
            {
                DoctorAccess doctorAccess = new DoctorAccess(_config);
                doctorAccess.AddDoctor(doctor);
                return Json(new { message = "success", firstName = "jeffin" });
            }
            catch (System.Exception)
            {
                return StatusCode(500,new { messege = "Error",discription="an error occured while proessing ur request" });
               
            }
           
        }

        public IActionResult Privacy()
        {
            ViewData["Jeffin"] = "This is my test Privacy policy";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        
        
    }
}