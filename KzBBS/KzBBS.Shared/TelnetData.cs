using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;

namespace KzBBS
{
    public class TelnetText
    {
        public string text;
        public Color fgColor;

        public TelnetText()
        {
            text = "";
            fgColor = Color.FromArgb(255, 192, 192, 192);
        }

        public TelnetText(string t, Color fg)
        {
            text = t;
            fgColor = fg;
        }
    }

    [DataContract]
    class TelnetData:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string text;
        private Color foreColor;
        private Color backColor;
        private bool blinking;
        private bool dualColor;
        private Point position;
        private int count;
        private bool twWord;

        [DataMember]
        public string Text 
        { 
            get { return text; } 
            set { text = value; NotifyPropertyChanged("Text"); }
        }
        [DataMember]
        public Color ForeColor 
        {
            get { return foreColor; }
            set { foreColor = value; NotifyPropertyChanged("ForeColor"); }
        }
        [DataMember]
        public Color BackColor
        {
            get { return backColor; }
            set { backColor = value; NotifyPropertyChanged("BackColor"); }
        }
        [DataMember]
        public bool Blinking 
        {
            get { return blinking; }
            set { blinking = value; NotifyPropertyChanged("Blinking"); }
        }
        [DataMember]
        public bool DualColor 
        {
            get { return dualColor; }
            set { dualColor = value; NotifyPropertyChanged("DualColor"); }
        }
        [DataMember]
        public Point Position
        {
            get { return position; }
            set { position = value; NotifyPropertyChanged("Position"); }
        }
        [DataMember]
        public int Count 
        {
            get { return count; }
            set { count = value; NotifyPropertyChanged("Count"); }
        }

        [DataMember]
        public bool TwWord 
        { 
            get { return twWord; }
            set { twWord = value; NotifyPropertyChanged("IsTwWord"); }
        }
        
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }

        public TelnetData()
        {
            Text = (char)0xA0 + "";
            ForeColor = Color.FromArgb(255,192,192,192); //Gray
            BackColor = Color.FromArgb(255, 0, 0, 0); //Black
            Blinking = false;
            DualColor = false;
            Position = new Point(0, 0);
            Count = 0;
            twWord = false;
        }

        public void setData(Color fg, Color bg, bool blink, bool twWord, Point pos)
        {
            ForeColor = fg;
            BackColor = bg;
            Blinking = blink;
            Position = pos;
            TwWord = twWord;
        }

        public void resetData()
        {
            Text = (char)0xA0 + "";
            ForeColor = Color.FromArgb(255, 192, 192, 192); //Gray
            BackColor = Color.FromArgb(255, 0, 0, 0); //Black
            Blinking = false;
            DualColor = false;
            Position = new Point(0, 0);
            Count = 0;
            TwWord = false;
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
            TwWord = source.TwWord;
        }

        public static bool operator == (TelnetData a, TelnetData b)
        {
            if (System.Object.ReferenceEquals(a,b))
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            return a.backColor == b.backColor && a.foreColor == b.foreColor && a.twWord == b.twWord
                && a.blinking == b.blinking && a.dualColor == b.dualColor;
        }

        public static bool operator != (TelnetData a, TelnetData b)
        {
            return !(a.backColor == b.backColor && a.foreColor == b.foreColor && a.twWord == b.twWord
                && a.blinking == b.blinking && a.dualColor == b.dualColor);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
