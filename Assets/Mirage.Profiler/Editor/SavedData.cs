using System;
using System.Collections.Generic;
using System.IO;
using Mirage.NetworkProfiler.ModuleGUI.MessageView;
using Mirage.NetworkProfiler.ModuleGUI.UITable;
using UnityEngine;

namespace Mirage.NetworkProfiler.ModuleGUI
{
    [Serializable]
    internal class SavedData
    {
        /// <summary>
        /// Message from each frame so they can survive domain reload
        /// </summary>
        public Frame[] Frames;

        /// <summary>
        /// Active sort header
        /// </summary>
        public string SortHeader;

        public SortMode SortMode;

        /// <summary>
        /// Which Message groups are expanded
        /// </summary>
        public List<string> Expanded;

        public (ColumnInfo, SortMode) GetSortHeader(Columns columns)
        {
            foreach (var c in columns)
            {
                if (SortHeader == c.Header)
                {
                    return (c, SortMode);
                }
            }

            return (null, SortMode.None);
        }
        public void SetSortHeader(SortHeader header)
        {
            if (header == null)
            {
                SortHeader = "";
            }
            else
            {
                SortHeader = header.Info.Header;
                SortMode = header.SortMode;
            }
        }
    }

    internal class SaveDataLoader
    {
        public static void Save(string path, SavedData data)
        {
            CheckDir(path);

            var text = JsonUtility.ToJson(data);
            File.WriteAllText(path, text);
        }

        public static SavedData Load(string path)
        {
            CheckDir(path);

            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                return JsonUtility.FromJson<SavedData>(text);
            }
            else
            {
                return new SavedData();
            }
        }

        private static void CheckDir(string path)
        {
            // check dir exists
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
