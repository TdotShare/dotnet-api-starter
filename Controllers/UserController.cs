using Dapper;
using dotnet_api_starter.Params;
using dotnet_api_starter.Params.Inputs.User;
using dotnet_api_starter.Params.Outputs.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace dotnet_api_starter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {

        private readonly IConfiguration _configuration;

        public UserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpGet("GetUser/{id}")]
        public async Task<GetUserOutput> GetUser(int id)
        {
            try
            {
                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {
                    var data = await conn.QueryFirstAsync<GetUserOutput>(@"SELECT * FROM dt_user WHERE user_id =  @user_id", new { user_id = id });

                    if (data == null)
                    {
                        return new GetUserOutput();
                    }

                    return data;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet("GetUserAll")]
        public async Task<IEnumerable<GetUserAllOutput>> GetUserAll()
        {
            try
            {
                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {
                    var data = await conn.QueryAsync<GetUserAllOutput>(@"SELECT * FROM dt_user");

                    if (data.Count() == 0)
                    {
                        return new List<GetUserAllOutput>();
                    }

                    return data;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        [HttpPost("CreateUser")]
        public async Task<ResponseMessage> CreateUser(PostCreateUserInput postCreateUserInput)
        {
            try
            {
                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {
                    await conn.ExecuteAsync(@"INSERT INTO dt_user (user_title_id, user_username , user_password , user_create_at , user_update_at  ) VALUES (@user_title_id,@user_username,@user_password , NOW() , NOW() )",
                        new
                        {
                            user_title_id = 1,
                            user_username = postCreateUserInput.user_username,
                            user_password = Convert.ToBase64String(Encoding.UTF8.GetBytes(postCreateUserInput.user_password))
                        }
                    );

                    var LastId = await conn.QueryFirstAsync<int>(@"SELECT MAX(user_id) FROM dt_user");

                    return new ResponseMessage() { bypass = true , msg = "CreateUser Successful !" , data  = LastId.ToString() };
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        [HttpPost("UpdateUser")]
        public async Task<string> UpdateUser(PostUpdateUserInput postUpdateUserInput)
        {
            try
            {

                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {

                    var data = await conn.QueryFirstAsync<GetUserOutput>(@"SELECT * FROM dt_user WHERE user_id =  @user_id", new { user_id = postUpdateUserInput.user_id });

                    if (data == null)
                    {
                        return "Not Found Data !";
                    }


                    await conn.ExecuteAsync(@"UPDATE dt_user SET user_username = @user_username , user_password = @user_password  WHERE user_id = @user_id",
                        new
                        {
                            user_id = postUpdateUserInput.user_id,
                            user_username = postUpdateUserInput.user_username,
                            user_password = Convert.ToBase64String(Encoding.UTF8.GetBytes(postUpdateUserInput.user_password))
                        }
                    );

                    return "UpdateUser Successful !";
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet("DeleteUser/{id}")]
        public async Task<string> DeleteUser(int id)
        {
            try
            {

                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {


                    await conn.ExecuteAsync(@"DELETE FROM dt_user WHERE user_id = @user_id",
                         new
                         {
                             user_id = id,
                         }
                    );

                    return "DeleteUser Successful !";
                }
                 
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost("UploadAttachedUser")]
        public async Task<string> UploadAttachedUser([FromForm] PostUploadAttachedUserInput postUploadAttachedUserInput)
        {
            try
            {

                var path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"Uploads"));
                this.CreateFolderUpload(path);

                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {


                    if (postUploadAttachedUserInput.FileData != null)
                    {
                        using (var fileStream = new FileStream(Path.Combine(path, postUploadAttachedUserInput.FileData.FileName), FileMode.Create))
                        {
                            await postUploadAttachedUserInput.FileData.CopyToAsync(fileStream);
                        }

                        await conn.ExecuteAsync(@"INSERT INTO dt_attach (attachUserId , attachFileName  , attachCreateAt , attachUpdateAt  ) 
                                              VALUES (@attachUserId , @attachFileName , NOW() , NOW() )",
                            new
                            {
                                attachUserId = postUploadAttachedUserInput.UserId,
                                attachFileName = postUploadAttachedUserInput.FileData.FileName,
                            }
                        );

                        return "UploadAttachedUser Successful !";
                    }
                    else
                    {
                        return "Not Found FileUpload !!";
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("DeleteUploadAttachedUser/{id}")]
        public async Task<string> DeleteUploadAttachedUser(int id)
        {
            try
            {
                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {

                    var data = await conn.QueryFirstAsync<GetAttachUserOutput>(@"SELECT * FROM dt_attach WHERE attachId = @attachId", new { attachId = id });

                    if (data == null)
                    {
                        return "Not Found Data !";
                    }

                    await conn.ExecuteAsync(@"DELETE FROM dt_attach WHERE attachId = @attachId",
                         new
                         {
                             attachId = id,
                         }
                    );

                    var path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"Uploads\" + data.attachFileName));

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    return "DeleteUploadAttachedUser Successful !";
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        private void CreateFolderUpload(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

        }
    }
}
