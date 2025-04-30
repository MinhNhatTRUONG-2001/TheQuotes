using Imagekit.Models;
using Imagekit.Sdk;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using QuoteApi.Data;

namespace server.Controllers.Helpers
{
    public class FileUploadHelper
    {
        public static async Task<List<string>> UpdateUserAvatarToImageKit(QuoteContext _context, User user, IFormFile? file, List<string>? tags)
        {
            ImagekitClient imagekit = new ImagekitClient(
                Environment.GetEnvironmentVariable("IMAGEKIT_PUBLIC_KEY"),
                Environment.GetEnvironmentVariable("IMAGEKIT_PRIVATE_KEY"),
                Environment.GetEnvironmentVariable("IMAGEKIT_URL_ENDPOINT")
            );
            using (var memoryStream = new MemoryStream())
            {
                string? fileUrl = user.avatar_url;
                string filePath = "the-quotes/avatars";

                // Delete old file if exists
                if (!string.IsNullOrEmpty(fileUrl))
                {
                    string fileName = fileUrl.Split('/').Last();
                    fileName = fileName.Split('?')[0]; // Remove query string if exists
                    GetFileListRequest model = new GetFileListRequest
                    {
                        SearchQuery = $"name = \"{fileName}\"",
                        Path = filePath
                    };
                    ResultList ikResponseList = await imagekit.GetFileListRequestAsync(model);
                    if (ikResponseList.HttpStatusCode == 200)
                    {
                        if (ikResponseList.FileList.Count == 0) // In case the file URL still exists in the database after file deletion in the previous request
                        {
                            user.avatar_url = null;
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            Root fileInfo = ikResponseList.FileList[0];
                            string fileId = fileInfo.fileId;
                            ResultDelete ikResponseDelete = await imagekit.DeleteFileAsync(fileId);
                            if (ikResponseDelete.HttpStatusCode == 204)
                            {
                                user.avatar_url = null;
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                return new List<string> { "BadRequest", "Error while deleting old file." };
                            }
                        }
                    }
                    else
                    {
                        return new List<string> { "BadRequest", "Error while deleting old file." };
                    }
                }

                // If IFormFile file is specified, save new file to Imagekit media library & file URL to database
                if (file != null)
                {
                    await file.CopyToAsync(memoryStream);
                    byte[] bytes = memoryStream.ToArray();
                    string newFileName = user.id.ToString() + "_" + file.FileName;
                    FileCreateRequest ob = new FileCreateRequest
                    {
                        file = bytes,
                        fileName = newFileName,
                        folder = "/" + filePath,
                        tags = tags
                    };
                    Result ikResponse = await imagekit.UploadAsync(ob);
                    if (ikResponse.HttpStatusCode == 200)
                    {
                        try {
                            string fileId = ikResponse.fileId;
                            var fileDetailResponse = await imagekit.GetFileDetailAsync(fileId);
                            JObject jsonResponse = JObject.Parse(fileDetailResponse.Raw);
                            user.avatar_url = jsonResponse["url"]?.ToString() ?? "";
                            user.last_updated = DateTime.UtcNow;
                            _context.Entry(user).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                            return new List<string> { "Ok", ikResponse.url };
                        }
                        catch (Exception) // In case the file URL cannot be saved in the database after file uploading
                        {
                            await imagekit.DeleteFileAsync(ikResponse.fileId);
                            return new List<string> { "BadRequest", "Error while saving new file." };
                        }
                    }
                    else
                    {
                        return new List<string> { "BadRequest", "Error while saving new file." };
                    }
                }
                // If IFormFile file is null, it means this method just deletes the file
                else
                {
                    return new List<string> { "Ok", "File was deleted." };
                }
            }
        }
    }
}
