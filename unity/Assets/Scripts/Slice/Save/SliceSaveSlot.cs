using System;
using System.IO;
using GameDev.Core.Save;
using UnityEngine;

namespace GameDev.Slice.Save
{
    /// <summary>
    /// The slice's one Suspend Save on disk (owner, 2026-11-04: one Suspend Save; 17 X.3): a JSON file in the player's
    /// data folder, written beside itself first and then put in place, so a save cut short never leaves half a file.
    /// </summary>
    public static class SliceSaveSlot
    {
        public const string FileName = "suspend.json";

        private static string _path;

        /// <summary>The slot's file; a test sets its own so it never touches the player's.</summary>
        public static string Path
        {
            get => _path ??= System.IO.Path.Combine(Application.persistentDataPath, FileName);
            set
            {
                _path = value;
                IsCustomPath = value != null;
            }
        }

        /// <summary>Whether a test set the slot's file, so a batch run may offer and write it.</summary>
        public static bool IsCustomPath { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _path = null;
            IsCustomPath = false;
        }

        public static bool Exists => File.Exists(Path);

        public static void Write(SaveFile file)
        {
            var path = Path;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? ".");
            var next = path + ".new";
            File.WriteAllText(next, JsonUtility.ToJson(file));
            if (File.Exists(path))
            {
                File.Replace(next, path, null);
            }
            else
            {
                File.Move(next, path);
            }
        }

        /// <summary>The saved file, or null when there is none or it cannot be read.</summary>
        public static SaveFile Read()
        {
            try
            {
                return Exists ? JsonUtility.FromJson<SaveFile>(File.ReadAllText(Path)) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] the Suspend Save could not be read: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            if (Exists)
            {
                File.Delete(Path);
            }
        }
    }
}
