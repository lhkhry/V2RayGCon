﻿using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using V2RayGCon.Resource.Resx;

namespace V2RayGCon.Service
{
    class Launcher : Model.BaseClass.SingletonService<Launcher>
    {
        Setting setting;
        Servers servers;
        Notifier notifier;
        PacServer pacServer;
        Model.Data.ProxyRegKeyValue orgSysProxySetting;

        bool isCleanupDone = false;

        Launcher()
        {
            orgSysProxySetting = Lib.Sys.ProxySetter.GetProxySetting();
            // warn-up
            var cache = Cache.Instance;
            var cmder = Cmder.Instance;

            setting = Setting.Instance;
            pacServer = PacServer.Instance;
            servers = Servers.Instance;
            notifier = Notifier.Instance;

            SetCulture(setting.culture);

            // dependency injection
            pacServer.Run(setting);
            servers.Run(setting, pacServer, cache);
            cmder.Run(setting, servers, pacServer);
            notifier.Run(setting, servers);

            Application.ApplicationExit +=
                (s, a) => OnApplicationExitHandler(false);

            Microsoft.Win32.SystemEvents.SessionEnding +=
                (s, a) => OnApplicationExitHandler(true);

            Application.ThreadException +=
                (s, a) => SaveExceptionAndExit(
                    a.Exception.ToString());

            AppDomain.CurrentDomain.UnhandledException +=
                (s, a) => SaveExceptionAndExit(
                    (a.ExceptionObject as Exception).ToString());
        }

        readonly object cleanupLocker = new object();
        void OnApplicationExitHandler(bool isShutdown)
        {
            lock (cleanupLocker)
            {
                if (isCleanupDone)
                {
                    return;
                }

                setting.isShutdown = isShutdown;

                notifier.Cleanup();
                servers.Cleanup();
                pacServer.Cleanup();
                setting.Cleanup();
                Lib.Sys.ProxySetter.SetProxy(orgSysProxySetting);

                isCleanupDone = true;
            }
        }

        #region private method
        void SetCulture(Model.Data.Enum.Cultures culture)
        {
            string cultureString = null;

            switch (culture)
            {
                case Model.Data.Enum.Cultures.enUS:
                    cultureString = "";
                    break;
                case Model.Data.Enum.Cultures.zhCN:
                    cultureString = "zh-CN";
                    break;
                case Model.Data.Enum.Cultures.auto:
                    return;
            }

            var ci = new CultureInfo(cultureString);

            Thread.CurrentThread.CurrentCulture.GetType()
                .GetProperty("DefaultThreadCurrentCulture")
                .SetValue(Thread.CurrentThread.CurrentCulture, ci, null);

            Thread.CurrentThread.CurrentCulture.GetType()
                .GetProperty("DefaultThreadCurrentUICulture")
                .SetValue(Thread.CurrentThread.CurrentCulture, ci, null);
        }
        #endregion

        #region debug
#if DEBUG
        void This_Function_Is_Used_For_Debugging()
        {
            notifier.InjectDebugMenuItem(new ToolStripMenuItem(
                "Debug",
                null,
                (s, a) =>
                {
                    servers.DbgFastRestartTest(100);
                }));

            // new Views.WinForms.FormConfiger(@"{}");
            // new Views.WinForms.FormConfigTester();
            // Views.WinForms.FormOption.GetForm();
            Views.WinForms.FormMain.GetForm();
            Views.WinForms.FormLog.GetForm();
            // setting.WakeupAutorunServer();
            // Views.WinForms.FormSimAddVmessClient.GetForm();
            // Views.WinForms.FormDownloadCore.GetForm();
        }
#endif
        #endregion

        #region public method
        public void run()
        {
            Lib.Utils.SupportProtocolTLS12();

            if (servers.IsEmpty())
            {
                Views.WinForms.FormMain.GetForm();
            }
            else
            {
                servers.WakeupServers();
            }

#if DEBUG
            This_Function_Is_Used_For_Debugging();
#endif
        }

        #endregion

        #region unhandled exception
        void ShowExceptionDetails()
        {
            System.Diagnostics.Process.Start(GetBugLogFileName());
            MessageBox.Show(I18N.LooksLikeABug
                + System.Environment.NewLine
                + GetBugLogFileName());
        }

        void SaveExceptionAndExit(string msg)
        {
            var log = msg;
            try
            {
                log += Environment.NewLine
                    + Environment.NewLine
                    + setting.logCache;
            }
            catch { }
            SaveBugLog(log);
            ShowExceptionDetails();
            Application.Exit();
        }

        string GetBugLogFileName()
        {
            var appData = Lib.Utils.GetAppDataFolder();
            return Path.Combine(appData, StrConst.BugFileName);
        }

        void SaveBugLog(string content)
        {
            try
            {
                var bugFileName = GetBugLogFileName();
                Lib.Utils.CreateAppDataFolder();
                File.WriteAllText(bugFileName, content);
            }
            catch { }
        }
        #endregion
    }
}
