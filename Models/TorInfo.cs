using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading;
using TSApi.Engine;

namespace TSApi.Models
{
    public class TorInfo
    {
        public int port { get; set; }

        [JsonIgnore]
        public bool work { get; set; }

        public HashSet<string> clientIps { get; set; } = new HashSet<string>();

        public UserData user { get; set; }

        [JsonIgnore]
        public Thread thread { get; set; }

        public DateTime lastActive { get; set; }

        [JsonIgnore]
        public int countError { get; set; }


        #region process
        [JsonIgnore]
        public Process process { get; set; }

        public event EventHandler processForExit;

        public void OnProcessForExit()
        {
            processForExit?.Invoke(this, null);
        }
        #endregion

        #region Dispose
        bool IsDispose;

        public void Dispose()
        {
            if (IsDispose)
                return;

            try
            {
                IsDispose = true;
                work = false;

                #region process
                try
                {
                    process.Kill(true);
                    process.Dispose();
                }
                catch { }
                #endregion

                #region Bash
                try
                {
                    foreach (string line in Bash.Run($"ps axu | grep \"/TorrServer-linux-amd64 -p {port} -d\" " + "| grep -v grep | awk '{print $2}'").Split("\n"))
                    {
                        if (int.TryParse(line, out int pid))
                            Bash.Run($"kill -9 {pid}");
                    }
                }
                catch { }
                #endregion

                if (user.IsShared)
                {
                    foreach (var ip in clientIps)
                        Bash.Run($"rm -rf {Startup.settings.appfolder}/sandbox/{user.login}/{ip.Replace(".", "").Replace(":", "")}");
                }

                clientIps.Clear();
                thread = null;
            }
            catch { }
        }
        #endregion
    }
}
