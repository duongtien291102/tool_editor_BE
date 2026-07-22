

namespace AiVideoStudio.Shared.DomainErrors;

public static class ScriptErrors
{
    public static readonly Error NotFound = new(
        "Script.NotFound",
        "The script with the specified ID was not found.");

    public static readonly Error Unauthorized = new(
        "Script.Unauthorized",
        "You do not have permission to access or modify this script.");

    public static readonly Error VersionConflict = new(
        "Script.VersionConflict",
        "The script has been modified by another user. Please refresh and try again.");
        
    public static readonly Error SceneNotFound = new(
        "Script.SceneNotFound",
        "The specified scene was not found in the script.");
        
    public static readonly Error ElementNotFound = new(
        "Script.ElementNotFound",
        "The specified scene element was not found in the script.");
}
