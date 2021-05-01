using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using DiscordInterface;
using Util;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "discord rich presence";
            about.Description = "plugin";
            about.Author = "Gianni";
            about.TargetApplication = "";
            about.Type = PluginType.General;
            about.VersionMajor = 1;
            about.VersionMinor = 0;
            about.Revision = 2;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            about.ConfigurationPanelHeight = 0;

            InitialiseDiscord();

            return about;
        }

        private void InitialiseDiscord()
        {
            DiscordRPC.DiscordEventHandlers handlers = new DiscordRPC.DiscordEventHandlers();
            handlers.readyCallback = HandleReadyCallback;
            handlers.errorCallback = HandleErrorCallback;
            handlers.disconnectedCallback = HandleDisconnectedCallback;
            // first parameter is the application id from discord dev apps
            DiscordRPC.Initialize("", ref handlers, true, null);
        }

        private void HandleReadyCallback() { }
        private void HandleErrorCallback(int errorCode, string message) { }
        private void HandleDisconnectedCallback(int errorCode, string message) { }

        DiscordRPC.RichPresence presence = new DiscordRPC.RichPresence();

        private void UpdatePlayedPresence(string name, string artist, string duration, int position)
        {
            presence.state = artist;
            name = Utility.Utf16ToUtf8(name);
            presence.details = name.Substring(0, name.Length - 1);
            presence.largeImageKey = "";
            presence.largeImageText = "";
            long now = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            presence.startTimestamp = now - position;
            DiscordRPC.UpdatePresence(ref presence);
        }

        private void UpdatePausedPresence()
        {
            presence.state = "";
            presence.details = "";
            presence.largeImageKey = "";
            presence.largeImageText = "";
            DiscordRPC.UpdatePresence(ref presence);
        }

        public bool Configure(IntPtr panelHandle)
        {
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "prompt:";
                TextBox textBox = new TextBox();
                textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            }
            return false;
        }

        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        }

        public void Close(PluginCloseReason reason)
        {
            // call this to shut down the rpc connection so it doesn't persist
            DiscordRPC.Shutdown();
        }

        public void Uninstall()
        {

        }

        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // musicbee api stuff
            string artist = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
            string trackTitle = mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle);
            string duration = mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.Duration);
            bool getSongLength = mbApiInterface.Player_GetShowTimeRemaining();
            string songLength = getSongLength.ToString();
            int position = mbApiInterface.Player_GetPosition();

            // create new variables so we can modify them for certain situations
            string songName = trackTitle;
            string songArtist = artist;

            // check if there is no artist so we can replace it with Unknown
            if (string.IsNullOrEmpty(artist))
            {
                songName = trackTitle;
                songArtist = "Unknown";
            }

            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                // perform startup initialization
                case NotificationType.PlayStateChanged:
                    switch (mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                            UpdatePlayedPresence(songName, songArtist, duration, position / 1000);
                            break;
                        case PlayState.Paused:
                            UpdatePausedPresence();
                            break;
                    }
                    break;
                case NotificationType.TrackChanged:
                    UpdatePlayedPresence(songName, songArtist, duration, 0);
                    break;
            }
        }
    }
}