using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenAI.Samples.Chat
{
    public class WriteDebugToFile : MonoBehaviour
    {
        string filename = Application.dataPath + "/Logifle.txt";
        // Start is called before the first frame update
        private void OnEnable()
        {
            Application.logMessageReceived += Log;
        }
        private void OnDisable()
        {
            Application.logMessageReceived -= Log;
        }
        public void Log(string logstring, string stacktrace, LogType type)
        {
            TextWriter tw = new StreamWriter(filename, true);
            tw.WriteLine("[ " + DateTime.Now + " ] " + logstring);
            tw.Close();
        }
    }
}
