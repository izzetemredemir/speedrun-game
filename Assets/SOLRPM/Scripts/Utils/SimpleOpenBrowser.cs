using UnityEngine;

public static class SimpleOpenBrowser
{
    public static void OpenBrowser(string url)
    {
        Application.OpenURL(url);
    }
}