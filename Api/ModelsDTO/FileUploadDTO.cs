using System.ComponentModel.DataAnnotations;

namespace Api.ModelsDTO
{
    public class FileUploadDTO
    {
        [Required]
        public IFormFile? File { get; set; }
    }
}
