using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TheGame;

public static class Program
{ 
    public static void Main(string[] args)
    {
        using var game = new Main();
        game.Assets = GetAssets(game.Content.RootDirectory);
        game.Run();
    }

    /// <summary>
    /// Перечисляем весь контент
    /// </summary>
    public static IList<string> GetAssets(string path)
    {
        var assets = new List<string>();

        var dir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path));        
        foreach (var file in dir.GetFiles("*.*"))
        {
            assets.Add(Path.Combine(path,  Path.GetFileNameWithoutExtension(file.Name)));
        }
        foreach (var directory in dir.GetDirectories())
        {
            assets.AddRange(GetAssets(Path.Combine(path, directory.Name)));
        }

        return assets;
    }
}