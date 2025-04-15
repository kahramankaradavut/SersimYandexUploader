// using System.Net;
// using WebDav;

// public class YandexDiskService
// {
//     private readonly WebDavClient _client;

//     public YandexDiskService(string username, string appPassword)
//     {
//         var parameters = new WebDavClientParams
//         {
//             BaseAddress = new Uri("https://webdav.yandex.com/"),
//             Credentials = new NetworkCredential(username, appPassword)
//         };
//         _client = new WebDavClient(parameters);
//     }

//     public async Task<bool> CreateFolderAsync(string folderName)
//     {
//         var result = await _client.Mkcol(folderName);
//         return result.IsSuccessful;
//     }

//     public async Task<bool> UploadFileAsync(string folderPath, string fileName, byte[] fileContent)
//     {
//         var fullPath = $"{folderPath}/{fileName}";
//         using var ms = new MemoryStream(fileContent);
//         var result = await _client.PutFile(fullPath, ms);
//         return result.IsSuccessful;
//     }

//     public string GetPublicLink(string folderPath)
//     {
//         return $"https://disk.yandex.com/d{folderPath}";
//     }
// }
