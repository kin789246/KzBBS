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

namespace KzBBS
{
    enum BBSMode : byte
    {
        PressAnyKey, //任意鍵        
        BoardList, //看板列表
        ArticleBrowse, //文章瀏覽
        AnimationPlay, //動畫播放
        Editor, //編輯文章
        Other,
    };

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
        //private static TelnetSocket _ptt;
        public static BBSMode _currentMode;
        public static bool isPushable = false;
        private static List<string> bottomTitle = new List<string>();
        public static double _fontSize = 30; //default font size =30
        public static Windows.UI.Xaml.LineStackingStrategy lineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        //public static double lineHeight = 30;
        public static string forClipBoard = "";
        public static BBSMode checkMode()
        { 
            string tempTitle = "";
            BBSMode mode;
            isPushable = false;
            
            //load the last line
            //IOrderedEnumerable<TelnetData> show2 = TelnetANSIParserCanvas.BBSPage[23].OrderBy(ob => ob.Position.Y);
            //foreach (TelnetData tD in show2)
            //{ tempTitle += tD.Text; }
            tempTitle = menuList[23];

            if (tempTitle.Contains("任意鍵繼續") || tempTitle.Contains("開始播放") || tempTitle.Contains("空白鍵"))
            {
                mode = BBSMode.PressAnyKey;
            }
            else if (tempTitle.Contains("瀏覽"))
            {
                mode = BBSMode.ArticleBrowse;
                isPushable = true;
            }
            else if (tempTitle.Contains("動畫播放中"))
            {
                mode = BBSMode.AnimationPlay;
            }
            else if(tempTitle.Contains("編輯文章"))
            {
                mode = BBSMode.Editor;
            }
            else if(tempTitle.Contains("文章選讀"))
            {
                mode = BBSMode.BoardList;
                isPushable = true;
            }
            else if (tempTitle.Contains("呼叫器") || tempTitle.Contains("功能鍵") || tempTitle.Contains("選擇看板")
                 || tempTitle.Contains("鴻雁往返"))
            {
                mode = BBSMode.BoardList;
            }
            else if(string.IsNullOrEmpty(tempTitle))
            {
                tempTitle = menuList[0];
                if(tempTitle.Contains("分類看板") || tempTitle.Contains("板主") || tempTitle.Contains("郵件選單"))
                { mode = BBSMode.BoardList; }
                else
                { mode = BBSMode.Other; }
            }
            else
            {
                mode = BBSMode.Other;
            }

            //Debug.WriteLine(mode.ToString());
            return mode;
        }

        public static bool isPortrait = false;
        public static void showAll(Canvas PTTCanvas, List<TelnetData>[] BBSPage)
        {
            getText();
            _currentMode = checkMode();
            //if (!isPortrait)
            //{ buildScreen(PTTCanvas, BBSPage); }
            //{
            //    TextBlock tempTB = new TextBlock();
            //    tempTB.FontSize = 30;
            //    tempTB.Text = "瀏覽瀏覽開放開放";
            //    tempTB.FontFamily = new FontFamily(cht_fontFamily);
            //    //tempTB.Height = 30;
            //    Canvas.SetTop(tempTB, 30);
            //    Canvas.SetLeft(tempTB, 100);
            //    PTTCanvas.Children.Add(tempTB);

            //    TextBlock tempTB2 = new TextBlock();
            //    tempTB2.FontSize = 30;
            //    tempTB2.Text = "開放開放";
            //    tempTB2.FontFamily = new FontFamily(cht_fontFamily);
            //    //tempTB2.Height = 30;
            //    Canvas.SetTop(tempTB2, 60);
            //    Canvas.SetLeft(tempTB2, 100);
            //    PTTCanvas.Children.Add(tempTB2);

            //    TextBlock tempTB3 = new TextBlock();
            //    tempTB3.FontSize = 30;
            //    tempTB3.Text = "abcdefgAAA";
            //    tempTB3.FontFamily = new FontFamily(ansi_fontFamily);
            //    //tempTB3.Height = 30;
            //    Canvas.SetTop(tempTB3, 60);
            //    Canvas.SetLeft(tempTB3, 220);
            //    PTTCanvas.Children.Add(tempTB3);

            //    TextBlock tempTB4 = new TextBlock();
            //    tempTB4.FontSize = 30;
            //    tempTB4.Text = "開放開放開放開放";
            //    tempTB4.FontFamily = new FontFamily(cht_fontFamily);
            //    //tempTB4.Height = 30;
            //    Canvas.SetTop(tempTB4, 90);
            //    Canvas.SetLeft(tempTB4, 100);
            //    PTTCanvas.Children.Add(tempTB4);
            //}
            //else
            //{ buildScreen(PTTCanvas, BBSPage); }
            //{ buildPortrait(PTTCanvas, BBSPage); }
        }

        //static int debugStart;
        //static string lastID = "";
        
        public static void DisplayLanscape(Canvas PTTCanvas, TelnetData[,] BBSPage)
        {
            List<TelnetData>[] final = new List<TelnetData>[24];
            PTTCanvas.Children.Clear();
            //compress the bbs data
            CompressBBSData(BBSPage, final);

            //display on the Canvas
            ProcessCanvas(PTTCanvas, final);
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

        private static void ProcessCanvas(Canvas PTTCanvas, List<TelnetData>[] final)
        {
            foreach (List<TelnetData> line in final)
            {
                foreach (TelnetData item in line)
                {
                    //display background
                    PTTCanvas.Children.Add(showBackground(item.BackColor, item.Count * _fontSize / 2,
                        (int)item.Position.X, (int)item.Position.Y, 0));
                    //display text
                    if (item.Blinking && item.DualColor) // half blinking
                    {
                        PTTCanvas.Children.Add(showBlinking(item.Text, item.ForeColor, item.BackColor, item.Count * _fontSize,
                            (int)item.Position.X, (int)(item.Position.Y - 1), 1));
                    }
                    else if (item.DualColor) // half color
                    {
                        PTTCanvas.Children.Add(showWord(item.Text, item.ForeColor, item.Count * _fontSize,
                           (int)item.Position.X, (int)(item.Position.Y - 1), 1));
                    }
                    else if (item.Blinking) // full blinking
                    {
                        PTTCanvas.Children.Add(showBlinking(item.Text, item.ForeColor, item.BackColor, item.Count * _fontSize / 2,
                           (int)item.Position.X, (int)item.Position.Y, 2));
                    }
                    else //normal
                    {
                        PTTCanvas.Children.Add(showWord(item.Text, item.ForeColor, item.Count * _fontSize / 2,
                           (int)item.Position.X, (int)item.Position.Y, 2));
                    }
                }
            }
            Debug.WriteLine("children count = {0}", PTTCanvas.Children.Count);
        }

        public static double chtOffset = 1;
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

        public static UIElement showBlinking(string word, Color fg, Color bg,
            double tbWidth, int curX, int curY, int zIndex)
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

            Canvas.SetLeft(btb, curY * _fontSize / 2);
            Canvas.SetTop(btb, curX * _fontSize);
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

        public static UIElement showWord(string big5Word, Color fg,
            double tbWidth, int curX, int curY, int zIndex)
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
            Canvas.SetLeft(tb, curY * _fontSize / 2);
            Canvas.SetTop(tb, curX * _fontSize);
            Canvas.SetZIndex(tb, zIndex);
            //PTTCanvas.Children.Add(twoColor1);
            return tb;
        }

        public static UIElement showBackground(Color bg, double bgWidth, int curX, int curY, int zIndex)
        {
            Border border = new Border();
            border.Height = _fontSize;
            border.Width = bgWidth;
            border.Background = new SolidColorBrush(bg);
            Canvas.SetTop(border, curX * _fontSize);
            Canvas.SetLeft(border, curY * _fontSize / 2);
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

    }
}
