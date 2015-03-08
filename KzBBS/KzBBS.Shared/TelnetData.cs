using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;

namespace KzBBS
{
    [DataContract]
    class TelnetData
    {
        [DataMember]
        public string Text;
        [DataMember]
        public Color ForeColor;
        [DataMember]
        public Color BackColor;
        [DataMember]
        public bool Blinking;
        [DataMember]
        public bool DualColor;
        [DataMember]
        public Point Position;
        [DataMember]
        public int Count;

        public List<UIElement> BBSUI;

        public TelnetData()
        {
            Text ="";
            ForeColor = Color.FromArgb(255,192,192,192); //Gray
            BackColor = Color.FromArgb(255, 0, 0, 0); //Black
            Blinking = false;
            DualColor = false;
            Position = new Point(0, 0);
            Count = 0;
            BBSUI = new List<UIElement>();
        }

        public void setData(Color fg, Color bg, bool blink, Point pos)
        {
            ForeColor = fg;
            BackColor = bg;
            Blinking = blink;
            Position = pos;
        }

        public void resetData()
        {
            Text = "";
            ForeColor = Color.FromArgb(255, 192, 192, 192); //Gray
            BackColor = Color.FromArgb(255, 0, 0, 0); //Black
            Blinking = false;
            DualColor = false;
            Position = new Point(0, 0);
            Count = 0;
            BBSUI.Clear();
        }

        public void cloneFrom(TelnetData source)
        {
            Text = source.Text;
            ForeColor = source.ForeColor;
            BackColor = source.BackColor;
            Blinking = source.Blinking;
            DualColor = source.DualColor;
            Position = source.Position;
            Count = source.Count;
        }
    }
}
