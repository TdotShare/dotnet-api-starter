namespace dotnet_api_starter.Params.Inputs.User
{
    public class PostUploadAttachedUserInput
    {
        public string UserId { get; set; }
        public IFormFile? FileData { get; set; }
    }
}
