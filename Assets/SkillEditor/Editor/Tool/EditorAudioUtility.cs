using System;
using System.Reflection;
using UnityEngine;

namespace SkillEditor
{
    public static class EditorAudioUtility
    {
        private static MethodInfo playClipMethodInfo;
        private static MethodInfo stopAllClipMethodInfo;

        static EditorAudioUtility()
        {
            var editorAssembly = typeof(UnityEditor.AudioImporter).Assembly;
            Type utilClassType = editorAssembly.GetType("UnityEditor.AudioUtil");
            
            playClipMethodInfo = utilClassType.GetMethod("PlayPreviewClip", 
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);

            stopAllClipMethodInfo = utilClassType.GetMethod("StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public);
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="audioClip"></param>
        /// <param name="start">播放进度：0~1</param>
        public static void PlayAudio(AudioClip audioClip, float start)
        {
            playClipMethodInfo.Invoke(null, new object[] { audioClip, (int)(start * 10000), false });
        }

        public static void StopAllAudios()
        {
            stopAllClipMethodInfo.Invoke(null, null);
        }
    }
}