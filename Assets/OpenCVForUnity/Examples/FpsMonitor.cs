using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace OpenCVForUnityExample
{
    public class FpsMonitor : MonoBehaviour
    {
        int tick = 0;
        float elapsed = 0;
        float fps = 0;

        public enum Alignment
        {
            LeftTop,
            RightTop,
            LeftBottom,
            RightBottom,
        }

        public Alignment alignment = Alignment.RightTop;

        const float GUI_WIDTH = 75f;
        const float GUI_HEIGHT = 30f;
        const float MARGIN_X = 10f;
        const float MARGIN_Y = 10f;
        const float INNER_X = 8f;
        const float INNER_Y = 5f;
        const float GUI_CONSOLE_HEIGHT = 50f;

        public Vector2 offset = new Vector2(MARGIN_X, MARGIN_Y);
        public bool boxVisible = true;
        public float boxWidth = GUI_WIDTH;
        public float boxHeight = GUI_HEIGHT;
        public Vector2 padding = new Vector2(INNER_X, INNER_Y);
        public float consoleHeight = GUI_CONSOLE_HEIGHT;

        GUIStyle console_labelStyle;

        float x, y;
        Rect outer;
        Rect inner;

        float console_x, console_y;
        Rect console_outer;
        Rect console_inner;

        int oldScrWidth;
        int oldScrHeight;

        Dictionary<string, string> outputDict = new Dictionary<string, string> ();
        public string consoleText;

        // Use this for initialization
        void Start () {
            console_labelStyle = new GUIStyle ();
            console_labelStyle.fontSize = 32;
            console_labelStyle.fontStyle = FontStyle.Normal;
            console_labelStyle.wordWrap = true;
            console_labelStyle.normal.textColor = Color.white;

            oldScrWidth = Screen.width;
            oldScrHeight = Screen.height;
            LocateGUI();
        }

        // Update is called once per frame
        void Update () {
            tick++;
            elapsed += Time.deltaTime;
            if (elapsed >= 1f) {
                fps = tick / elapsed;
                tick = 0;
                elapsed = 0;
            }
        }

        void OnGUI () {
            if (oldScrWidth != Screen.width || oldScrHeight != Screen.height) {
                LocateGUI();
            }
            oldScrWidth = Screen.width;
            oldScrHeight = Screen.height;

            if (boxVisible) {
                GUI.Box(outer, "");
            }

            GUILayout.BeginArea(inner);
            {
                GUILayout.BeginVertical();
                GUILayout.Label("fps : " + fps.ToString("F1"));
                foreach (KeyValuePair<string, string> pair in outputDict) {
                    GUILayout.Label(pair.Key + " : " + pair.Value);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea ();

            if (!string.IsNullOrEmpty(consoleText)) {
                if (boxVisible) {
                    GUI.Box (console_outer, "");
                }

                GUILayout.BeginArea (console_inner);
                {
                    GUILayout.BeginVertical ();
                    GUILayout.Label (consoleText, console_labelStyle);
                    GUILayout.EndVertical ();
                }
                GUILayout.EndArea ();
            }
        }

        public void Add (string key, string value) {
            if (outputDict.ContainsKey (key)) {
                outputDict [key] = value;
            } else {
                outputDict.Add (key, value);
            }
        }

        public void Remove (string key) {
            outputDict.Remove (key);
        }

        public void Clear () {
            outputDict.Clear ();
        }
            
        public void LocateGUI() {
            x = GetAlignedX(alignment, boxWidth);
            y = GetAlignedY(alignment, boxHeight);
            outer = new Rect(x, y, boxWidth, boxHeight);
            inner = new Rect(x + padding.x, y + padding.y, boxWidth, boxHeight);

            console_x = GetAlignedX(Alignment.LeftBottom, Screen.width);
            console_y = GetAlignedY(Alignment.LeftBottom, consoleHeight);
            console_outer = new Rect(console_x, console_y, Screen.width - offset.x*2, consoleHeight);
            console_inner = new Rect(console_x + padding.x, console_y + padding.y, Screen.width - offset.x*2 - padding.x, consoleHeight);
        }
            
        float GetAlignedX(Alignment anchor, float w) {
            switch (anchor) {
            default:
            case Alignment.LeftTop:
            case Alignment.LeftBottom:
                return offset.x;

            case Alignment.RightTop:
            case Alignment.RightBottom:
                return Screen.width - w - offset.x;
            }
        }

        float GetAlignedY(Alignment anchor, float h) {
            switch (anchor) {
            default:
            case Alignment.LeftTop:
            case Alignment.RightTop:
                return offset.y;

            case Alignment.LeftBottom:
            case Alignment.RightBottom:
                return Screen.height - h - offset.y;
            }
        }
    }
}