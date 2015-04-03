using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Data;
using System.Collections.ObjectModel;

namespace KzBBS
{
    enum BBSMode : byte
    {
        PressAnyKey, //任意鍵        
        BoardList, //看板列表
        ArticleBrowse, //文章瀏覽
        AnimationPlay, //動畫播放
        Editor, //編輯文章
        Login, //登入畫面
        Other,
    };

    class PTTBlock
    {
        public string Text { get; set; }
        public int LeftPoint { get; set; }
        public int TopPoint { get; set; }
        public SolidColorBrush ForeColor { get; set; }
        public SolidColorBrush BackColor { get; set; }
        public double Width { get; set; }
        public string FontName { get; set; }
        public int ZIndex { get; set; }
        public bool Blinking { get; set; }
        public bool DualColor { get; set; }
    }

    class PTTLine
    {
        public int No { get; set; }
        public string Text { get; set; }
        public string UniqueId { get; set; }
        public ObservableCollection<PTTBlock> Blocks { get; set; }
    }

    class PTTDisplay
    {
//#if WINDOWS_PHONE_APP
        public static string cht_fontFamily = "Arial";
        public static string ansi_fontFamily = "kaiu.ttf#DFKai-SB";
//#endif

//#if WINDOWS_APP
//        public static string cht_fontFamily = "Arial";
//        public static string ansi_fontFamily = "DFKai-SB";
//#endif
        /*---------- define properties ----------*/
        public static double _fontSize = 30; //default font size =30
        public static double chtOffset = 1;
        public static BBSMode LastMode = BBSMode.Other;
        public static BBSMode currentMode;
        public static bool LoginScreen = false;
        /*---------------------------------------*/

        public static PTTDisplay pttDisplay = new PTTDisplay();
        private static ObservableCollection<PTTLine> lines = new ObservableCollection<PTTLine>();
        public static ObservableCollection<PTTLine> Lines
        {
            get { return lines; }
        }

        internal static void resetAllSetting()
        {
            LastMode = BBSMode.Other;
            currentMode = BBSMode.Other;
            LoginScreen = false;
            lines.Clear();
        }

        public void LoadFromSource(TelnetData[,] BBSPage)
        {
            lines.Clear();
            TelnetData point;
            lastID = new PTTLine();
            for (int row = 0; row < 24; row++)
            {
                PTTLine pttline = new PTTLine();
                pttline.Blocks = new ObservableCollection<PTTBlock>();
                pttline.No = row;
                point = new TelnetData();
                point.cloneFrom(BBSPage[row, 0]);
                point.Position = new Point(row, 0);
                for (int col = 1; col < 80; col++)
                {
                    if (point == BBSPage[row, col]) //only check backColor, forColor, blinking, dualcolor, isTwWord
                    {
                        point.Text += BBSPage[row, col].Text;
                    }
                    else
                    {
                        point.Count = col - (int)point.Position.Y;
                        if (point.Text == "")
                        { point.Text = (char)0xA0 + ""; }
                        pttline.Blocks.Add(getBlock(point));
                        point = new TelnetData();
                        point.cloneFrom(BBSPage[row, col]);
                        point.Position = new Point(row, col);
                    }
                }
                if (point.Text == "") //record the final portion
                {
                    point.Text = (char)0xA0 + "";
                }
                point.Count = 80 - (int)point.Position.Y;
                pttline.Blocks.Add(getBlock(point));
                getLineProp(pttline);
                lines.Add(pttline);
            }
            onLinesChanged(new EventArgs());
            Debug.WriteLine("mode: {0}", currentMode.ToString());
        }

        PTTLine lastID = new PTTLine();
        private void getLineProp(PTTLine pttline)
        {
            int parseNo = 0;
            foreach (PTTBlock item in pttline.Blocks)
            {
                pttline.Text += item.Text;
            }

            //check login screen
            if (pttline.No == 22 || pttline.No == 20 || pttline.No == 23)
            {
                if (pttline.Text.Contains("guest") || pttline.Text.Contains("勇者代號"))
                { LoginScreen = true; }
            }
            if (pttline.No == 23)
            {
                if (!string.IsNullOrWhiteSpace(pttline.Text))
                {
                    checkMode(pttline.Text);
                }
                else
                {
                    PTTLine line = Lines.First(x => x.No == 0);
                    checkMode(line.Text);
                }
            }
            string getId = pttline.Text;
            if (pttline.Text.Contains("★"))
            {
                getId = pttline.Text.Substring(0, 9);
            }
            pttline.UniqueId = getSelectedID(getId);
            if (pttline.UniqueId == "★" && !string.IsNullOrEmpty(lastID.UniqueId))
            {
                int id = pttline.No - lastID.No + int.Parse(lastID.UniqueId);
                pttline.UniqueId = id.ToString();
            }
            else if(int.TryParse(pttline.UniqueId, out parseNo))
            {
                lastID = pttline;
            }
        }

        private static PTTBlock getBlock(TelnetData point)
        {
            PTTBlock pb = new PTTBlock();

            pb.Text = point.Text;
            pb.TopPoint = (int)(point.Position.X * _fontSize);
            pb.LeftPoint = (int)(point.Position.Y * _fontSize / 2);
            pb.Width = point.Count * _fontSize / 2;
            pb.ForeColor = new SolidColorBrush(point.ForeColor);
            pb.BackColor = new SolidColorBrush(point.BackColor);
            pb.ZIndex = 1;
            if (point.TwWord)
            {
                pb.FontName = cht_fontFamily;
            }
            else
            {
                pb.FontName = ansi_fontFamily;
            }
            pb.Blinking = point.Blinking;
            pb.DualColor = point.DualColor;

            return pb;
            
        }

        public static bool isPushable = false;
        private static List<string> bottomTitle = new List<string>();
        public static Windows.UI.Xaml.LineStackingStrategy lineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        //public static double lineHeight = 30;
        public static string forClipBoard = "";
        public static void checkMode(string text)
        {
            LastMode = currentMode;
            isPushable = false;
            if (text.Contains("動畫播放中"))
            {
                currentMode = BBSMode.AnimationPlay;
            }
            else if (text.Contains("任意鍵繼續") || text.Contains("開始播放") || text.Contains("空白鍵"))
            {
                currentMode = BBSMode.PressAnyKey;
            }
            else if (text.Contains("瀏覽"))
            {
                currentMode = BBSMode.ArticleBrowse;
                isPushable = true;
            }
            else if(text.Contains("編輯文章"))
            {
                currentMode = BBSMode.Editor;
            }
            else if(text.Contains("文章選讀"))
            {
                currentMode = BBSMode.BoardList;
                isPushable = true;
            }
            else if (text.Contains("呼叫") || text.Contains("功能鍵") || text.Contains("選擇看板") || text.Contains("鴻雁往返"))
            {
                currentMode = BBSMode.BoardList;
            }
            else if (text.Contains("分類看板") || text.Contains("板主") || text.Contains("郵件選單")) //push article
            {
                currentMode = BBSMode.BoardList;
            }
            else
            {
                currentMode = BBSMode.Other;
            }

            //Debug.WriteLine(mode.ToString());
        }

        public static bool isPortrait = false;
        public static void showAll(Canvas PTTCanvas, TelnetData[,] BBSPage)
        {
            PTTCanvas.Children.Clear();
            getText();
            //_currentMode = checkMode();
            if (!isPortrait)
            { 
                DisplayLanscape(PTTCanvas, BBSPage);
            }
            else
            {
                DisplayPortrait(PTTCanvas, BBSPage);
            }
            
        }

        private static void DisplayPortrait(Canvas PTTCanvas, TelnetData[,] BBSPage)
        {
            PTTCanvas.Children.Clear();
        }

        static int debugStart;
        
        public static void DisplayLanscape(Canvas PTTCanvas, TelnetData[,] BBSPage)
        {
            debugStart = Environment.TickCount;
            pttDisplay.LoadFromSource(BBSPage);
            Debug.WriteLine("load to display model time: {0}", Environment.TickCount - debugStart);
            //display on the Canvas
            debugStart = Environment.TickCount;
            ProcessCanvas(PTTCanvas);
            Debug.WriteLine("process canvas time: {0}", Environment.TickCount - debugStart);
        }

        private static void CompressBBSData(TelnetData[,] BBSPage, List<TelnetData>[] final)
        {
            TelnetData point;
            for (int row = 0; row < 24; row++)
            {
                final[row] = new List<TelnetData>();
                point = new TelnetData();
                point.cloneFrom(BBSPage[row, 0]);
                //point.Text = "";
                point.Position = new Point(row, 0);
                for (int col = 1; col < 80; col++)
                {
                    if (point == BBSPage[row, col]) //only check backColor, forColor, blinking, dualcolor, isTwWord
                    {
                        point.Text += BBSPage[row, col].Text;
                    }
                    else
                    {
                        point.Count = col - (int)point.Position.Y;
                        if(point.Text == "")
                        { point.Text = (char)0xA0 + ""; }
                        final[row].Add(point);
                        point = new TelnetData();
                        point.cloneFrom(BBSPage[row, col]);
                        point.Position = new Point(row, col);
                    }
                }
                if (point.Text == "") //record the final portion
                {
                    point.Text = (char)0xA0 + "";
                }
                point.Count = 80 - (int)point.Position.Y;
                final[row].Add(point);
            }
        }

        private static bool isChineseWord(string p)
        {
            if (p[p.Length-1] > 0x3040)
            {
                return true;
            }
            else
            { 
                return false;
            }
        }

        public static void ProcessCanvas(Canvas PTTCanvas)
        {
            foreach (var line in Lines)
            {
                foreach (var block in line.Blocks)
                {
                    //display background
                    PTTCanvas.Children.Add(showBackground(block.BackColor.Color, block.Width, block.TopPoint, block.LeftPoint, 0));
                    //display text
                    if (block.Blinking && block.DualColor) // half blinking
                    {
                        PTTCanvas.Children.Add(showBlinking(block.Text, block.ForeColor.Color, block.BackColor.Color, 
                            block.Width*2, block.TopPoint, block.LeftPoint - (int)_fontSize / 2, 1));
                    }
                    else if (block.DualColor) // half color
                    {
                        PTTCanvas.Children.Add(showWord(block.Text, block.ForeColor.Color, 
                            block.Width*2, block.TopPoint, block.LeftPoint - (int)_fontSize / 2, 1));
                    }
                    else if (block.Blinking) // full blinking
                    {
                        PTTCanvas.Children.Add(showBlinking(block.Text, block.ForeColor.Color, block.BackColor.Color,
                            block.Width, block.TopPoint, block.LeftPoint, 2));
                    }
                    else //normal
                    {
                        PTTCanvas.Children.Add(showWord(block.Text, block.ForeColor.Color, block.Width, block.TopPoint, block.LeftPoint, 2));
                    }
                }
            }
            Debug.WriteLine("children count = {0}", PTTCanvas.Children.Count);
        }

        public event EventHandler LinesPropChanged;
        public virtual void onLinesChanged(EventArgs e)
        {
            if (LinesPropChanged != null)
            {
                LinesPropChanged(this, e);
            }
        }

        //static TelnetData highByte;
        //static bool lowByte = false;
        //public static void buildScreen(Canvas PTTCanvas, List<TelnetData>[] BBSPage)
        //{
        //    TextBlock tempTB;
        //    Border tempBgColor;
        //    TelnetData ptHighByte = new TelnetData();
        //    int lastY;
        //    forClipBoard = "";
        //    debugStart = Environment.TickCount;
        //    for (int i = 0; i < 24; i++)
        //    {
        //        if (TelnetANSIParser.BBSPage[i] == null) continue;

        //        IOrderedEnumerable<TelnetData> show = BBSPage[i].OrderBy(ob => ob.Position.Y);

        //        lastY = -1;
        //        foreach (TelnetData block in show)
        //        {
        //            if (block.Text == "") continue;
        //            if (block.BBSUI.Count != 0) continue;

        //            tempBgColor = new Border();
        //            tempBgColor.Background = new SolidColorBrush(block.BackColor);
        //            if (block.BackColor == TelnetANSIParserCanvas.nBlack)
        //            { tempBgColor.Background = new SolidColorBrush(Colors.Transparent); }
        //            tempBgColor.Width = block.Count * _fontSize / 2;
        //            tempBgColor.Height = _fontSize;
        //            Canvas.SetLeft(tempBgColor, block.Position.Y * _fontSize / 2);
        //            Canvas.SetTop(tempBgColor, i * _fontSize);
        //            PTTCanvas.Children.Add(tempBgColor);
        //            block.BBSBackground = tempBgColor;
        //            block.BBSUI.Add(tempBgColor);

        //            tempTB = new TextBlock();
        //            tempTB.LineStackingStrategy = lineStackingStrategy;
        //            tempTB.LineHeight = _fontSize;
        //            tempTB.FontSize = _fontSize;
        //            if (block.Text[0] > 0x3040)
        //            {
        //                tempTB.FontFamily = new FontFamily(cht_fontFamily);
        //                tempTB.TextLineBounds = TextLineBounds.TrimToCapHeight;
        //                tempTB.Padding = new Thickness(0, chtOffset, 0, 0);
        //            }
        //            else
        //            { tempTB.FontFamily = new FontFamily(ansi_fontFamily); }

        //            if (highByte != null)
        //            {
        //                if (block.DualColor)
        //                {
        //                    if (highByte.Blinking)
        //                    {
        //                        showBlinking(PTTCanvas, highByte.Text, highByte.ForeColor,
        //                       highByte.BackColor, _fontSize / 2, i, (int)highByte.Position.Y, 2);
        //                    }
        //                    else
        //                    {
        //                        showDualColor(PTTCanvas, highByte.Text, highByte.ForeColor, _fontSize / 2,
        //                            i, (int)highByte.Position.Y, 2);
        //                    }

        //                    ptHighByte.BBSbldu = PTTCanvas.Children[PTTCanvas.Children.Count - 1];
        //                    ptHighByte.BBSUI.Add(PTTCanvas.Children[PTTCanvas.Children.Count - 1]);
        //                    highByte = null;
        //                    lowByte = true;
        //                }
        //            }

        //            if (block.DualColor && lowByte == false)
        //            {
        //                ptHighByte = block;
        //                highByte = new TelnetData();
        //                highByte.cloneFrom(block);
        //                tempBgColor.Width = _fontSize / 2;
        //                lowByte = true;
        //                continue;
        //            }

        //            if (lowByte)
        //            {
        //                Canvas.SetLeft(tempBgColor, (block.Position.Y + 1) * _fontSize / 2);
        //                tempBgColor.Width = _fontSize / 2;
        //                lowByte = false;
        //            }

        //            if ((int)block.Position.Y - lastY > 0)
        //            {
        //                forClipBoard += new string((char)0xA0, (int)block.Position.Y - lastY - 1);
        //            }
        //            tempTB.Text += block.Text;
        //            generateHyperlink(block.Text, tempTB, Colors.LightBlue);
        //            if (block.Blinking)
        //            {
        //                tempTB.Foreground = new SolidColorBrush(block.BackColor);
        //                showBlinking(PTTCanvas, block.Text, block.ForeColor,
        //                   block.BackColor, block.Count * _fontSize / 2, i, (int)block.Position.Y, 1);
        //                block.BBSbldu = PTTCanvas.Children[PTTCanvas.Children.Count - 1];
        //                block.BBSUI.Add(PTTCanvas.Children[PTTCanvas.Children.Count - 1]);
        //            }
        //            else
        //            {
        //                tempTB.Foreground = new SolidColorBrush(block.ForeColor);
        //            }
        //            lastY = (int)block.Position.Y + block.Count - 1;
        //            Canvas.SetTop(tempTB, i * _fontSize);
        //            Canvas.SetLeft(tempTB, block.Position.Y * _fontSize / 2);
        //            PTTCanvas.Children.Add(tempTB);
        //            block.BBSText = tempTB;
        //            block.BBSUI.Add(tempTB);
        //            forClipBoard += tempTB.Text;
        //        }
        //        //contains "★"
        //        if (Regex.IsMatch(tempTB.Text, @"(\s+\u2605\s+\S)"))
        //        {
        //            if (lastID != "")
        //            {
        //                if (bottomTitle.Count == 0)
        //                {
        //                    bottomTitle.Add(lastID);
        //                    bottomTitle.Add(tempTB.Text);
        //                }
        //                else
        //                { bottomTitle.Add(tempTB.Text); }
        //            }
        //        }
        //        else
        //        { lastID = getSelectedID(tempTB.Text); }

        //        forClipBoard += "\n";
        //    }
        //    Debug.WriteLine("canvas draw time = {0}, canvas.children count = {1}", Environment.TickCount - debugStart,
        //        PTTCanvas.Children.Count);
        //}

        private static void buildPortrait(Canvas PTTCanvas, List<TelnetData>[] BBSPage)
        {
            TextBlock tb;
            Border bbsBground;

            bbsBground = new Border();
            bbsBground.Height = _fontSize;
            bbsBground.Width = 40 * _fontSize / 2;
            bbsBground.Background = new SolidColorBrush(Colors.Yellow);
            PTTCanvas.Children.Add(bbsBground);
            Canvas.SetTop(bbsBground, 5 * _fontSize);
            Canvas.SetLeft(bbsBground, 20 * _fontSize / 2);

            tb = new TextBlock();
            tb.FontFamily = new FontFamily(cht_fontFamily);
            tb.FontSize = _fontSize;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.Width = PTTCanvas.Width;
            tb.LineHeight = _fontSize;
            tb.LineStackingStrategy = lineStackingStrategy;

            Run run1 = new Run();
            run1.Text = "1234567890一二三四五六七八九零12345678901234567890一二三四五六";
            run1.Foreground = new SolidColorBrush(Colors.Blue);
            Run run2 = new Run();
            run2.Text = "34567890一二三四五七八九零1234567890";
            run2.Foreground = new SolidColorBrush(Colors.Red);
            tb.Inlines.Add(run1);
            tb.Inlines.Add(run2);
            PTTCanvas.Children.Add(tb);
            Canvas.SetTop(tb, 5 * _fontSize);

            TextBlock tb2 = new TextBlock();
            tb2.FontFamily = new FontFamily(cht_fontFamily);
            tb2.FontSize = _fontSize;
            tb2.Foreground = new SolidColorBrush(Colors.GreenYellow);
            tb2.Width = _fontSize / 2;
            tb2.Text = "二";
            PTTCanvas.Children.Add(tb2);
            Canvas.SetLeft(tb2, 13 * _fontSize / 2);
            Canvas.SetTop(tb2, 5 * _fontSize);

            TextBlock tb3 = new TextBlock();
            tb3.FontFamily = new FontFamily(cht_fontFamily);
            tb3.FontSize = _fontSize;
            tb3.Foreground = new SolidColorBrush(Colors.Gray);
            tb3.Text = "二二二二二二二二二二二二二二二二三二二二二二四二二八九零12345";
            PTTCanvas.Children.Add(tb3);
            Canvas.SetTop(tb3, 6 * _fontSize);
        }

        public static UIElement showBlinking(string word, Color fg, Color bg, double tbWidth, double top, double left, int zIndex)
        {
            TextBlock btb = new TextBlock();
            btb.Text = word;
            btb.Foreground = new SolidColorBrush(fg);
            btb.FontSize = _fontSize;
            btb.Width = tbWidth;
            if (btb.Text[0] > 0x3040)
            { 
                btb.FontFamily = new FontFamily(cht_fontFamily);
                btb.TextLineBounds = TextLineBounds.TrimToCapHeight;
                btb.Padding = new Thickness(0, chtOffset, 0, 0);
            }
            else
            { btb.FontFamily = new FontFamily(ansi_fontFamily); }
            //btb.LineStackingStrategy = lineStackingStrategy;
            //btb.LineHeight = _fontSize;

            Canvas.SetLeft(btb, left);
            Canvas.SetTop(btb, top);
            Canvas.SetZIndex(btb, zIndex);
            //PTTCanvas.Children.Add(btb);

            ColorAnimationUsingKeyFrames caukf = new ColorAnimationUsingKeyFrames();
            DiscreteColorKeyFrame dckf = new DiscreteColorKeyFrame();
            dckf.KeyTime = TimeSpan.FromSeconds(1);
            dckf.Value = bg;
            caukf.KeyFrames.Add(dckf);
            Storyboard flashText = new Storyboard();
            Storyboard.SetTargetProperty(caukf, "(TextBlock.Foreground).(SolidColorBrush.Color)");
            Storyboard.SetTarget(caukf, (TextBlock)btb);
            flashText.Children.Add(caukf);
            flashText.Duration = TimeSpan.FromSeconds(2);
            RepeatBehavior rb = new RepeatBehavior();
            rb.Type = RepeatBehaviorType.Forever;
            flashText.RepeatBehavior = rb;
            flashText.Begin();

            return btb;
        }

        public static Inline runText(string big5Word, Color fg)
        {
            Run r = new Run();
            r.Text = big5Word;
            if(r.Text[0] > 0x3040)
            {
                r.FontFamily = new FontFamily(cht_fontFamily);
            }
            else
            {
                r.FontFamily = new FontFamily(ansi_fontFamily);
            }
            r.FontSize = _fontSize;
            r.Foreground = new SolidColorBrush(fg);

            return r;
        }

        public static UIElement showWord(string big5Word, Color fg, double tbWidth, double top, double left, int zIndex)
        {
            TextBlock tb = new TextBlock();
            //twoColor1.LineStackingStrategy = lineStackingStrategy;
            //twoColor1.LineHeight = _fontSize;
            tb.Text = big5Word;
            if (tb.Text[0] > 0x3040)
            {
                tb.FontFamily = new FontFamily(cht_fontFamily);
                tb.TextLineBounds = TextLineBounds.TrimToCapHeight;
                tb.Padding = new Thickness(0, chtOffset, 0, 0);
            }
            else
            { tb.FontFamily = new FontFamily(ansi_fontFamily); }
            tb.FontSize = _fontSize;
            tb.Width = tbWidth;
            tb.Foreground = new SolidColorBrush(fg);
            Canvas.SetLeft(tb, left);
            Canvas.SetTop(tb, top);
            Canvas.SetZIndex(tb, zIndex);
            //PTTCanvas.Children.Add(twoColor1);
            return tb;
        }

        public static UIElement showBackground(Color bg, double bgWidth, double top, double left, int zIndex)
        {
            Border border = new Border();
            border.Height = _fontSize;
            border.Width = bgWidth;
            border.Background = new SolidColorBrush(bg);
            Canvas.SetTop(border, top);
            Canvas.SetLeft(border, left);
            return border;
        }

        public static void generateHyperlink(string source, TextBlock clipB, Color linkForeground)
        {
            string matchURL = @"(https?:\/\/[\x21-\x7E]+)(\s|[^\x21-\x7E]|$)";
            string[] afterSplit = Regex.Split(source, matchURL);

            foreach (string toAdd in afterSplit)
            {
                Run tempRun = new Run();
                tempRun.Text = toAdd;
                if (Regex.IsMatch(toAdd, @"https?:\/\/[\x21-\x7E]+"))
                {
                    tempRun.Foreground = new SolidColorBrush(linkForeground);
                    Underline tempUL = new Underline();
                    tempUL.Inlines.Add(tempRun);
                    Hyperlink tempHyperLink = new Hyperlink();
                    tempHyperLink.Inlines.Add(tempUL);
                    try
                    {
                        tempHyperLink.NavigateUri = new Uri(toAdd);
                    }
                    catch(Exception)
                    {
                        //Home.ShowMessage(msg.Message);
                    }
                    clipB.Inlines.Add(tempHyperLink);
                }
                else
                {
                    clipB.Inlines.Add(tempRun);
                }
            }
        }

        public static string getSelectedID(string source)
        {
            string[] afterSplit;
            string matchPattern = @"(\d+\s)|(\d+[\.X\)])|(\([A-Z]\))";
            //source = TelnetANSIParser.CropText(source, 0, 40);
            afterSplit = Regex.Split(source, matchPattern);
            char[] forTrim = { '.', 'X', ')', '●', '(', '\xa0' };

            foreach (string x in afterSplit)
            {
                //Debug.WriteLine(x);
                if (Regex.IsMatch(x, matchPattern))
                {
                    string id;
                    if (x == "(X)")
                    { return "X"; }
                    else
                    {
                        id = x.TrimStart(forTrim);
                        id = id.TrimEnd(forTrim);
                        return id;
                    }
                }
                else if (x.Contains("★"))
                { return "★"; }
            }
            return "";
        }

        public static int lastNum;
        public static string[] menuList = new string[24];
        public static void getText()
        { 
            //int lastY;
            forClipBoard = "";
            TelnetData lastBlock = new TelnetData();
            lastNum = 0;

            //for (int i = 0; i < 24; i++)
            //{
            //    menuList[i] = "";
            //    if (TelnetANSIParser.BBSPage[i] == null) continue;
            //    IOrderedEnumerable<TelnetData> show3 = TelnetANSIParser.BBSPage[i].OrderBy(ob => ob.Position.Y);
            //    lastY = -1;
            //    foreach (TelnetData block in show3)
            //    {
            //        if (block.Text == lastBlock.Text && block.Position == lastBlock.Position ) continue;
            //        if ((int)block.Position.Y - lastY > 0)
            //        {
            //            forClipBoard += new string((char)0xA0, (int)block.Position.Y - lastY - 1);
            //        }

            //        lastY = (int)block.Position.Y + block.Count - 1;

            //        forClipBoard += block.Text;
            //        menuList[i] += block.Text;
                    
            //        lastBlock = block;
            //    }
            //    if (menuList[i].Length > 10)
            //    {
            //        if (lastNum == 0 && menuList[i].Substring(0, 9).Contains("★"))
            //        { lastNum = i - 1; }
            //    }
            //    forClipBoard += "\n";
            //}
        }


        internal static async void showBBS(Canvas PTTCanvas)
        {
            //check auto login
            if (TelnetConnect.connection.autoLogin && LoginScreen)
            {
                await TelnetConnect.sendCommand(TelnetConnect.connection.account + "\r" + TelnetConnect.connection.password + "\r");
                TelnetConnect.connection.autoLogin = false;
                LoginScreen = false;
            }
            PTTCanvas.Children.Clear();
            ProcessCanvas(PTTCanvas);
        }

    }
}
