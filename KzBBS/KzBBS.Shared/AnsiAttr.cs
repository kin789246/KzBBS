using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;

namespace KzBBS
{
    class AnsiAttr
    {
        private Color fgColor;
        private Color bgColor;
        private bool isBlinking;
        //public bool brightColor;
        //public bool normalColor;
        public Color FgColor { get { return fgColor; } set { fgColor = value; } }
        public Color BgColor { get { return bgColor; } set { bgColor = value; } }
        public bool IsBlinking { get { return isBlinking; } set { isBlinking = value; } }

        public AnsiAttr()
        {
            fgColor = Color.FromArgb(255, 170, 170, 170);
            bgColor = Color.FromArgb(255, 0, 0, 0);
            isBlinking = false;
            //brightColor = false;
            //normalColor = true;
        }

        public AnsiAttr(Color fg, Color bg, bool blinking)
        {
            fgColor = fg;
            bgColor = bg;
            isBlinking = blinking;
        }

        public void setAtt(Color fg, Color bg, bool blink)
        {
            fgColor = fg;
            bgColor = bg;
            isBlinking = blink;
            //brightColor = bright;
            //normalColor = normal;
        }
    }
}
