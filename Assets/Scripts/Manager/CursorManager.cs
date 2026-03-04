using JKFrame;
using UnityEngine;

namespace Manager
{
    public class CursorManager : SingletonMono<CursorManager>
    {
        public Texture2D cursorTexture;

        private void Start()
        {
            Vector2 hotspot = new Vector2(0, 0); 
            Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
        }
    }
}