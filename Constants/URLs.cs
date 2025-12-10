/*
===================================================================================
 Author          : ---
 Institution     : Universidad Politécnica de Madrid (UPM)
 Course          : ---
 Description     : Centralized class for all URLs used in the application, including
                   web services for loading settings, saving results, and the main website.
 Last Modified   : 2025-08-13
===================================================================================
*/

namespace Kinect_Middleware.Models
{
    /// <summary>
    /// Holds all URLs used in the application for web services and website access.
    /// </summary>
    internal class URLs
    {
        // URL to load user-specific settings from the server
        static public string LoadSettingsByUser = "http://blexer-med.citsem.upm.es/blexermed/ws/LoadSettingsByUser.php";

        // URL to save user results to the server
        static public string SaveResult = "https://blexer-med.citsem.upm.es/blexermed/ws/saveResult.php";

        // URL of the main website
        static public string Website = "https://blexer-med.citsem.upm.es/index.php";
    }
}
