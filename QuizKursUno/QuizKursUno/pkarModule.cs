using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pkar.UI.Extensions;
using static pkar.DotNetExtensions;

using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel;

namespace QuizKursUno;

#nullable disable

public partial class App : Application
{


    /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElseDirectiveTrivia */
    private void OnNavigatedAddBackButton(object sender, object e)
    {
        // tak naprawdę e to NavigationEventArgs, ale do tego trzeba imports Microsoft.UI.Xaml.Navigation (na WinUI3, bo na UWP nie trzeba)
        try
        {
            Frame oFrame = sender as Frame;
            if (oFrame == null)
                return;
            if (!oFrame.CanGoBack)
                return;

            Page oPage = oFrame.Content as Page;
            if (oPage == null)
                return;

            Grid oGrid = oPage.Content as Grid;
            if (oGrid == null)
                return;

            Button oButton = new Button() { Content = new SymbolIcon(Symbol.Back), Name = "uiPkAutoBackButton", VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
            oButton.Click += OnBackButtonPressed;

            int iCols = 0;
            if (oGrid.ColumnDefinitions != null)
                iCols = oGrid.ColumnDefinitions.Count; // może być 0
            int iRows = 0;
            if (oGrid.RowDefinitions != null)
                iRows = oGrid.RowDefinitions.Count; // może być 0
            if (iRows > 1)
            {
                Grid.SetRow(oButton, 0);
                Grid.SetRowSpan(oButton, iRows);
            }
            if (iCols > 1)
            {
                Grid.SetColumn(oButton, 0);
                Grid.SetColumnSpan(oButton, iCols);
            }
            oGrid.Children.Add(oButton);
        }

        catch (Exception ex)
        {
            pkarUno.CrashMessageExit("@OnNavigatedAddBackButton", ex.Message);
        }
    }

    private void OnBackButtonPressed(object sender, RoutedEventArgs e)
    {
        FrameworkElement oFE = sender as FrameworkElement;
        Page oPage = null/* TODO Change to default(_) if this is not a reference type */;

        while (true)
        {
            oPage = oFE as Page;
            if (oPage != null)
                break;
            oFE = oFE.Parent as FrameworkElement;
            if (oFE == null)
                return;
        }

        oPage.GoBack();
    }
    /* TODO ERROR: Skipped EndIfDirectiveTrivia */

    private Windows.ApplicationModel.Background.BackgroundTaskDeferral moTaskDeferal = null/* TODO Change to default(_) if this is not a reference type */;
    private Windows.ApplicationModel.AppService.AppServiceConnection moAppConn;
    private string msLocalCmdsHelp = "";

    private void RemSysOnServiceClosed(Windows.ApplicationModel.AppService.AppServiceConnection appCon, Windows.ApplicationModel.AppService.AppServiceClosedEventArgs args)
    {
        if (appCon != null)
            appCon.Dispose();
        if (moTaskDeferal != null)
        {
            moTaskDeferal.Complete();
            moTaskDeferal = null;
        }
    }

    private void RemSysOnTaskCanceled(Windows.ApplicationModel.Background.IBackgroundTaskInstance sender, Windows.ApplicationModel.Background.BackgroundTaskCancellationReason reason)
    {
        if (moTaskDeferal != null)
        {
            moTaskDeferal.Complete();
            moTaskDeferal = null;
        }
    }

    /// <summary>
    ///     ''' do sprawdzania w OnBackgroundActivated
    ///     ''' jak zwróci True, to znaczy że nie wolno zwalniać moTaskDeferal !
    ///     ''' sLocalCmdsHelp: tekst do odesłania na HELP
    ///     ''' </summary>
    public bool RemSysInit(BackgroundActivatedEventArgs args, string sLocalCmdsHelp)
    {
        Windows.ApplicationModel.AppService.AppServiceTriggerDetails oDetails = args.TaskInstance.TriggerDetails as Windows.ApplicationModel.AppService.AppServiceTriggerDetails;
        if (oDetails == null)
            return false;

        msLocalCmdsHelp = sLocalCmdsHelp;

        args.TaskInstance.Canceled += RemSysOnTaskCanceled;
        moAppConn = oDetails.AppServiceConnection;
        moAppConn.RequestReceived += RemSysOnRequestReceived;
        moAppConn.ServiceClosed += RemSysOnServiceClosed;
        return true;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<string> CmdLineOrRemSys(string sCommand)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        string sResult = pkarUno.AppServiceStdCmd(sCommand, msLocalCmdsHelp);
        if (string.IsNullOrEmpty(sResult))
        {
        }

        return sResult;
    }

    public async Task ObsluzCommandLine(string sCommand)
    {
        Windows.Storage.StorageFolder oFold = Windows.Storage.ApplicationData.Current.TemporaryFolder;
        if (oFold == null)
            return;

        string sLockFilepathname = System.IO.Path.Combine(oFold.Path, "cmdline.lock");
        string sResultFilepathname = System.IO.Path.Combine(oFold.Path, "stdout.txt");

        try
        {
            System.IO.File.WriteAllText(sLockFilepathname, "lock");
        }
        catch
        {
            return;
        }

        var sResult = await CmdLineOrRemSys(sCommand);
        if (string.IsNullOrEmpty(sResult))
            sResult = "(empty - probably unrecognized command)";

        System.IO.File.WriteAllText(sResultFilepathname, sResult);

        System.IO.File.Delete(sLockFilepathname);
    }

    private async void RemSysOnRequestReceived(Windows.ApplicationModel.AppService.AppServiceConnection sender, Windows.ApplicationModel.AppService.AppServiceRequestReceivedEventArgs args)
    {
        // // 'Get a deferral so we can use an awaitable API to respond to the message 

        string sStatus;
        string sResult = "";
        Windows.ApplicationModel.AppService.AppServiceDeferral messageDeferral = args.GetDeferral();

        if (VBlib.pkarlibmodule14.GetSettingsBool("remoteSystemDisabled"))
            sStatus = "No permission";
        else
        {
            Windows.Foundation.Collections.ValueSet oInputMsg = args.Request.Message;

            sStatus = "ERROR while processing command";

            if (oInputMsg.ContainsKey("command"))
            {
                // *TODO*
                // string sCommand = oInputMsg("command");
                string sCommand = "command";
                sResult = await CmdLineOrRemSys(sCommand);
            }

            if (sResult != "")
                sStatus = "OK";
        }

        Windows.Foundation.Collections.ValueSet oResultMsg = new Windows.Foundation.Collections.ValueSet();
        oResultMsg.Add("status", sStatus);
        oResultMsg.Add("result", sResult);

        await args.Request.SendResponseAsync(oResultMsg);

        messageDeferral?.Complete();
        moTaskDeferal?.Complete();
    }



    public static void OpenRateIt()
    {
        Uri sUri = new Uri("ms-windows-store://review/?PFN=" + Package.Current.Id.FamilyName);
        sUri.OpenBrowser();
    }
}

public static class pkarUno
{



    // nie dla UWP
    /* TODO ERROR: Skipped IfDirectiveTrivia */
    /// <summary>
    ///     ''' import settingsów JSON z UWP, o ile tam są a tutaj nie ma - wywoływać przed InitLib!
    ///     ''' </summary>
    ///     ''' <param name="packageName">Zobacz w Manifest, Packaging, Package Name</param>
    public static void TryImportSettingsFromUwp(string packageName)
    {
        string sPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string uwpPath = System.IO.Path.Combine(sPath, packageName);
        string wpfPath = System.IO.Path.Combine(sPath, GetAppName());

        // normalne
        // UWP = C:\Users\xxx\AppData\Local\Packages\xxx\LocalState)
        // WPF = WinUI3 = MAUI = C:\Users\xxx\AppData\Local
        TryImportSettingsFromDir(System.IO.Path.Combine(uwpPath, "LocalState"), wpfPath);

        // roaming
        // UWP = C:\Users\xxx\AppData\Local\Packages\xxx\RoamingState)
        // WPF = WinUI3 = MAUI = C:\Users\xxx\AppData\Roaming
        string dirsep = string.Format("%c", System.IO.Path.DirectorySeparatorChar);
        wpfPath = wpfPath.Replace(dirsep + "Local", dirsep + "Roaming");
        TryImportSettingsFromDir(System.IO.Path.Combine(uwpPath, "RoamingState"), wpfPath);
    }

    private static void TryImportSettingsFromDir(string srcDir, string dstDir)
    {
        string JSON_FILENAME = "AppSettings.json";

        string srcFile = System.IO.Path.Combine(srcDir, JSON_FILENAME);
        if (!System.IO.File.Exists(srcFile))
            return;

        string dstFile = System.IO.Path.Combine(dstDir, JSON_FILENAME);
        if (System.IO.File.Exists(dstFile))
            return;

        if (!System.IO.Directory.Exists(dstDir))
            System.IO.Directory.CreateDirectory(dstDir);

        System.IO.File.Copy(srcFile, dstFile);
    }

    private static string GetAppName()
    {
        var sAssemblyFullName = System.Reflection.Assembly.GetEntryAssembly().FullName;
        System.Reflection.AssemblyName oAss = new System.Reflection.AssemblyName(sAssemblyFullName);
        return oAss.Name;
    }


    /* TODO ERROR: Skipped EndIfDirectiveTrivia */


    /// <summary>
    ///     ''' dla starszych: InitLib(Nothing)
    ///     ''' dla nowszych:  InitLib(Environment.GetCommandLineArgs)
    ///     ''' </summary>
    public static void InitLib(List<string> aCmdLineArgs, bool bUseOwnFolderIfNotSD = true)
    {
        /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped ElseDirectiveTrivia */
        pkar.UI.Configs.UnoConfig.InitSettings(VBlib.IniLikeDefaults.sIniContent, false);
        /* TODO ERROR: Skipped EndIfDirectiveTrivia */
        VBlib.pkarlibmodule14.LibInitToast(FromLibMakeToast);
        VBlib.pkarlibmodule14.LibInitDialogBox(FromLibDialogBoxAsync, FromLibDialogBoxYNAsync, FromLibDialogBoxInputAllDirectAsync);

        
        VBlib.pkarlibmodule14.LibInitClip(FromLibClipPut, FromLibClipPutHtml);
    }

    // większość w VBlib

    /// <summary>
    ///     ''' DialogBox z dotychczasowym logiem i skasowanie logu
    ///     ''' </summary>
    public async static Task CrashMessageShowAsync()
    {
        string sTxt = VBlib.pkarlibmodule14.GetSettingsString("appFailData");
        if (sTxt == "")
            return;
        await VBlib.pkarlibmodule14.DialogBoxAsync("FAIL messages:\n" + sTxt);
        VBlib.pkarlibmodule14.SetSettingsString("appFailData", "");
    }

    /// <summary>
    ///     ''' Dodaj do logu, ewentualnie toast, i zakończ App
    ///     ''' </summary>
    public static void CrashMessageExit(string sTxt, string exMsg)
    {
        VBlib.pkarlibmodule14.CrashMessageAdd(sTxt, exMsg);
        (Application.Current as App)?.Exit();
    }


    // -- CLIPBOARD ---------------------------------------------

    private static void FromLibClipPut(string sTxt)
    {
        try
        {
            Windows.ApplicationModel.DataTransfer.DataPackage oClipCont = new Windows.ApplicationModel.DataTransfer.DataPackage()
            {
                RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy
            };
            oClipCont.SetText(sTxt);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(oClipCont);
        }
        catch
        {
        }
    }

    private static void FromLibClipPutHtml(string sHtml)
    {
        Windows.ApplicationModel.DataTransfer.DataPackage oClipCont = new Windows.ApplicationModel.DataTransfer.DataPackage()
        {
            RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy
        };
        oClipCont.SetHtmlFormat(sHtml);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(oClipCont);
    }

    /// <summary>
    ///     ''' w razie Catch() zwraca ""
    ///     ''' </summary>
    public async static Task<string> ClipGetAsync()
    {
        Windows.ApplicationModel.DataTransfer.DataPackageView oClipCont = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
        try
        {
            return await oClipCont.GetTextAsync();
        }
        catch
        {
            return "";
        }
    }


    // -- Testy sieciowe ---------------------------------------------


    public static bool IsFamilyMobile()
    {
        return (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile");
    }

    public static bool IsFamilyDesktop()
    {
        return (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop");
    }


    // <Obsolete("Jest w .Net Standard 2.0 (lib)")>
    public static bool NetIsIPavailable(bool bMsg = false)
    {
        if (VBlib.pkarlibmodule14.GetSettingsBool("offline"))
            return false;

        if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            return true;
        if (bMsg)
            VBlib.pkarlibmodule14.DialogBox("ERROR: no IP network available");
        return false;
    }

    // <Obsolete("Jest w .Net Standard 2.0 (lib), ale on jest nie do telefonu :)")>
    public static bool NetIsCellInet()
    {
        return Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWwanConnectionProfile;
    }


    // <Obsolete("Jest w .Net Standard 2.0 (lib)")>
    public static string GetHostName()
    {
        IReadOnlyList<Windows.Networking.HostName> hostNames = Windows.Networking.Connectivity.NetworkInformation.GetHostNames();
        foreach (Windows.Networking.HostName oItem in hostNames)
        {
            if (oItem.DisplayName.Contains(".local"))
                return oItem.DisplayName.Replace(".local", "");
        }
        return "";
    }

    // <Obsolete("Jest w .Net Standard 2.0 (lib)")>
    /// <summary>
    ///     ''' Ale to chyba przestało działać...
    ///     ''' </summary>
    public static bool IsThisMoje()
    {
        string sTmp = GetHostName().ToLower();
        if (sTmp == "home-pkar")
            return true;
        if (sTmp == "lumia_pkar")
            return true;
        if (sTmp == "kuchnia_pk")
            return true;
        if (sTmp == "ppok_pk")
            return true;
        // If sTmp.Contains("pkar") Then Return True
        // If sTmp.EndsWith("_pk") Then Return True
        return false;
    }

    /// <summary>
    ///     ''' w razie Catch() zwraca false
    ///     ''' </summary>
    public async static Task<bool> NetWiFiOffOnAsync()
    {
        try
        {
            // https://social.msdn.microsoft.com/Forums/ie/en-US/60c4a813-dc66-4af5-bf43-e632c5f85593/uwpbluetoothhow-to-turn-onoff-wifi-bluetooth-programmatically?forum=wpdevelop
            Windows.Devices.Radios.RadioAccessStatus result222 = await Windows.Devices.Radios.Radio.RequestAccessAsync();
            if (result222 != Windows.Devices.Radios.RadioAccessStatus.Allowed)
                return false;

            IReadOnlyList<Windows.Devices.Radios.Radio> radios = await Windows.Devices.Radios.Radio.GetRadiosAsync();

            foreach (var oRadio in radios)
            {
                if (oRadio.Kind == Windows.Devices.Radios.RadioKind.WiFi)
                {
                    Windows.Devices.Radios.RadioAccessStatus oStat = await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.Off);
                    if (oStat != Windows.Devices.Radios.RadioAccessStatus.Allowed)
                        return false;
                    await Task.Delay(3 * 1000);
                    oStat = await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.On);
                    if (oStat != Windows.Devices.Radios.RadioAccessStatus.Allowed)
                        return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void OpenBrowser(string sLink)
    {
        Uri oUri = new Uri(sLink);
        oUri.OpenBrowser();
    }

    /// <summary>
    ///     ''' Zwraca -1 (no radio), 0 (off), 1 (on), ale gdy bMsg to pokazuje dokładniej błąd (nie włączony, albo nie ma radia Bluetooth) - wedle stringów podanych, które mogą być jednak identyfikatorami w Resources
    ///     ''' </summary>
    public async static Task<int> NetIsBTavailableAsync(bool bMsg, bool bRes = false, string sBtDisabled = "ERROR: Bluetooth is not enabled", string sNoRadio = "ERROR: Bluetooth radio not found")
    {


        // Dim result222 As Windows.Devices.Radios.RadioAccessStatus = Await Windows.Devices.Radios.Radio.RequestAccessAsync()
        // If result222 <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return -1

        IReadOnlyList<Windows.Devices.Radios.Radio> oRadios = await Windows.Devices.Radios.Radio.GetRadiosAsync();

        bool bHasBT = false;

        foreach (Windows.Devices.Radios.Radio oRadio in oRadios)
        {
            if (oRadio.Kind == Windows.Devices.Radios.RadioKind.Bluetooth)
            {
                if (oRadio.State == Windows.Devices.Radios.RadioState.On)
                    return 1;
                bHasBT = true;
            }
        }

        if (bHasBT)
        {
            if (bMsg)
            {
                if (bRes)
                    await VBlib.pkarlibmodule14.DialogBoxResAsync(sBtDisabled);
                else
                    await VBlib.pkarlibmodule14.DialogBoxAsync(sBtDisabled);
            }
            return 0;
        }
        else
        {
            if (bMsg)
            {
                if (bRes)
                    await VBlib.pkarlibmodule14.DialogBoxResAsync(sNoRadio);
                else
                    await VBlib.pkarlibmodule14.DialogBoxAsync(sNoRadio);
            }
            return -1;
        }
    }




    // -- DialogBoxy - tylko jako wskok z VBLib ---------------------------------------------


    public async static Task FromLibDialogBoxAsync(string sMsg)
    {
        Windows.UI.Popups.MessageDialog oMsg = new Windows.UI.Popups.MessageDialog(sMsg);
        await oMsg.ShowAsync();
    }

    /// <summary>
    ///     ''' Dla Cancel zwraca ""
    ///     ''' </summary>
    public async static Task<bool> FromLibDialogBoxYNAsync(string sMsg, string sYes = "Tak", string sNo = "Nie")
    {
        Windows.UI.Popups.MessageDialog oMsg = new Windows.UI.Popups.MessageDialog(sMsg);
        Windows.UI.Popups.UICommand oYes = new Windows.UI.Popups.UICommand(sYes);
        Windows.UI.Popups.UICommand oNo = new Windows.UI.Popups.UICommand(sNo);
        oMsg.Commands.Add(oYes);
        oMsg.Commands.Add(oNo);
        oMsg.DefaultCommandIndex = 1;    // default: No
        oMsg.CancelCommandIndex = 1;
        Windows.UI.Popups.IUICommand oCmd = await oMsg.ShowAsync();
        if (oCmd == null)
            return false;
        if (oCmd.Label == sYes)
            return true;

        return false;
    }

    public async static Task<string> FromLibDialogBoxInputAllDirectAsync(string sMsg, string sDefault = "", string sYes = "Continue", string sNo = "Cancel")
    {
        var oInputTextBox = new TextBox()
        {
            AcceptsReturn = false,
            Text = sDefault,
            IsSpellCheckEnabled = false
        };

        ContentDialog oDlg = new ContentDialog()
        {
            Content = oInputTextBox,
            PrimaryButtonText = sYes,
            SecondaryButtonText = sNo,
            Title = sMsg
            // XamlRoot = this
        };

        var oCmd = await oDlg.ShowAsync();
        if (oCmd != ContentDialogResult.Primary)
            return "";

        return oInputTextBox.Text;
    }




    // --- INNE FUNKCJE ------------------------
    public static void SetBadgeNo(int iInt)
    {
        // https://docs.microsoft.com/en-us/windows/uwp/controls-and-patterns/tiles-and-notifications-badges

        Windows.Data.Xml.Dom.XmlDocument oXmlBadge;
        oXmlBadge = Windows.UI.Notifications.BadgeUpdateManager.GetTemplateContent(Windows.UI.Notifications.BadgeTemplateType.BadgeNumber);

        Windows.Data.Xml.Dom.XmlElement oXmlNum;
        oXmlNum = (Windows.Data.Xml.Dom.XmlElement)oXmlBadge.SelectSingleNode("/badge");
        oXmlNum.SetAttribute("value", iInt.ToString());

        Windows.UI.Notifications.BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(new Windows.UI.Notifications.BadgeNotification(oXmlBadge));
    }

    [Obsolete("Czy na pewno ma być GetSettingsString a nie GetLangString?")]
    public static string ToastAction(string sAType, string sAct, string sGuid, string sContent)
    {
        string sTmp = sContent;
        if (sTmp != "")
            sTmp = VBlib.pkarlibmodule14.GetSettingsString(sTmp, sTmp);

        string sTxt = "<action " + "activationType=\"" + sAType + "\" " + "arguments=\"" + sAct + sGuid + "\" " + "content=\"" + sTmp + "\"/> ";
        return sTxt;
    }

    private static void FromLibMakeToast(string sMsg, string sMsg1)
    {
        var sXml = "<visual><binding template='ToastGeneric'><text>" + VBlib.pkarlibmodule14.XmlSafeStringQt(sMsg);
        if (sMsg1 != "")
            sXml = sXml + "</text><text>" + VBlib.pkarlibmodule14.XmlSafeStringQt(sMsg1);
        sXml += "</text></binding></visual>";
        var oXml = new Windows.Data.Xml.Dom.XmlDocument();
        oXml.LoadXml("<toast>" + sXml + "</toast>");
        var oToast = new Windows.UI.Notifications.ToastNotification(oXml);
        Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(oToast);
    }

    /// <summary>
    ///     ''' dwa kolejne teksty, sMsg oraz sMsg1
    ///     ''' </summary>
    public static void MakeToast(string sMsg, string sMsg1 = "")
    {
        FromLibMakeToast(sMsg, sMsg1);
    }
    public static void MakeToast(DateTime oDate, string sMsg, string sMsg1 = "")
    {
        var sXml = "<visual><binding template='ToastGeneric'><text>" + VBlib.pkarlibmodule14.XmlSafeStringQt(sMsg);
        if (sMsg1 != "")
            sXml = sXml + "</text><text>" + VBlib.pkarlibmodule14.XmlSafeStringQt(sMsg1);
        sXml += "</text></binding></visual>";
        var oXml = new Windows.Data.Xml.Dom.XmlDocument();
        oXml.LoadXml("<toast>" + sXml + "</toast>");
        try
        {
            // Dim oToast = New Windows.UI.Notifications.ScheduledToastNotification(oXml, oDate, TimeSpan.FromHours(1), 10)
            var oToast = new Windows.UI.Notifications.ScheduledToastNotification(oXml, oDate);
            Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().AddToSchedule(oToast);
        }
        catch
        {
        }
    }

    public static void RemoveScheduledToasts()
    {
        try
        {
            while (Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications().Count > 0)
                Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().RemoveFromSchedule(Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications()[0]);
        }
        catch
        {
        }
    }

    public static void RemoveCurrentToasts()
    {
        Windows.UI.Notifications.ToastNotificationManager.History.Clear();
    }




    public static int WinVer()
    {
        // Unknown = 0,
        // Threshold1 = 1507,   // 10240
        // Threshold2 = 1511,   // 10586
        // Anniversary = 1607,  // 14393 Redstone 1
        // Creators = 1703,     // 15063 Redstone 2
        // FallCreators = 1709 // 16299 Redstone 3
        // April = 1803		// 17134
        // October = 1809		// 17763
        // ? = 190?		// 18???

        // April  1803, 17134, RS5

        ulong u = ulong.Parse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
        u = (u & 0xFFFF0000L) >> 16;
        return (int)u;
    }

    // <Obsolete("Jest w .Net Standard 2.0 (lib)")>
    public static string GetAppVers()
    {
        return Windows.ApplicationModel.Package.Current.Id.Version.Major + "." + Windows.ApplicationModel.Package.Current.Id.Version.Minor + "." + Windows.ApplicationModel.Package.Current.Id.Version.Build;
    }

    public static string GetBuildTimestamp()
    {
        string install_folder = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
        string sManifestPath = Path.Combine(install_folder, "AppxManifest.xml");

        if (File.Exists(sManifestPath))
            return File.GetLastWriteTime(sManifestPath).ToString("yyyy.MM.dd HH:mm");

        return "";
    }





    /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */

    /// <summary>
    ///     ''' jeśli na wejściu jest jakaś standardowa komenda, to na wyjściu będzie jej rezultat. Else = ""
    ///     ''' </summary>
    public static string AppServiceStdCmd(string sCommand, string sLocalCmds)
    {
        string sTmp = VBlib.pkarlibmodule14.LibAppServiceStdCmd(sCommand, sLocalCmds);
        if (sTmp != "")
            return sTmp;

        // If sCommand.StartsWith("debug loglevel") Then - vbLib

        switch (sCommand.ToLower())
        {
            case "ver":
                {
                    return GetAppVers();
                }

            case "localdir":
                {
                    return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
                }

            case "installeddate":
                {
                    return Windows.ApplicationModel.Package.Current.InstalledDate.ToString("yyyy.MM.dd HH:mm:ss");
                }

            case "debug toasts":
                {
                    return DumpToasts();
                }

            case "debug memsize":
                {
                    return Windows.System.MemoryManager.AppMemoryUsage.ToString() + "/" + Windows.System.MemoryManager.AppMemoryUsageLimit.ToString();
                }

            case "debug rungc":
                {
                    sTmp = "Memory usage before Global Collector call: " + Windows.System.MemoryManager.AppMemoryUsage.ToString() + "\n";
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    sTmp = sTmp + "After: " + Windows.System.MemoryManager.AppMemoryUsage.ToString() + "/" + Windows.System.MemoryManager.AppMemoryUsageLimit.ToString();
                    return sTmp;
                }

            case "lib isfamilymobile":
                {
                    return IsFamilyMobile().ToString();
                }

            case "lib isfamilydesktop":
                {
                    return IsFamilyDesktop().ToString();
                }

            case "lib netisipavailable":
                {
                    return NetIsIPavailable(false).ToString();
                }

            case "lib netiscellinet":
                {
                    return NetIsCellInet().ToString();
                }

            case "lib gethostname":
                {
                    return GetHostName();
                }

            case "lib isthismoje":
                {
                    return IsThisMoje().ToString();
                }
        }

        return "";  // oznacza: to nie jest standardowa komenda
    }


    private static string DumpToasts()
    {
        string sResult = "";
        foreach (Windows.UI.Notifications.ScheduledToastNotification oToast in Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications())

            sResult = sResult + oToast.DeliveryTime.ToString("yyyy-MM-dd HH:mm:ss") + "\n";

        if (sResult == "")
            sResult = "(no toasts scheduled)";
        else
            sResult = "Toasts scheduled for dates: \n" + sResult;

        return sResult;
    }




    /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */


    public async static Task<bool> IsFullVersion()
    {
        /* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */
        if (IsThisMoje())
            return true;

        // Windows.Services.Store.StoreContext: min 14393 (1607)
        var oLicencja = await Windows.Services.Store.StoreContext.GetDefault().GetAppLicenseAsync();
        if (!oLicencja.IsActive)
            return false; // bez licencji? jakżeż to możliwe?

        if (oLicencja.IsTrial)
            return false;

        return true;
    }
}


#if false
public class UwpConfigurationProvider : MsExtConfig.IConfigurationProvider
{
    private readonly string _roamPrefix1 = null;
    private readonly string _roamPrefix2 = null;

    /// <summary>
    ///     ''' Create Configuration Provider, for LocalSettings and RoamSettings
    ///     ''' </summary>
    ///     ''' <param name="sRoamPrefix1">prefix for RoamSettings, use NULL if want only LocalSettings</param>
    ///     ''' <param name="sRoamPrefix2">prefix for RoamSettings, use NULL if want only LocalSettings</param>
    public UwpConfigurationProvider(string sRoamPrefix1 = "[ROAM]", string sRoamPrefix2 = null)
    {
        Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _roamPrefix1 = sRoamPrefix1;
        _roamPrefix2 = sRoamPrefix2;
    }

    private void LoadData(IPropertySet settSource)
    {
        foreach (var oItem in settSource)
            Data[oItem.Key] = oItem.Value;
    }

    /// <summary>
    ///     ''' read current state of settings (all values); although it is not used in TryGet, but we should have Data property set for other reasons (e.g. for listing all variables)...
    ///     ''' </summary>
    public void Load()
    {
        LoadData(WinAppData.Current.RoamingSettings.Values);
        LoadData(WinAppData.Current.LocalSettings.Values);
    }


    /// <summary>
    ///     ''' always set LocalSettings, and if value is prefixed with Roam prefix, also RoamSettings (prefix is stripped)
    ///     ''' </summary>
    ///     ''' <param name="key"></param>
    ///     ''' <param name="value"></param>
    public void Set(string key, string value)
    {
        if (value == null)
            value = "";

        if (_roamPrefix1 != null && value.ToUpperInvariant().StartsWith(_roamPrefix1, StringComparison.Ordinal))
        {
            value = value.Substring(_roamPrefix1.Length);
            try
            {
                WinAppData.Current.RoamingSettings.Values(key) = value;
            }
            catch
            {
            }
        }

        if (_roamPrefix2 != null && value.ToUpperInvariant().StartsWith(_roamPrefix2, StringComparison.Ordinal))
        {
            value = value.Substring(_roamPrefix2.Length);
            try
            {
                WinAppData.Current.RoamingSettings.Values(key) = value;
            }
            catch
            {
            }
        }

        Data[key] = value;
        try
        {
            WinAppData.Current.LocalSettings.Values(key) = value;
        }
        catch
        {
        }
    }

    /// <summary>
    ///     ''' this is used only for iterating keys, not for Get/Set
    ///     ''' </summary>
    ///     ''' <returns></returns>
    protected IDictionary<string, string> Data { get; set; }

    /// <summary>
    ///     ''' gets current Value of Key; local value overrides roaming value
    ///     ''' </summary>
    ///     ''' <returns>True if Key is found (and Value is set)</returns>
    public bool TryGet(string key, ref string value)
    {
        bool bFound = false;

        if (WinAppData.Current.RoamingSettings.Values.ContainsKey(key))
        {
            value = WinAppData.Current.RoamingSettings.Values(key).ToString;
            bFound = true;
        }

        if (WinAppData.Current.LocalSettings.Values.ContainsKey(key))
        {
            value = WinAppData.Current.LocalSettings.Values(key).ToString;
            bFound = true;
        }

        return bFound;
    }

    public MsExtPrim.IChangeToken GetReloadToken()
    {
        return new MsExtConfig.ConfigurationReloadToken();
    }

    public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
    {
        // in this configuration, we don't have structure - so just return list

        List<string> results = new List<string>();
        foreach (KeyValuePair<string, string> kv in Data)
            results.Add(kv.Key);

        results.Sort();

        return results;
    }
}

public class UwpConfigurationSource : MsExtConfig.IConfigurationSource
{
    private readonly string _roamPrefix1 = null;
    private readonly string _roamPrefix2 = null;

    public MsExtConfig.IConfigurationProvider Build(MsExtConfig.IConfigurationBuilder builder)
    {
        return new UwpConfigurationProvider(_roamPrefix1, _roamPrefix2);
    }

    public UwpConfigurationSource(string sRoamPrefix1 = "[ROAM]", string sRoamPrefix2 = null)
    {
        _roamPrefix1 = sRoamPrefix1;
        _roamPrefix2 = sRoamPrefix2;
    }
}

partial static class Extensions
{
    public static MsExtConfig.IConfigurationBuilder AddUwpSettings(this MsExtConfig.IConfigurationBuilder configurationBuilder, string sRoamPrefix1 = "[ROAM]", string sRoamPrefix2 = null)
    {
        configurationBuilder.Add(new UwpConfigurationSource(sRoamPrefix1, sRoamPrefix2));
        return configurationBuilder;
    }
}

#endif

public static partial class Extensions
{

    /// <summary>
    ///     ''' ustaw wszystkie Properties według resources, jeśli są zdefiniowane dla tego elementu
    ///     ''' </summary>
    public static void SetFromResources(this FrameworkElement uiElement)
    {
        VBlib.pkarlibmodule14.SetUiPropertiesFromLang(uiElement);
    }

    /// <summary>
    ///     ''' ustaw wszystkie Properties według resources, jeśli są zdefiniowane dla tego elementu bądź jego dzieci
    ///     ''' </summary>
    public static void SetFromResourcesTree(this FrameworkElement uiElement)
    {
        uiElement.SetFromResources();

        int iMax = VisualTreeHelper.GetChildrenCount(uiElement);
        for (var iLp = 0; iLp <= iMax - 1; iLp++)
        {
            var depObj = VisualTreeHelper.GetChild(uiElement, iLp);
            FrameworkElement frmwrkEl = depObj as FrameworkElement;
            frmwrkEl?.SetFromResourcesTree();
        }
    }

    /// <summary>
    ///     ''' ustaw .Text używając podanego stringu z resources
    ///     ''' </summary>
    public static void SetLangText(this TextBlock uiElement, string stringId)
    {
        uiElement.Text = VBlib.pkarlibmodule14.GetLangString(stringId);
    }

    /// <summary>
    ///     ''' ustaw .Content używając podanego stringu z resources
    ///     ''' </summary>
    public static void SetLangText(this Button uiElement, string stringId)
    {
        uiElement.Content = VBlib.pkarlibmodule14.GetLangString(stringId);
    }
}

// nie mogą być w VBlib, bo Implements Microsoft.UI.Xaml.Data.IValueConverter

/* TODO ERROR: Skipped IfDirectiveTrivia *//* TODO ERROR: Skipped DisabledTextTrivia *//* TODO ERROR: Skipped EndIfDirectiveTrivia */


// parameter = NEG robi negację
public class KonwersjaVisibility : ValueConverterOneWay
{
    public override object Convert(object value, Type targetType, object parameter, System.String language)
    {
        bool bTemp = System.Convert.ToBoolean(value);
        if (parameter != null)
        {
            string sParam = System.Convert.ToString(parameter);
            if (sParam.ToUpperInvariant() == "NEG")
                bTemp = !bTemp;
        }
        if (bTemp)
            return Visibility.Visible;

        return Visibility.Collapsed;
    }
}

// ULONG to String
public class KonwersjaMAC : ValueConverterOneWaySimple
{

    // Define the Convert method to change a DateTime object to
    // a month string.
    protected override object Convert(object value)
    {

        // value is the data from the source object.

        ulong uMAC = System.Convert.ToUInt64(value);
        if (uMAC == 0)
            return "";

        return uMAC.ToHexBytesString();
    }
}

public class KonwersjaVal2StringFormat : ValueConverterOneWay
{

    // Define the Convert method to change a DateTime object to
    // a month string.
    public override object Convert(object value, Type targetType, object parameter, System.String language)
    {
        string sFormat = "";
        if (parameter != null)
            sFormat = System.Convert.ToString(parameter);

        // value is the data from the source object.
        if (value.GetType() == typeof(int))
        {
            var temp = System.Convert.ToInt32(value);
            if (sFormat == "")
                return temp.ToString();
            else
                return temp.ToString(sFormat);
        }

        if (value.GetType() == typeof(long))
        {
            var temp = System.Convert.ToInt64(value);
            if (sFormat == "")
                return temp.ToString();
            else
                return temp.ToString(sFormat);
        }

        if (value.GetType() == typeof(double))
        {
            var temp = System.Convert.ToDouble(value);
            if (sFormat == "")
                return temp.ToString();
            else
                return temp.ToString(sFormat);
        }

        if (value.GetType() == typeof(string))
        {
            var temp = System.Convert.ToString(value);
            if (sFormat == "")
                return temp.ToString();
            else
                return string.Format(sFormat, temp);
        }

        return "???";
    }
}




