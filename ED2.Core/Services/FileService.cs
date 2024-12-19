using System.Text;

using ED2.Core.Contracts.Services;
using LiteDB;
using Newtonsoft.Json;

namespace ED2.Core.Services;

public class FileService : IFileService
{
    public async Task<T> Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json);
        }

        return default;
    }

    public async Task Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fileContent = JsonConvert.SerializeObject(content);
        await File.WriteAllTextAsync(Path.Combine(folderPath, fileName), fileContent, Encoding.UTF8).ConfigureAwait(false);
    }

    public Task Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
            File.Delete(Path.Combine(folderPath, fileName));
        return Task.CompletedTask;
    }

    ILiteDatabase db;
    public ILiteDatabase GetSettingsDatabase(string folderPath) =>
        db ??= new LiteDatabase(Path.Combine(folderPath, "storage.db"));
}
