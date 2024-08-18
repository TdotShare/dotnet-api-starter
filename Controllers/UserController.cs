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
                    var data = await conn.QueryAsync<GetUserAllOutput>(@"SELECT 
                                                                        u.user_id , 
                                                                        u.user_title_id , 
                                                                        u.user_username , 
                                                                        u.user_password , 
                                                                        u.user_create_at ,
                                                                        u.user_update_at ,
                                                                        a.attachFileName 
                                                                        FROM dt_user u LEFT JOIN dt_attach a ON u.user_id = a.attachUserId");

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

                    return new ResponseMessage() { bypass = true, msg = "CreateUser Successful !", data = LastId.ToString() };
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        [HttpPost("UpdateUser")]
        public async Task<ResponseMessage> UpdateUser(PostUpdateUserInput postUpdateUserInput)
        {
            try
            {

                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {

                    var data = await conn.QueryFirstAsync<GetUserOutput>(@"SELECT * FROM dt_user WHERE user_id =  @user_id", new { user_id = postUpdateUserInput.user_id });

                    if (data == null)
                    {
                        return new ResponseMessage() { bypass = true, msg = "Not Found Data !", data = "" };
                    }


                    await conn.ExecuteAsync(@"UPDATE dt_user SET user_username = @user_username , user_password = @user_password  WHERE user_id = @user_id",
                        new
                        {
                            user_id = postUpdateUserInput.user_id,
                            user_username = postUpdateUserInput.user_username,
                            user_password = Convert.ToBase64String(Encoding.UTF8.GetBytes(postUpdateUserInput.user_password))
                        }
                    );

                    return new ResponseMessage() { bypass = true, msg = "UpdateUser Successful !", data = "" };
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet("DeleteUser/{id}")]
        public async Task<ResponseMessage> DeleteUser(int id)
        {
            try
            {
                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {

                    var fileData = await conn.QueryFirstAsync<GetAttachUserOutput>(@"SELECT * FROM dt_attach WHERE attachUserId = @attachUserId", new { attachUserId = id });

                    if (fileData != null)
                    {
                        await conn.ExecuteAsync(@"DELETE FROM dt_attach WHERE attachId = @attachId", new {attachId = fileData.attachId,});

                        var path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"Uploads\" + fileData.attachFileName));

                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }
                    }

                    await conn.ExecuteAsync(@"DELETE FROM dt_user WHERE user_id = @user_id",
                         new
                         {
                             user_id = id,
                         }
                    );

                    return new ResponseMessage() { bypass = true, msg = "DeleteUser Successful !", data = "" };
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost("UploadAttachedUser")]
        public async Task<ResponseMessage> UploadAttachedUser([FromForm] PostUploadAttachedUserInput postUploadAttachedUserInput)
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


                        return new ResponseMessage() { bypass = true, msg = "UploadAttachedUser Successful !", data = "" };
                    }
                    else
                    {

                        return new ResponseMessage() { bypass = true, msg = "Not Found FileUpload !!", data = "" };
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet("DeleteUploadAttachedUser/{id}")]
        public async Task<ResponseMessage> DeleteUploadAttachedUser(int id)
        {
            try
            {
                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {

                    var data = await conn.QueryFirstAsync<GetAttachUserOutput>(@"SELECT * FROM dt_attach WHERE attachId = @attachId", new { attachId = id });

                    if (data == null)
                    {
                        return new ResponseMessage() { bypass = true, msg = "Not Found Data !", data = "" };
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

                    return new ResponseMessage() { bypass = true, msg = "DeleteUploadAttachedUser Successful !", data = "" };
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        [HttpPost("GetSearchUser")]
        public async Task<IEnumerable<GetSearchUserOutput>> GetSearchUser(PostSearchUserInput postSearchUserInput)
        {
            try
            {
                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {
                    var data = await conn.QueryAsync<GetSearchUserOutput>(@"SELECT u.user_id , u.user_title_id , u.user_username , u.user_password , u.user_create_at , u.user_update_at , a.attachFileName FROM dt_user u LEFT JOIN dt_attach a ON u.user_id = a.attachUserId WHERE user_username LIKE @user_username OR user_password LIKE @user_password",
                        new
                        {
                            user_username = $"%{postSearchUserInput.searchUser}%",
                            user_password = $"%{postSearchUserInput.searchUser}%",
                        });

                    if (data.Count() == 0)
                    {
                        return new List<GetSearchUserOutput>();
                    }

                    return data;
                }

            }
            catch (Exception ex)
            {
                throw ex;
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
