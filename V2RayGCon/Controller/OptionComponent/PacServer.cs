﻿using System.Windows.Forms;

namespace V2RayGCon.Controller.OptionComponent
{
    class PacServer : OptionComponentController
    {
        Service.Setting setting;
        Service.PACServer pacServer;

        TextBox tboxPort;
        CheckBox chkIsAutorun;
        RichTextBox rtboxCustomWhiteList, rtboxCustomBlackList;

        public PacServer(

            TextBox port,
            CheckBox isAutorun,
            RichTextBox customWhiteList,
            RichTextBox customBlackList)
        {
            setting = Service.Setting.Instance;
            pacServer = Service.PACServer.Instance;

            InitControls(port, isAutorun, customWhiteList, customBlackList);
        }

        private void InitControls(TextBox port, CheckBox isAutorun, RichTextBox customWhiteList, RichTextBox customBlackList)
        {

            tboxPort = port;
            chkIsAutorun = isAutorun;
            rtboxCustomBlackList = customBlackList;
            rtboxCustomWhiteList = customWhiteList;

            var pacSetting = setting.GetPacServerSettings();

            port.Text = pacSetting.port.ToString();
            isAutorun.Checked = pacSetting.isAutorun;
            customBlackList.Text = pacSetting.customBlackList;
            customWhiteList.Text = pacSetting.customWhiteList;
        }

        #region public method
        public override bool SaveOptions()
        {
            if (!IsOptionsChanged())
            {
                return false;
            }

            var pacSetting = new Model.Data.PacServerSettings
            {
                port = Lib.Utils.Str2Int(tboxPort.Text),
                isAutorun = chkIsAutorun.Checked,
                customBlackList = rtboxCustomBlackList.Text,
                customWhiteList = rtboxCustomWhiteList.Text,
            };

            setting.SavePacServerSettings(pacSetting);
            if (pacServer.isWebServRunning)
            {
                pacServer.RestartPacServer();
            }
            return true;
        }

        public override bool IsOptionsChanged()
        {
            var s = setting.GetPacServerSettings();

            if (s.port != Lib.Utils.Str2Int(tboxPort.Text)
                || s.isAutorun != chkIsAutorun.Checked
                || s.customBlackList != rtboxCustomBlackList.Text
                || s.customWhiteList != rtboxCustomWhiteList.Text)
            {
                return true;
            }

            return false;
        }
        #endregion

        #region private method
        bool IsIndexValide(int index)
        {
            if (index < 0 || index > 2)
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}