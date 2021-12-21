#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FACS01.Utilities
{
    [InitializeOnLoad]
    public class CheckForUpdates
    {
        public static SynchronizationContext syncContext = SynchronizationContext.Current;
        static CheckForUpdates()
        {
            if (SessionState.GetBool("FACSUtilitiesCheckForUpdatesOnce", true))
            {
                SessionState.SetBool("FACSUtilitiesCheckForUpdatesOnce", false);
                StartCFUThread();
            }
        }
        [MenuItem("FACS Utils/Misc/Check for Updates", false, 999)]
        public static void StartCFUThread()
        {   
            Thread thread = new Thread(() => CheckForInternetConnection());
            thread.Start();
        }
        public static void CheckForInternetConnection()
        {
            bool goodtogo = false;
            try { using (var client = new WebClient())
                {
                    using (client.OpenRead("https://raw.githubusercontent.com/FACS01-01/FACS_Utilities/main/version.txt"))
                    {goodtogo = true;}
            }}
            catch
            { Debug.LogWarning($"[<color=cyan>FACS Utilities</color>] Can't Check for Updates. no Internet connection? or GitHub is down?"); }
            if (goodtogo) syncContext.Post(_ => { RunCFU(); }, null);
        }
        public static void RunCFU()
        {
            string myVersion = File.ReadLines(Application.dataPath + "/FACS01 Utilities/version.txt").First();
            string[] myVersionSplit = myVersion.Split('.');
            string latestVersion = "";
            using (WebClient wc = new WebClient())
            { latestVersion = wc.DownloadString("https://raw.githubusercontent.com/FACS01-01/FACS_Utilities/main/version.txt"); }
            string[] latestVersionSplit = latestVersion.Split('.');

            if (myVersionSplit[0]==latestVersionSplit[0] && myVersionSplit[1]==latestVersionSplit[1] && myVersionSplit[2]==latestVersionSplit[2])
            { Debug.Log($"[<color=cyan>FACS Utilities</color>] Tools are up to date!"); return; }

            decimal ver = int.Parse(myVersionSplit[0])*365.25m + int.Parse(myVersionSplit[1])*30.4375m + int.Parse(myVersionSplit[2]);
            decimal latestver = int.Parse(latestVersionSplit[0])*365.25m + int.Parse(latestVersionSplit[1])*30.4375m + int.Parse(latestVersionSplit[2]);

            if (ver > latestver)
            { Debug.LogWarning($"[<color=cyan>FACS Utilities</color>] Are you a time traveller? Tools version in higher than source!"); return; }

            bool update;
            string lastFullReinstall = "";
            using (WebClient wc = new WebClient())
            { lastFullReinstall = wc.DownloadString("https://raw.githubusercontent.com/FACS01-01/FACS_Utilities/main/last%20full%20reinstall.txt"); }
            string[] lastFullReinstallSplit = lastFullReinstall.Split('.');
            decimal lastfullreins = int.Parse(lastFullReinstallSplit[0]) * 365.25m + int.Parse(lastFullReinstallSplit[1]) * 30.4375m + int.Parse(lastFullReinstallSplit[2]);

            if (ver < lastfullreins)
            {
                Debug.Log($"[<color=cyan>FACS Utilities</color>] Your tools are out of date, and are incompatible with newer versions.");
                update = EditorUtility.DisplayDialog("FACS Utilities : Check for Updates", "Your tools are out of date," +
                    " and are incompatible with newer versions!\n\nGo to FACS Utilities GitHub page and get the latest Release.\nAnd remember to delete 'FACS Utilities' folder" +
                    " before importing the newer one.", "Open GitHub", "Ignore");
                if (update) Process.Start("https://github.com/FACS01-01/FACS_Utilities");
                return;
            }

            Debug.Log($"[<color=cyan>FACS Utilities</color>] Your tools are out of date.");
            update = EditorUtility.DisplayDialog("FACS Utilities : Check for Updates", "Your tools are out of date!\n\n" +
                "Go to FACS Utilities GitHub page and get the latest Release.", "Open GitHub", "Ignore");
            if (update) Process.Start("https://github.com/FACS01-01/FACS_Utilities");
        }
    }
}
#endif