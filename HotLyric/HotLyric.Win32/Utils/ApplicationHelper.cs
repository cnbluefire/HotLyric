using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Services.Store;
using Windows.Storage;

namespace HotLyric.Win32.Utils
{
    public static class ApplicationHelper
    {
        private static HttpClient? client;
        private static StoreContext? storeContext;

        public static bool RestartRequested { get; private set; }

        public static async Task<Package?> TryGetPackageFromAppUserModelIdAsync(string appUserModelId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!string.IsNullOrEmpty(appUserModelId))
                    {
                        try
                        {
                            var packageId = "";

                            if (appUserModelId.EndsWith("!app", StringComparison.OrdinalIgnoreCase))
                            {
                                packageId = appUserModelId.Substring(0, appUserModelId.Length - 4);
                            }
                            else if (appUserModelId.IndexOf("!") is int index && index >= 0)
                            {
                                packageId = appUserModelId.Substring(0, index);
                            }
                            else
                            {
                                packageId = appUserModelId;
                            }

                            var packageManager = new PackageManager();
                            return packageManager.FindPackagesForUser(string.Empty, packageId).OrderByDescending(c =>
                            {
                                try
                                {
                                    var v = c.Id.Version;
                                    return new Version(v.Major, v.Minor, v.Build, v.Revision);
                                }
                                catch { }
                                return new Version(0, 0, 0, 0);
                            }).FirstOrDefault();
                        }
                        catch { }
                    }
                    return null;
                }, cancellationToken);
            }
            catch { }
            return null;
        }

        public static async Task<bool> TryLaunchAppAsync(string packageFamilyNamePrefix, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(packageFamilyNamePrefix)) return false;

            try
            {
                return await Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var packageManager = new PackageManager();
                        var package = packageManager.FindPackagesForUser(string.Empty)
                            .Where(c => c.Id.FamilyName.StartsWith(packageFamilyNamePrefix, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(c =>
                            {
                                try
                                {
                                    var v = c.Id.Version;
                                    return new Version(v.Major, v.Minor, v.Build, v.Revision);
                                }
                                catch { }
                                return new Version(0, 0, 0, 0);
                            }).FirstOrDefault();

                        var entryPoint = await GetApplicationIdAsync(package);

                        if (string.IsNullOrEmpty(entryPoint)) entryPoint = "App";

                        var path = GetAppDataFolderLocation(package);

                        LaunchByAMUID($"{package.Id.FamilyName}!{entryPoint}");

                        //Process.Start("explorer.exe", $"shell:AppsFolder\\{package.Id.FamilyName}!App");
                        return true;
                    }
                    catch { }
                    return false;
                }, cancellationToken);
            }
            catch { }
            return false;
        }

        private static async Task<string?> GetApplicationIdAsync(Package package)
        {
            if (package == null) return null;

            try
            {
                var appxManifest = (await package.InstalledLocation.TryGetItemAsync("AppxManifest.xml")) as StorageFile;
                if (appxManifest != null)
                {
                    var content = await FileIO.ReadTextAsync(appxManifest);
                    var xdoc = XDocument.Parse(content);

                    var applicationNode = xdoc.Nodes().OfType<XElement>().FirstOrDefault(c => c.Name?.LocalName == "Package")?
                        .Nodes().OfType<XElement>().FirstOrDefault(c => c.Name?.LocalName == "Applications")?
                        .Nodes().OfType<XElement>().FirstOrDefault(c => c.Name?.LocalName == "Application");

                    if (applicationNode != null)
                    {
                        return applicationNode.Attribute("Id")?.Value;
                    }
                }

            }
            catch { }

            return null;
        }

        public static string? GetAppDataFolderLocation(Package package)
        {
            if (package == null) return null;

            try
            {
                // 即使应用安装到非系统盘，依旧会在用户文件夹里建立链接，所以直接访问用户目录即可
                var appdataPackages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
                var packageAppDataFolder = Path.Combine(appdataPackages, package.Id.FamilyName);
                
                if (Directory.Exists(packageAppDataFolder))
                {
                    return packageAppDataFolder;
                }
            }
            catch { }

            return null;
        }

        public static async Task<BitmapImage?> GetPackageIconAsync(Package package, CancellationToken cancellationToken = default)
        {
            var stream = await OpenIconStreamAsync(package, cancellationToken);

            if (stream == null) return null;

            BitmapImage? image = null;

            var func = new Action(() =>
            {
                image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.EndInit();
            });

            if (DispatcherHelper.UIDispatcher != null && !DispatcherHelper.UIDispatcher.CheckAccess())
            {
                await DispatcherHelper.UIDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, func);
            }
            else
            {
                func.Invoke();
            }
            return image;
        }

        private static async Task<Stream?> OpenIconStreamAsync(Package package, CancellationToken cancellationToken = default)
        {
            try
            {
                var logo = package?.Logo;

                if (logo == null) return null;

                var stream = new MemoryStream();

                if (string.Equals(logo.Scheme, "file", StringComparison.OrdinalIgnoreCase))
                {
                    var path = Uri.UnescapeDataString(logo.AbsolutePath).Replace("/", "\\");
                    if (File.Exists(path))
                    {
                        using (var fileStream = File.OpenRead(path))
                        {
                            await fileStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                            await stream.FlushAsync().ConfigureAwait(false);
                        }
                        return stream;
                    }
                }
                else if (string.Equals(logo.Scheme, "file", StringComparison.OrdinalIgnoreCase))
                {
                    if (client == null) client = new HttpClient();

                    var response = await client.GetAsync(logo, cancellationToken).ConfigureAwait(false);
                    using (var internetStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        await internetStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                        await stream.FlushAsync().ConfigureAwait(false);
                    }

                    return stream;
                }

            }
            catch { }

            return null;
        }


        public static bool LaunchByAMUID(string pfn)
        {
            if (string.IsNullOrEmpty(pfn)) return false;

            var launcher = new ApplicationActivationManager();
            try
            {
                return launcher.ActivateApplication(pfn, null, ActivateOptions.None, out var pid) == IntPtr.Zero;
            }
            catch { }
            return false;
        }


        public static async Task RestartApplicationAsync()
        {
            RestartRequested = true;
            SingleInstance.TryReleaseMutex();

            if (DispatcherHelper.UIDispatcher != null)
            {
                if (DispatcherHelper.UIDispatcher.CheckAccess())
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("hot-lyric:"));
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();

                    _ = DispatcherHelper.UIDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(async () =>
                    {
                        try
                        {
                            await Windows.System.Launcher.LaunchUriAsync(new Uri("hot-lyric:///?restart=true"));
                            tcs.SetResult(true);
                        }
                        catch
                        {
                            tcs.SetResult(false);
                        }
                    }));

                    await tcs.Task;
                }
            }

            System.Windows.Application.Current.Shutdown();
        }

        public static async Task<ApplicationUpdateResult> CheckUpdateAsync()
        {
            try
            {
                if (storeContext == null)
                {
                    storeContext = StoreContext.GetDefault();
                    var initWindow = (IInitializeWithWindow)(object)storeContext;
                    initWindow.Initialize(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
                }

                var updates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
                return new ApplicationUpdateResult(updates);
            }
            catch { }
            return new ApplicationUpdateResult(null);
        }

        private enum ActivateOptions
        {
            /// <summary>
            /// The none
            /// </summary>
            None = 0x00000000,  // No flags set
            /// <summary>
            /// The design mode
            /// </summary>
            DesignMode = 0x00000001,  // The application is being activated for design mode, and thus will not be able to
                                      // to create an immersive window. Window creation must be done by design tools which
                                      // load the necessary components by communicating with a designer-specified service on
                                      // the site chain established on the activation manager.  The splash screen normally
                                      // shown when an application is activated will also not appear.  Most activations
                                      // will not use this flag.
            /// <summary>
            /// The no error UI
            /// </summary>
            NoErrorUI = 0x00000002,  // Do not show an error dialog if the app fails to activate.                                
            /// <summary>
            /// The no splash screen
            /// </summary>
            NoSplashScreen = 0x00000004,  // Do not show the splash screen when activating the app.
        }

        /// <summary>
        /// Interface IApplicationActivationManager
        /// </summary>
        [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IApplicationActivationManager
        {
            // Activates the specified immersive application for the "Launch" contract, passing the provided arguments
            // string into the application.  Callers can obtain the process Id of the application instance fulfilling this contract.
            /// <summary>
            /// Activates the application.
            /// </summary>
            /// <param name="appUserModelId">The application user model identifier.</param>
            /// <param name="arguments">The arguments.</param>
            /// <param name="options">The options.</param>
            /// <param name="processId">The process identifier.</param>
            /// <returns>IntPtr.</returns>
            IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);
            /// <summary>
            /// Activates for file.
            /// </summary>
            /// <param name="appUserModelId">The application user model identifier.</param>
            /// <param name="itemArray">The item array.</param>
            /// <param name="verb">The verb.</param>
            /// <param name="processId">The process identifier.</param>
            /// <returns>IntPtr.</returns>
            IntPtr ActivateForFile([In] String appUserModelId, [In] IntPtr /*IShellItemArray* */ itemArray, [In] String verb, [Out] out UInt32 processId);
            /// <summary>
            /// Activates for protocol.
            /// </summary>
            /// <param name="appUserModelId">The application user model identifier.</param>
            /// <param name="itemArray">The item array.</param>
            /// <param name="processId">The process identifier.</param>
            /// <returns>IntPtr.</returns>
            IntPtr ActivateForProtocol([In] String appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out UInt32 processId);
        }

        /// <summary>
        /// Class ApplicationActivationManager.
        /// </summary>
        /// <remarks>
        ///     implementation was made from community members at stackoverflow http://stackoverflow.com/questions/12925748/iapplicationactivationmanageractivateapplication-in-c
        /// </remarks>
        [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]//Application Activation Manager
        private class ApplicationActivationManager : IApplicationActivationManager
        {
            /// <summary>
            /// Activates the application.
            /// </summary>
            /// <param name="appUserModelId">The application user model identifier.</param>
            /// <param name="arguments">The arguments.</param>
            /// <param name="options">The options.</param>
            /// <param name="processId">The process identifier.</param>
            /// <returns>IntPtr.</returns>
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)/*, PreserveSig*/]
            public extern IntPtr ActivateApplication([In] string? appUserModelId, [In] string? arguments, [In] ActivateOptions options, [Out] out UInt32 processId);
            /// <summary>
            /// Activates for file.
            /// </summary>
            /// <param name="appUserModelId">The application user model identifier.</param>
            /// <param name="itemArray">The item array.</param>
            /// <param name="verb">The verb.</param>
            /// <param name="processId">The process identifier.</param>
            /// <returns>IntPtr.</returns>
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public extern IntPtr ActivateForFile([In] String appUserModelId, [In] IntPtr /*IShellItemArray* */ itemArray, [In] String verb, [Out] out UInt32 processId);
            /// <summary>
            /// Activates for protocol.
            /// </summary>
            /// <param name="appUserModelId">The application user model identifier.</param>
            /// <param name="itemArray">The item array.</param>
            /// <param name="processId">The process identifier.</param>
            /// <returns>IntPtr.</returns>
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public extern IntPtr ActivateForProtocol([In] String appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out UInt32 processId);
        }

        public class ApplicationUpdateResult
        {
            private readonly IReadOnlyList<StorePackageUpdate>? updates;

            internal ApplicationUpdateResult(IReadOnlyList<StorePackageUpdate>? updates)
            {
                this.updates = updates;
                HasUpdate = updates != null && updates.Count > 0;
            }


            public bool HasUpdate { get; }

            public async Task TryStartUpdateAsync()
            {
                try
                {
                    if (storeContext == null || !HasUpdate || !storeContext.CanSilentlyDownloadStorePackageUpdates) return;

                    await storeContext.TrySilentDownloadAndInstallStorePackageUpdatesAsync(updates);
                }
                catch { }
            }
        }
    }
}

