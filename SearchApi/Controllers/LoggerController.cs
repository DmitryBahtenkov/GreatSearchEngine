﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SearchApi.Services;

namespace SearchApi.Controllers
{
    [Route("logger")]
    [ApiController]
    public class LoggerController : ControllerBase
    {

        private readonly UserService _userService;

        public LoggerController(UserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Получить файл логгера
        /// </summary>
        /// <param name="date">Дата в формате гггг-мм-дд</param>
        /// <returns></returns>
        [HttpGet("download/{date}")]
        public async Task<IActionResult> DownloadLogByDate(string date)
        {
            if (await _userService.CheckAuthorize(Request, true) is null)
            {
                return Unauthorized();
            }
            var filename = date.Replace("-", "");
            return PhysicalFile($"{AppDomain.CurrentDomain.BaseDirectory}Logs/{filename}.log", "application/.log", $"{filename}.log");
        }
    }
}