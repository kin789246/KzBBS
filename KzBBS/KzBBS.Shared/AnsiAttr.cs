using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;

namespace KzBBS
{
    class AnsiAttr
    {
        public Color fgColor;
        public Color bgColor;
        public bool isBlinking;
        //public bool brightColor;
        //public bool normalColor;
        public AnsiAttr()
        {
            fgColor = Color.FromArgb(255, 170, 170, 170);
            bgColor = Color.FromArgb(255, 0, 0, 0);
            isBlinking = false;
            //brightColor = false;
            //normalColor = true;
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
