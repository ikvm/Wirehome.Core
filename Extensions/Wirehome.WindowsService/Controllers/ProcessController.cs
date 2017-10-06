﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Wirehome.WindowsService.Services;

namespace Wirehome.WindowsService.Controllers
{
    [Route("api/[controller]")]
    public class ProcessController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public ProcessController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        private string ReadProcessPath(string processName)
        {
            var configuration = Path.Combine(_hostingEnvironment.ContentRootPath, "configuration.json");

            if (!System.IO.File.Exists(configuration)) throw new Exception("Configuration file was not found");

            var jsonConfig = JObject.Parse(System.IO.File.ReadAllText(configuration));

            var map = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonConfig["Process"].ToString());

            if (!map.ContainsKey(processName)) throw new Exception($"Process {processName} is not registred in Wirehome Winsows Service");

            return map[processName];
        }

        [HttpGet]
        public bool Get(string processName)
        {
            return ProcessService.IsProcessStarted(ReadProcessPath(processName));
        }
        
        [HttpPost]
        public IActionResult Post(string processName, bool start)
        {
            var processPath = ReadProcessPath(processName);

            if (start)
            {
                ProcessService.StartProcess(processPath);
            }
            else
            {
                ProcessService.StopProcess(processPath);
            }

            return Ok();
        }
    }
}