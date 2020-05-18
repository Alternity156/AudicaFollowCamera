using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;

[assembly: AssemblyTitle(FollowCamera.BuildInfo.Name)]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(FollowCamera.BuildInfo.Company)]
[assembly: AssemblyProduct(FollowCamera.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + FollowCamera.BuildInfo.Author)]
[assembly: AssemblyTrademark(FollowCamera.BuildInfo.Company)]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
//[assembly: Guid("")]
[assembly: AssemblyVersion(FollowCamera.BuildInfo.Version)]
[assembly: AssemblyFileVersion(FollowCamera.BuildInfo.Version)]
[assembly: NeutralResourcesLanguage("en")]
[assembly: MelonModInfo(typeof(FollowCamera.FollowCamera), FollowCamera.BuildInfo.Name, FollowCamera.BuildInfo.Version, FollowCamera.BuildInfo.Author, FollowCamera.BuildInfo.DownloadLink)]


// Create and Setup a MelonModGame to mark a Mod as Universal or Compatible with specific Games.
// If no MelonModGameAttribute is found or any of the Values for any MelonModGame on the Mod is null or empty it will be assumed the Mod is Universal.
// Values for MelonModGame can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonModGame(null, null)]