using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using TheGame;

namespace TheGameAndroid;

[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Icon = "@drawable/icon",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize
)]
public class Activity : AndroidGameActivity
{
    private Main _game;
    private View _view;

    /// <inheritdoc/>
    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

        _game = new Main();
        _game.Assets = GetAssets(_game.Content.RootDirectory);

        _view = _game.Services.GetService(typeof(View)) as View;

        SetContentView(_view);
        _game.Run();
    }

    /// <summary>
    /// Перечислояем весь контент
    /// </summary>
    protected IList<string> GetAssets(string folder)
    {
        var result = new List<string>();

        foreach (var asset in Assets.List(folder))
        {
            if (string.IsNullOrEmpty(Path.GetExtension(asset)))
                result.AddRange(GetAssets(Path.Combine(folder, asset)));
            else
                result.Add(Path.Combine(folder, Path.GetFileNameWithoutExtension(asset)));
        }

        return result;
    }
}
