﻿using DAL;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Model;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.Threading.Tasks;
using Web.Mvc.Extentions;
//using System.Web.Mvc;
using Web.Mvc.Models;

namespace Web.Mvc.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ILogger<AppointmentController> _logger;
        private readonly IConfiguration _config;
        private ILoggerFactory _factory;
        private string _connectionString;
        private SqlConnection connection;

        public AppointmentController(ILogger<AppointmentController> logger, IConfiguration config,ILoggerFactory factory)
        {
            _logger = logger;
            _config=config;
            _factory = factory;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] int? id)
        {
            Appointment viewModel = new Appointment();
            if (id > 0)
            {                               
                AppointmentAccess aa = new AppointmentAccess(_config,_factory.CreateLogger<AppointmentAccess>());
                viewModel = await aa.GetAppointmentById(id.Value);
                var doctorList = await aa.AllDoctorList();
                viewModel.DoctorList = doctorList.ToViewModel();
                viewModel.SelectedDoctorId = viewModel.DoctorId;
                                
            }
            return View("appointment", viewModel);            
        }

        //[HttpPost]
        //public IActionResult Index([FromBody] Appointment appointment)
        //{
        //        appointment.StatusId = (int)StatusEnum.Scheduled;
        //        AppointmentAccess appointmentAccess = new AppointmentAccess(_config);
        //        appointmentAccess.AddAppointment(appointment);
        //        return Json(new { message = "success"});            
        //}
       
        [HttpGet]
        public IActionResult PetId()
        {           
            var viewModel = new PetTypeModel();
            return View(viewModel);
        }
        
        [HttpPost]
        public async Task<IActionResult> PetId([FromBody] PetTypeModel model)
        {
            if (model == null || model.PetTypeValue < 1 || model.PetTypeValue > 3)
            {
                return BadRequest("Invalid pet type value.");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("dbo.GetBreedTypeListByPetTypeId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PetTypeValue", model.PetTypeValue);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                // Handle exceptions, log errors, etc.
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }
       

        [HttpGet]
        public IActionResult GetPetTypes()
        {
            // Replace this sample data with data from your database
            var petTypes = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "Dog" },
            new SelectListItem { Value = "2", Text = "Cat" },
            new SelectListItem { Value = "3", Text = "Rabbit" }
        };

            return Json(petTypes);
        }

        
        [HttpGet]
        public async Task<IActionResult> GetBreedsByPetTypeId([FromQuery]int petTypeId)
        {
            AppointmentAccess a = new AppointmentAccess(_config, _factory.CreateLogger<AppointmentAccess>());

            List<Breed> breedList = await a.GetBreedsByPetTypeId(petTypeId);
            return Json(breedList);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment([FromBody] Appointment request)
        {
            
            try
            {
                AppointmentAccess appointmentAccess = new AppointmentAccess(_config, _factory.CreateLogger<AppointmentAccess>());

                if (request.Id > 0)
                {
                   await appointmentAccess.UpdateAppointmentById(request);
                }
                else 
                {
                    request.StatusId = (int)StatusEnum.Scheduled;
                    appointmentAccess.AddAppointment(request);
                }
                
                return Json(new { message = "success"});
            }
            catch (System.Exception)
            {
                return StatusCode(500, new { messege = "Error", discription = "an error occured while proessing ur request" });

            }

        }
        [HttpGet]
        public async Task<ActionResult<List<Doctor>>> GetAllDoctors()
        {
            try
            {
                AppointmentAccess a = new AppointmentAccess(_config, _factory.CreateLogger<AppointmentAccess>());
                List<Doctor> doctorList = await a.AllDoctorList();
                return Ok(doctorList);
            }
            catch (Exception ex)
            {
                // Handle exceptions here
                return StatusCode(500, "An error occurred while fetching doctors.");
            }
        }

        [HttpGet]
        public async Task<ActionResult<AppointmentListContainer>> GetAllAppointments([FromQuery]AppointmentSearchParms request)
        {
            try
            {              

                int? doctorId = request.SelectedDoctorId == 0 ? null : request.SelectedDoctorId ;
                int? statusId = request.SelectedStatusId == 0 ? null : request.SelectedStatusId;
                AppointmentAccess apt = new AppointmentAccess(_config, _factory.CreateLogger<AppointmentAccess>());
                List<Appointment> appointmentlist = await apt.GetAllAppointmentsDapper(request.CurrentPage, request.PageSize, doctorId, statusId);
                List<Status> statuslist = await apt.GetAllStatus();
                List<Doctor> doctorList = await apt.AllDoctorList();
                AppointmentListContainer container = new AppointmentListContainer();
                container.SearchParms = new AppointmentSearchParms();                
                container.SearchParms.StatusList = statuslist.ToViewModel();
                container.SearchParms.DoctorList = doctorList.ToDoctorViewModel();
                container.AppointmentList = appointmentlist;
                if(appointmentlist.Count > 0)
                {
                 container.PagingInfo = new Pager(appointmentlist.First().TotalRecords, request.CurrentPage, request.PageSize);
                }
                return View("AppointmentList", container);
            }
            catch (DivideByZeroException ex)
            {
                // Handle exceptions here
                _logger.LogError(ex, "error occured while retreving appointment list");
                return StatusCode(500, "An error occurred while fetching doctors.");
            }
            catch (Exception ex)
            {
                // Handle exceptions here
                return StatusCode(500, "An error occurred while fetching doctors.");
            }
        }


        [HttpDelete]
        public ActionResult Delete(int deleteId)
        {
            try
            {

                AppointmentAccess appointmentAccess = new AppointmentAccess(_config, _factory.CreateLogger<AppointmentAccess>());
                appointmentAccess.DeleteAppointment(deleteId);

                // Assuming the operation was successful, return a JSON success response
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the deletion process
                // You can log the error or return an error response as needed
                return Json(new { success = false, errorMessage = ex.Message });
            }
        }
        [HttpGet]
        public async Task<ActionResult<Appointment>> GetAppointmentById(int id )
        {
            try
            {
                AppointmentAccess apt = new AppointmentAccess(_config, _factory.CreateLogger<AppointmentAccess>());
                Appointment appointment = await apt.GetAppointmentById(id);
                return View("editappointment", appointment);
            }
            catch (Exception ex)
            {
                // Handle exceptions here
                return StatusCode(500, "An error occurred while fetching doctors.");
            }
        }
        [HttpGet]
        public IActionResult EditAppointment()
        {
            
            return View("appointment"); 
        }
              
        


    }
}

    