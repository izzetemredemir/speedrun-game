using UnityEngine;

public static class SimpleGetClipboard
{
    public static string GetClipboardText()
    {
        string clipboardText = GUIUtility.systemCopyBuffer;

        return clipboardText;
    }
}