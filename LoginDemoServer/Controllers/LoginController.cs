
using LoginDemoServer.Models;
using LoginDemoServer.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.AspNetCore.Server.IIS;

namespace LoginDemoServer.Controllers
{
    [Route("api")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        //a variable to hold a reference to the db context!
        private LoginDemoDbContext context;

        private IWebHostEnvironment webHostEnvironment;
        //Use dependency injection to get the db context intot he constructor
        public LoginController(LoginDemoDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.webHostEnvironment = env;
        }
        // POST api/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] DTO.LoginInfo loginDto)
        {
            try
            {
                HttpContext.Session.Clear(); //Logout any previous login attempt

                //Get model user class from DB with matching email. 
                Models.User modelsUser = context.GetUSerFromDB(loginDto.Email);
                
                //Check if user exist for this email and if password match, if not return Access Denied (Error 403) 
                if (modelsUser == null || modelsUser.Password != loginDto.Password) 
                {
                    return Unauthorized();
                }

                //Login suceed! now mark login in session memory!
                HttpContext.Session.SetString("loggedInUser", modelsUser.Email);

                return Ok(new DTO.User(modelsUser));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        

        // Get api/check
        [HttpGet("check")]
        public IActionResult Check()
        {
            try
            {
                //Check if user is logged in 
                string userEmail = HttpContext.Session.GetString("loggedInUser");

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized("User is not logged in");
                }


                //user is logged in - lets check who is the user
                Models.User modelsUser = context.GetUSerFromDB(userEmail);

                return Ok(new DTO.User(modelsUser));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("UploadProfileImage")]
        public async Task<IActionResult> UploadProfileImageAsync(IFormFile file)
        {
            //Check if user is logged in 
            string userEmail = HttpContext.Session.GetString("loggedInUser");
            //string userEmail = "ofer@ofer.com";
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized("User is not logged in");
            }

            //Read user from database
            Models.User modelsUser = context.GetUSerFromDB(userEmail);

            //Read all files sent
            long imagesSize = 0, nonImagesSize=0;
            int images = 0, nonImages = 0;
            
            if (file.Length > 0)
            {
                //Check the file extention!
                string []  allowedExtentions = { ".png", ".jpg" };
                string extention = "";
                if (file.FileName.LastIndexOf(".") > 0)
                {
                    extention = file.FileName.Substring(file.FileName.LastIndexOf(".")).ToLower();
                }
                if (!allowedExtentions.Where(e => e == extention).Any()) 
                {
                    //Extention is not supported
                    return BadRequest("File sent with non supported extention");
                }
                    
                //Build path in the web root (better to a specific folder under the web root
                string filePath = $"{this.webHostEnvironment.WebRootPath}\\{modelsUser.Email}{extention}";
                    
                using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);

                    if (IsImage(stream))
                    {
                        images++;
                        imagesSize += stream.Length;
                    }
                    else
                    {
                        //Delete the file if it is not supported!
                        System.IO.File.Delete(filePath);
                        nonImages++;
                        nonImagesSize += stream.Length;
                    }

                }
                
            }

            
            return Ok(new { CountImages = images, SumImagesSize = imagesSize, CountNonImages = nonImages, SumNonImagesSize = nonImagesSize, });
        }

        private static bool IsImage(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            List<string> jpg = new List<string> { "FF", "D8" };
            List<string> bmp = new List<string> { "42", "4D" };
            List<string> gif = new List<string> { "47", "49", "46" };
            List<string> png = new List<string> { "89", "50", "4E", "47", "0D", "0A", "1A", "0A" };
            List<List<string>> imgTypes = new List<List<string>> { jpg, bmp, gif, png };

            List<string> bytesIterated = new List<string>();

            for (int i = 0; i < 8; i++)
            {
                string bit = stream.ReadByte().ToString("X2");
                bytesIterated.Add(bit);

                bool isImage = imgTypes.Any(img => !img.Except(bytesIterated).Any());
                if (isImage)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
