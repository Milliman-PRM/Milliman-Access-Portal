using nsoftware.IPWorksSSH;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SftpServerLib
{
    internal partial class IpWorksSftpServer : SftpLibApi
    {
        protected JsonSerializerOptions prettyJsonOptions = new JsonSerializerOptions { WriteIndented = true, };

        private protected void EstablishServerInstance(Certificate cert)
        {
            _sftpServer = new Sftpserver
            {
                About = "Hello world `About`",
                RootDirectory = @"C:\sftproot",
                SSHCert = cert,
            };
            Debug.WriteLine("Server instance constructed");

            //[Description("Information about errors during data delivery.")]
            _sftpServer.OnError += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("Error", evtData));
            };
            //[Description("Fires when a client wants to delete a file.")]
            _sftpServer.OnFileRemove += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("FileRemove", evtData));
            };
            //[Description("Fires when a client wants to read from an open file.")]
            _sftpServer.OnFileRead += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("FileRead", evtData));
            };
            //[Description("Fires when a client wants to open or create a file.")]
            _sftpServer.OnFileOpen += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("fileOpen", evtData));

                //evtData.StatusCode = 3;  // to block the action, set a status
            };
            //[Description("Fires when a client attempts to close an open file or directory handle.")]
            _sftpServer.OnFileClose += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("FileClose", evtData));
            };
            //[Description("Fired when a connection is closed.")]
            _sftpServer.OnDisconnected += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("Disconnected", evtData));

                try
                {
                    string currentConfig = _sftpServer.Config($"UserRootDirectory[{evtData.ConnectionId}]");
                    //string removedConfig = _sftpServer.Config($"UserRootDirectory[51235992]");
                    // TODO: The connection specific config appears to get cleared out after the disconnect.  Confirm this with /n support. 
                }
                catch (Exception e) { string msg = e.Message; };
            };
            //[Description("Fires when a client wants to delete a directory.")]
            _sftpServer.OnDirRemove += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("DirRemove", evtData));
            };
            //[Description("Fires when a client attempts to open a directory for listing.")]
            _sftpServer.OnDirList += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("DirList", evtData));
            };
            //[Description("Fires when a client wants to create a new directory.")]
            _sftpServer.OnDirCreate += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("DirCreate", evtData));
                Debug.WriteLine($"Path above is relative to UserRootDirectory {_sftpServer.Config($"UserRootDirectory[{evtData.ConnectionId}]")}");
            };
            //[Description("Fired when a request for connection comes from a remote host.")]
            _sftpServer.OnConnectionRequest += (sender, evtData) => OnConnectionRequest(sender as Sftpserver, evtData);

            //[Description("Fired immediately after a connection completes (or fails).")]
            _sftpServer.OnConnected += (sender, evtData) => OnConnected(sender as Sftpserver, evtData);

            //[Description("Fires when a client needs to get file information.")]
            _sftpServer.OnGetAttributes += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("GetAttributes", evtData));

                switch (evtData.FileType)
                {
                    case (int)SFTPFileTypes.sftDirectory:
                        break;

                    case (int)SFTPFileTypes.sftRegular:
                        break;
                }
            };

            //[Description("Fires once for each log message.")]
            //_sftpServer.OnLog += (sender, evtData) =>
            //{
            //    Debug.WriteLine(GenerateEventArgsLogMessage("Log", evtData));
            //    Debug.WriteLine($"Event OnLog: Args: {JsonSerializer.Serialize(evtData, prettyJsonOptions)}");
            //};

            //[Description("Fires when a client attempts to canonicalize a path.")]
            _sftpServer.OnResolvePath += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("ResolvePath", evtData));
            };
            //[Description("Fires when a client wants to rename a file.")]
            _sftpServer.OnFileRename += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("FileRename", evtData));
            };
            //[Description("Fires when a client wants to write to an open file.")]
            _sftpServer.OnFileWrite += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("FileWrite", evtData));
            };

            _sftpServer.ShutdownCompleted += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("ShutdownCompleted", evtData));
            };

            _sftpServer.SetFileListCompleted += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("SetFileListCompleted", evtData));
            };

            //[Description("Fires when a client attempts to set file or directory attributes.")]
            _sftpServer.OnSetAttributes += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("SetAttributes", evtData));
            };

            _sftpServer.ExchangeKeysCompleted += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("ExchangeKeysCompleted", evtData));
            };
            _sftpServer.DisconnectCompleted += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("DisconnectCompleted", evtData));
            };

            //[Description("Fires when a client attempts to authenticate a connection.")]
            _sftpServer.OnSSHUserAuthRequest += (sender, evtData) =>
            {
                Debug.WriteLine(GenerateEventArgsLogMessage("SSHUserAuthRequest", evtData));

                //_sftpServer.Config($"UserAuthBanner[{evtData.ConnectionId}]=Whatever");
                if (evtData.AuthMethod.Equals("password", StringComparison.InvariantCultureIgnoreCase))
                {
                    switch (evtData.User)
                    {
                        case string user when user.Equals("user1", StringComparison.InvariantCultureIgnoreCase) && evtData.AuthParam == "password1":
                            evtData.Accept = true;
                            _sftpServer.Config($"UserRootDirectory[{evtData.ConnectionId}]={@"c:\sftproot\child1\"}");

                            //evtData.HomeDir = @"c:\sftproot\child1";
                            //_sftpServer.Config("RestrictUserToHomeDir[" + evtData.ConnectionId + "]=true");
                            break;

                        case string user when user.Equals("user2", StringComparison.InvariantCultureIgnoreCase) && evtData.AuthParam == "password2":
                            evtData.Accept = true;
                            _sftpServer.Config($"UserRootDirectory[{evtData.ConnectionId}]={@"c:\sftproot\child2\"}");
                            break;

                        case string user when user.Equals("user3", StringComparison.InvariantCultureIgnoreCase) && evtData.AuthParam == "password3":
                            evtData.Accept = true;
                            _sftpServer.Config($"UserRootDirectory[{evtData.ConnectionId}]={@"c:\sftproot\child3\"}");
                            return;

                        default:
                            return;
                    }
                }
            };

            //[Description("Shows the progress of the secure connection.")]
            _sftpServer.OnSSHStatus += (sender, evtData) =>
            {
                //Debug.WriteLine(GenerateEventArgsLogMessage("SSHStatus", evtData));
            };

            Debug.WriteLine("Event handlers assigned");
        }

        protected string GenerateEventArgsLogMessage(string eventName, EventArgs args)
        {
            return $"{DateTime.UtcNow.ToString("u")} {eventName} event with EventArgs: {JsonSerializer.Serialize(args, args.GetType(), prettyJsonOptions)}";

            // Some event data has escaped characters (e.g. " => \0022, \r => <CR>, \n => <LF>, etc.). The following unescapes those.
            //return $"{DateTime.UtcNow.ToString("u")} {eventName} event with EventArgs: {Regex.Unescape(JsonSerializer.Serialize(args, args.GetType(), prettyJsonOptions))}";
        }

        //[Description("Fired when a request for connection comes from a remote host.")]
        public void OnConnectionRequest(Sftpserver sender, SftpserverConnectionRequestEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("ConnectionRequest", evtData));
        }

        //[Description("Fired immediately after a connection completes (or fails).")]
        public void OnConnected(object sender, SftpserverConnectedEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("Connected", evtData));
        }

    }
}
