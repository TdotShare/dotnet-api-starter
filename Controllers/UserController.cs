using Dapper;
using dotnet_api_starter.Params.Inputs.User;
using dotnet_api_starter.Params.Outputs.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Collections.Generic;
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
        public async Task<Boolean> CreateUser(PostCreateUserInput postCreateUserInput)
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

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task<Boolean> UpdateUser(PostUpdateUserInput postUpdateUserInput)
        {
            try
            {
                using (var conn = new MySqlConnection(_configuration.GetConnectionString("Default")))
                {
                    await conn.ExecuteAsync(@"UPDATE dt_user SET user_username = @user_username , user_password = @user_password  WHERE user_id = @user_id",
                        new
                        {
                            user_id = postUpdateUserInput.user_id,
                            user_username = postUpdateUserInput.user_username,
                            user_password = Convert.ToBase64String(Encoding.UTF8.GetBytes(postUpdateUserInput.user_password))
                        }
                    );

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
