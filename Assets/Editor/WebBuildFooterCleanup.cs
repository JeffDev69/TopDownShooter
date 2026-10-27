using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;

public static class WebBuildFooterCleanup
{
    [PostProcessBuild(100)]
    public static void RemoveFooterTitle(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.WebGL) return;

        string indexPath = Path.Combine(buildPath, "index.html");
        if (!File.Exists(indexPath)) return;

        string html = File.ReadAllText(indexPath);
        // Remove the default template's product label, keeping its fullscreen button.
        string updatedHtml = Regex.Replace(html,
            @"[ \t]*<div id=""unity-build-title"">.*?</div>\r?\n?",
            string.Empty, RegexOptions.Singleline);

        // Let simultaneous thumb gestures reach Unity without scrolling the page.
        if (!updatedHtml.Contains("id=\"mobile-touch-style\""))
            updatedHtml = updatedHtml.Replace("</head>",
                "<style id=\"mobile-touch-style\">#unity-canvas { touch-action: none; }</style>\n  </head>");
        // Avoid rendering at an unnecessarily large resolution on high-DPI phones.
        updatedHtml = updatedHtml.Replace("// config.devicePixelRatio = 1;",
            "config.devicePixelRatio = Math.min(window.devicePixelRatio || 1, 2);");

        if (updatedHtml != html)
            File.WriteAllText(indexPath, updatedHtml, new UTF8Encoding(false));
    }
}
