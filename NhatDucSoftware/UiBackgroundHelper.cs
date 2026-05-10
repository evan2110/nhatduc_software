namespace NhatDucSoftware;

public static class UiBackgroundHelper
{
    private static Image? _cachedBackground;

    public static void ApplyBackground(Form form)
    {
        var image = GetBackgroundImage();
        if (image is null)
        {
            return;
        }

        form.BackgroundImage = image;
        form.BackgroundImageLayout = ImageLayout.Stretch;
    }

    private static Image? GetBackgroundImage()
    {
        if (_cachedBackground is not null)
        {
            return _cachedBackground;
        }

        var imagePath = Path.Combine(AppPaths.InstallDirectory, "Assets", "background.jpg");
        if (!File.Exists(imagePath))
        {
            return null;
        }

        using var source = Image.FromFile(imagePath);
        _cachedBackground = new Bitmap(source);
        return _cachedBackground;
    }
}
