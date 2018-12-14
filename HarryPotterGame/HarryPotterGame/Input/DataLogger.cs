using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework.Input;
using HarryPotterGame;

namespace Microsoft.Xna.Framework
{
    /// <summary>
    /// What kind of logging environment is currently active in the game. An active logging
    /// environment is one where the player has full control over the game and each of his
    /// actions affect the data collected. A menul logging environment represenets interface
    /// navigation, where data is less important. An inactive logging environment is one
    /// where the player has no control over the game's actions, and data he logs has no
    /// affect on the game world. These should be changed as the game state changes, to
    /// reflect the importance of the data collected.
    /// </summary>
    public enum LoggingEnvironment { NOGESTURE, YESSELECT, NOSELECT, GRAB, YESDRAG, YESDROP, NODROP }

    /// <summary>
    /// Will create a file of all data collected during a single session of play. 
    /// Must be initialized, and updated regularly. The time "interval"- how many
    /// times per second the data is updated can be modified from the default 1/10th
    /// of a second default. Logger needs no attachment to other framing devices-
    /// It works entirely autonomously from the rest of the game.
    /// </summary>
    class DataLogger
    {
        public struct InputEntry
        {
            public float timeStamp;
            public Vector2 cursorpos;
            public string flag;
            public string[] customvalues;

            public InputEntry(float timeStamp, Vector2 cursorpos, string flag, string[] customvalues)
            {
                this.timeStamp = timeStamp;
                this.cursorpos = cursorpos;
                this.flag = flag;
                this.customvalues = customvalues;
            }

            /// <summary>
            /// A string output for debugging.
            /// </summary>
            /// <returns>A neat string representation of the data</returns>
            public override string  ToString()
            {
                String datastr = timeStamp + ": (" + cursorpos.X + "," + cursorpos.Y + "),E=" + flag;
                for (int i = 0; i < customvalues.Length; i++) datastr += ",CV" + (i + 1) + "=" + customvalues[i];
                return datastr;
            }

            /// <summary>
            /// A string output for export into excel
            /// </summary>
            /// <returns>An excel-importable row</returns>
            public String toExcel()
            {
                String excelstring = timeStamp + "," + cursorpos.X + "," + cursorpos.Y + "," + flag;
                foreach (String s in customvalues) excelstring += "," + s;
                return excelstring;
            }
        }

        public static String UserName;

        private static StreamWriter writer;
        private static bool isOpen = true;
        private static List<InputEntry> entryList = new List<InputEntry>();
        private static float timer = 0.0f;
        private static float rumbleDuration = 0.0f;
        private static bool initialized = false;
        public static bool Initialized { get { return initialized; } }

        private static Game game;
        private static float gameInitializedTime = 0f;

        private static float timeInterval = 1f / 6f;
        /// <summary>
        /// The length in seconds between each data update.
        /// </summary>
        public static float TimeInterval
        {
            set { timeInterval = value; }
        }
        private static LoggingEnvironment environment = LoggingEnvironment.NOGESTURE;
        /// <summary>
        /// The active logging environment.
        /// </summary>
        public static LoggingEnvironment Environment
        {
            set { environment = value; }
        }

        /// <summary>
        /// Initializes the data logger. Prepares the output stream, enters the list tites of each game
        /// and prepares any other variables needed for data collection
        /// </summary>
        /// <param name="username">The username of the person playing</param>
        /// <param name="gamename">The name of the game being played</param>
        /// <param name="gameTime">The time elapsed- set to null for 0 (if starting in-game etc)</param>
        public static void Initialize(String username, String path, Game game_, GameTime gameTime, params string[] additionalTitles)
        {
            DateTime time = DateTime.Now;
            DataLogger.UserName = username;
            game = game_;

            writer = new StreamWriter(path + "/log.txt", false);
            string[] extraparams = new string[additionalTitles.Length];
            entryList.Clear();
            entryList.Add(new InputEntry(0, Vector2.Zero, environment.ToString(), extraparams));

            string appendedtitles = "";
            foreach (string title in additionalTitles) appendedtitles += ", " + title;

            writer.WriteLine("timestamp, xpos, ypos, flag" + appendedtitles);
            writer.WriteLine(entryList[0].toExcel());

            if (gameTime == null)
                gameInitializedTime = 0;
            else 
                gameInitializedTime = (float)gameTime.TotalGameTime.TotalSeconds;

            isOpen = true;
            initialized = true;
        }

        /// <summary>
        /// Increases the timer. If sufficient time has elapsed for the next
        /// interval, another entry will be added to the data log.
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public static void Update(GameTime gameTime, params string[] extravalues)
        {
            timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            if (timer > timeInterval && isOpen && !(environment == LoggingEnvironment.NOGESTURE && entryList[0].flag == "NOGESTURE") 
                && !(environment == LoggingEnvironment.GRAB && entryList[0].flag == "GRAB"))
            {
                Vector2 pos = ((Game1)(game)).CursorPos;

                //We first check the standard parameters to see if there's been a change to flag, rumble or position.
                bool standardparamschanged = false;
                if (entryList[0].cursorpos != pos) standardparamschanged = true;
                else if (entryList[0].flag != environment.ToString()) standardparamschanged = true;

                //Here we have to make sure that every extra parameter is identical, recoding if a change has occurred.
                bool extraparamschanged = false;
                string[] lastextravalues = entryList[0].customvalues;

                for (int i = 0; i < extravalues.Length; i++)
                {
                    if (extravalues[i] != lastextravalues[i])
                    {
                        extraparamschanged = true;
                        break;
                    }
                }

                //Have there been any changes? If so, we need a new tuple.
                if (extraparamschanged || standardparamschanged)
                {
                    entryList.Insert(0, new InputEntry((float)gameTime.TotalGameTime.TotalSeconds - gameInitializedTime, pos, environment.ToString(), extravalues));
                    writer.WriteLine(entryList[0].toExcel());
                }
            }
        }

        /// <summary>
        /// Returns each data entry as a string value
        /// </summary>
        /// <returns>The string representation of each input entry</returns>
        public static string CreateString()
        {
            String s = "";
            foreach (InputEntry entry in entryList) s += entry.ToString() + "\n";

            return s;
        }

        /// <summary>
        /// Closes the stream writer. Do this before the game terminates.
        /// </summary>
        public static void Close()
        {
            isOpen = false;
            writer.Close();
        }
    }
}
