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
        ArticleList, //文章列表
        MailList, //郵件選單
        Essence, //精華區
        MainList, //主畫面 
        ClassBoard, //分類看板
        ArticleBrowse, //文章瀏覽
        AnimationPlay, //動畫播放
        Editor, //編輯文章
        Login, //登入畫面
        Other,
    };

    class PTTBlock
    {
        public string Text { get; set; }
        public double LeftPoint { get; set; }
        public double TopPoint { get; set; }
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
        public string Author { get; set; }
        public bool Changed { get; set; }
        public List<PTTBlock> Blocks { get; set; }

        public PTTLine()
        {
            No = 0;
            Text = "";
            UniqueId = "";
            Author = "";
            Changed = true;
            Blocks = new List<PTTBlock>();
        }

        public PTTLine(PTTLine line)
        {
            No = line.No;
            Text = line.Text;
            UniqueId = line.UniqueId;
            Author = line.Author;
            Changed = line.Changed;

            Blocks = new List<PTTBlock>();
            foreach (var block in line.Blocks)
            {
                Blocks.Add(block);
            }
        }
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
        public static List<UIElement>[] toRemove = new List<UIElement>[24];
        public static bool PTTMode;
        public static string User = "";
        /*---------------------------------------*/

        public static PTTDisplay pttDisplay = new PTTDisplay();

        private static List<PTTLine> toShowLines = new List<PTTLine>();
        ///<Summary>
        ///For the page that show on the screen
        ///</Summary>
        public static List<PTTLine> ToShowLines
        {
            get { return toShowLines; }
            set { toShowLines = value; }
        }

        private static List<PTTLine> boardLists = new List<PTTLine>();
        ///<Summary>
        ///Save the lists
        ///</Summary>
        public static List<PTTLine> BoardLists
        {
            get { return boardLists ; }
            set { boardLists = value; }
        }

        private static List<PTTLine> lastLines = new List<PTTLine>();
        /// <summary>
        /// For compare if changed
        /// </summary>
        public static List<PTTLine> LastLines
        {
            get { return lastLines; }
        }

        private static List<PTTLine> lines = new List<PTTLine>();
        /// <summary>
        /// For the page load from server
        /// </summary>
        public static List<PTTLine> Lines
        {
            get { return lines; }
        }

        internal static void resetAllSetting()
        {
            LastMode = BBSMode.Other;
            currentMode = BBSMode.Other;
            LoginScreen = false;
            lines.Clear();
            lastLines.Clear();
            toRemove = new List<UIElement>[24];
        }

        public void LoadFromSource(TelnetData[,] BBSPage)
        {
            lines.Clear();
            TelnetData point;
            lastID = new PTTLine();
            for (int row = 0; row < 24; row++)
            {
                PTTLine pttline = new PTTLine();
                pttline.Blocks = new List<PTTBlock>();
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
            saveToLastLines();
            //onLinesChanged(new EventArgs());
            if (TelnetPage.Current != null)
            { TelnetPage.Current.onDataChange(); }
            #region debug
            //foreach (var line in Lines)
            //{
            //    Debug.WriteLine(line.Text);
            //}
            #endregion
            Debug.WriteLine("mode: {0}", currentMode.ToString());
        }

        private void saveToLastLines()
        {
            lastLines.Clear();
            foreach (var line in lines)
            {
                lastLines.Add(new PTTLine(line));
            }
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
            if (pttline.No == 22 || pttline.No == 20 || pttline.No == 23 || pttline.No == 21)
            {
                if (pttline.Text.Contains("請輸入代號") || pttline.Text.Contains("請輸入勇者代號") || pttline.Text.Contains("您的帳號"))
                { LoginScreen = true; }
            }
            if (pttline.No == 23)
            {
                if (!string.IsNullOrWhiteSpace(pttline.Text))
                {
                    checkMode(pttline.Text.Substring(0,12));
                    if (currentMode == BBSMode.Other)
                    {
                        if (pttline.Text.Contains("任意鍵繼續") || pttline.Text.Contains("開始播放") || pttline.Text.Contains("空白鍵"))
                        {
                            currentMode = BBSMode.PressAnyKey;
                        }
                        //else
                        //{
                        //    PTTLine line = Lines.First(x => x.No == 0);
                        //    checkMode(line.Text.Substring(0, 12));
                        //}
                    }
                    //find login user
                    if (currentMode == BBSMode.MainList && pttline.Text.Contains("我是"))
                    {
                        foreach (var item in pttline.Text.Split('\xA0'))
                        {
                            if (Regex.IsMatch(item, @"我是\w+"))
                            {
                                User = item.TrimStart('我').TrimStart('是');
                                break;
                            }
                        }
                    }
                }
                else
                {
                    PTTLine line = Lines.First(x => x.No == 0);
                    checkMode(line.Text.Substring(0,12));
                }
            }

            //get line's unique id
            string getId = pttline.Text.Substring(0, 40);
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
            
            //get author
            foreach (var item in Regex.Split(pttline.Text, @"(\d+/\d+\s+\w+\s)"))
            {
                if (Regex.IsMatch(item, @"(\d+/\d+\s+\w+\s)"))
                {
                    pttline.Author = item.Split('\xA0')[1];
                }
            }

            if (pttline.No == 23)
            {
                pttline.UniqueId = "";
                pttline.Author = "";
            }
            if (pttline.No == 22 && pttline.Text.Contains("▲ 回應至"))
            {
                pttline.UniqueId = "";
                pttline.Author = "";
            }
            //check if changed
            if (lastLines.Count != 0)
            {
                foreach (var line in lastLines)
                {
                    if (line.No == pttline.No)
                    {
                        if (line.Text == pttline.Text)
                        {
                            pttline.Changed = false;
                        }
                        else
                        {
                            pttline.Changed = true;
                        }
                    }
                    break;
                }
            }
        }

        private static PTTBlock getBlock(TelnetData point)
        {
            PTTBlock pb = new PTTBlock();

            pb.Text = point.Text;
            pb.TopPoint = point.Position.X * _fontSize;
            pb.LeftPoint = point.Position.Y * _fontSize / 2;
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
            //if (pb.Text.Contains("\x25CF")) { Debug.WriteLine(pb.Text); }
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
            if (text.Contains("動畫播放中"))
            {
                currentMode = BBSMode.AnimationPlay;
            }
            else if (text.Contains("任意鍵繼續") || text.Contains("開始播放") || text.Contains("空白鍵"))
            {
                currentMode = BBSMode.PressAnyKey;
            }
            else if (text.Contains("瀏覽") || text.Contains("作者"))
            {
                currentMode = BBSMode.ArticleBrowse;
            }
            else if(text.Contains("編輯文章"))
            {
                currentMode = BBSMode.Editor;
            }
            else if (text.Contains("看板列表") || text.Contains("選擇看板"))
            {
                currentMode = BBSMode.BoardList;
            }
            else if (text.Contains("文章選讀") || text.Contains("板主"))
            {
                currentMode = BBSMode.ArticleList;
            }
            else if (text.Contains("郵件選單") || text.Contains("鴻雁往返"))
            {
                currentMode = BBSMode.MailList;
            }
            else if (text.Contains("呼叫") || text.Contains("主功能表") || text.Contains("電子郵件") || text.Contains("聊天說話")
                || text.Contains("個人") || text.Contains("工具程式") || text.Contains("熱門話題") || text.Contains("使用者統計")
                || text.Contains("網路遊樂場") || text.Contains("Ptt量販店") || text.Contains("Ptt棋院")
                || text.Contains("名單編輯") || text.Contains("星期"))
            {
                currentMode = BBSMode.MainList;
            }
            else if (text.Contains("功能鍵") || text.Contains("精華文章"))
            {
                currentMode = BBSMode.Essence;
            }
            else if (text.Contains("分類看板"))
            {
                currentMode = BBSMode.ClassBoard;
            }
            else
            {
                currentMode = BBSMode.Other;
            }
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
            //ProcessCanvas(PTTCanvas, PTTDisplay.Lines, operationBoard);
            Debug.WriteLine("process canvas time: {0}", Environment.TickCount - debugStart);
        }

        //private static void CompressBBSData(TelnetData[,] BBSPage, List<TelnetData>[] final)
        //{
        //    TelnetData point;
        //    for (int row = 0; row < 24; row++)
        //    {
        //        final[row] = new List<TelnetData>();
        //        point = new TelnetData();
        //        point.cloneFrom(BBSPage[row, 0]);
        //        //point.Text = "";
        //        point.Position = new Point(row, 0);
        //        for (int col = 1; col < 80; col++)
        //        {
        //            if (point == BBSPage[row, col]) //only check backColor, forColor, blinking, dualcolor, isTwWord
        //            {
        //                point.Text += BBSPage[row, col].Text;
        //            }
        //            else
        //            {
        //                point.Count = col - (int)point.Position.Y;
        //                if(point.Text == "")
        //                { point.Text = (char)0xA0 + ""; }
        //                final[row].Add(point);
        //                point = new TelnetData();
        //                point.cloneFrom(BBSPage[row, col]);
        //                point.Position = new Point(row, col);
        //            }
        //        }
        //        if (point.Text == "") //record the final portion
        //        {
        //            point.Text = (char)0xA0 + "";
        //        }
        //        point.Count = 80 - (int)point.Position.Y;
        //        final[row].Add(point);
        //    }
        //}

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

        public async static void ShowBBSListView(StackPanel top, ListView list, StackPanel bottom, List<PTTLine> currPage, Canvas operationBoard)
        {
            //check auto login
            if (TelnetConnect.connection.autoLogin && LoginScreen)
            {
                await TelnetConnect.sendCommand(TelnetConnect.connection.account + "\r" + TelnetConnect.connection.password + "\r");
                TelnetConnect.connection.autoLogin = false;
                LoginScreen = false;
            }
            top.Children.Clear();
            list.Items.Clear();
            bottom.Children.Clear();
            operationBoard.Children.Clear();
            if (currentMode == BBSMode.MainList || currentMode == BBSMode.ClassBoard || currentMode == BBSMode.BoardList
                || currentMode == BBSMode.Essence || currentMode == BBSMode.ArticleList || currentMode == BBSMode.MailList)
            {
                int topCount = 0;
                int bottomCount = 0;
                bool articleTitle = false;
                foreach (var line in currPage)
                {
                    Canvas lineCanvas = getCanvas(list.Width, _fontSize);
                    if (currentMode == BBSMode.MainList && line.No < 12)
                    { 
                        line.UniqueId = "";
                    }
                    if (currentMode == BBSMode.ArticleList && line.No < 2 )
                    {
                        line.UniqueId = "";
                    }
                    
                    if (string.IsNullOrEmpty(line.UniqueId) && articleTitle == false) //continue non-list
                    {
                        foreach (var block in line.Blocks)
                        {
                            saveToCanvas(lineCanvas, block, 0);
                        }
                        if (list.Items.Count == 0)
                        {
                            top.Children.Add(lineCanvas);
                            topCount++;
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(line.Text))
                            {
                                bottom.Children.Add(lineCanvas);
                                bottomCount++;
                            }
                        }
                        articleTitle = false;
                    }
                    else if (string.IsNullOrEmpty(line.UniqueId) && articleTitle == true) //stop list, start another non-list
                    {
                        bottomCount++;
                        lineCanvas = getCanvas(list.Width, _fontSize);
                        foreach (var block in line.Blocks)
                        {
                            saveToCanvas(lineCanvas, block, 0);
                        }
                        bottom.Children.Add(lineCanvas);
                        articleTitle = false;
                    }
                    else if (!string.IsNullOrEmpty(line.UniqueId) && articleTitle == false) //start list, stop non-list
                    {
                        top.Children.Add(lineCanvas);
                        lineCanvas = getCanvas(list.Width, _fontSize);
                        lineCanvas.Tag = line.UniqueId;
                        foreach (var block in line.Blocks)
                        {
                            saveToCanvas(lineCanvas, block, 0);
                        }
                        list.Items.Add(lineCanvas);
                        articleTitle = true;
                    }
                    else //continue list
                    {
                        lineCanvas = getCanvas(list.Width, _fontSize);
                        lineCanvas.Tag = line.UniqueId;
                        foreach (var block in line.Blocks)
                        {
                            saveToCanvas(lineCanvas, block, 0);
                        }
                        list.Items.Add(lineCanvas);
                        articleTitle = true;
                    }
                }
                top.Height = _fontSize * topCount;
                bottom.Height = _fontSize * bottomCount;
                list.Height = _fontSize * (24 - topCount - bottomCount);
                //scroll to the cusor
                int index = (int)TelnetANSIParser.curPos.X;
                switch (currentMode)
                {
                    case BBSMode.BoardList:
                        index -= 3;
                        break;
                    case BBSMode.ArticleList:
                        index -= 3;
                        break;
                    case BBSMode.MailList:
                        index -= 3;
                        break;
                    case BBSMode.Essence:
                        index -= 2;
                        break;
                    case BBSMode.MainList:
                        index -= 13;
                        break;
                    case BBSMode.ClassBoard:
                        index -= 7;
                        break;
                    default:
                        break;
                }
                if (index > -1 && index < list.Items.Count)
                {
                    list.ScrollIntoView(list.Items[index]);
                }
                
            }
            else
            {
                top.Height = 0;
                bottom.Height = 0;
                list.Height = _fontSize * 25;
                Canvas lineCanvas = getCanvas(list.Width, _fontSize * 24);
                string wholeText = "";
                foreach (var line in currPage)
                {
                    wholeText += line.Text + "\n";
                    foreach (var block in line.Blocks)
                    {
                        saveToCanvas(lineCanvas, block, line.No * _fontSize);
                    }
                }
                //set cursor
                lineCanvas.Children.Add(showBlinking("_", Colors.White, TelnetANSIParser.bg, _fontSize / 2
                        , TelnetANSIParser.curPos.X * _fontSize, TelnetANSIParser.curPos.Y * _fontSize / 2, 1));
                list.Items.Add(lineCanvas);
                if (currentMode == BBSMode.ArticleBrowse || currentMode == BBSMode.PressAnyKey)
                {
                    TextBlock tb = getClipTextBlock(Colors.Transparent);
                    generateHyperlink(wholeText, tb, Colors.Transparent);
                    operationBoard.Children.Add(tb);
                }
            }
        }

        private static TextBlock getClipTextBlock(Color fg)
        {
            TextBlock tb = new TextBlock();
            tb.FontFamily = new FontFamily(ansi_fontFamily);
            tb.FontSize = _fontSize;
            tb.LineHeight = _fontSize;
            tb.Margin = new Thickness(0, -2, 0, 0);
            tb.LineStackingStrategy = LineStackingStrategy.BaselineToBaseline;
            tb.Foreground = new SolidColorBrush(fg);
            tb.Padding = new Thickness(0, chtOffset, 0, 0);
            tb.IsTextSelectionEnabled = true;
            return tb;
        }

        private static void saveToCanvas(Canvas lineCanvas, PTTBlock block, double topPoint)
        {
            //display background
            lineCanvas.Children.Add(showBackground(block.BackColor.Color, block.Width, topPoint, block.LeftPoint, 0));
            //display text
            if (block.Blinking && block.DualColor) // half blinking
            {
                lineCanvas.Children.Add(showBlinking(block.Text, block.ForeColor.Color, block.BackColor.Color,
                    block.Width * 2, topPoint, block.LeftPoint - _fontSize / 2, 1));
            }
            else if (block.DualColor) // half color
            {
                lineCanvas.Children.Add(showWord(block.Text, block.ForeColor.Color,
                    block.Width * 2, topPoint, block.LeftPoint - _fontSize / 2, 1));
            }
            else if (block.Blinking) // full blinking
            {
                lineCanvas.Children.Add(showBlinking(block.Text, block.ForeColor.Color, block.BackColor.Color,
                    block.Width, topPoint, block.LeftPoint, 2));
            }
            else //normal
            {
                lineCanvas.Children.Add(showWord(block.Text, block.ForeColor.Color, block.Width, topPoint, block.LeftPoint, 2));
            }
        }

        private static Canvas getCanvas(double width, double height)
        {
            Canvas pttCanvas = new Canvas();
            pttCanvas.Width = width;
            pttCanvas.Height = height;
            pttCanvas.Background = new SolidColorBrush(Colors.Black);

            return pttCanvas;
        }

        public static void ProcessCanvas(Canvas PTTCanvas, List<PTTLine> currPage, Canvas operationBoard)
        {
            operationBoard.Children.Clear();
            string wholeText = "";
            foreach (var line in currPage)
            {
                wholeText += line.Text + "\n";
                if (line.Changed)
                {
                    if (toRemove[line.No] != null)
                    {
                        foreach (var element in toRemove[line.No])
                        {
                            PTTCanvas.Children.Remove(element);
                        }
                        toRemove[line.No].Clear();
                    }

                    foreach (var block in line.Blocks)
                    {
                        if (toRemove[line.No] == null)
                        { toRemove[line.No] = new List<UIElement>(); }
                        saveToCanvas(PTTCanvas, block, block.TopPoint);
                        //background block
                        toRemove[line.No].Add(PTTCanvas.Children[PTTCanvas.Children.Count - 2]);
                        //foreground block
                        toRemove[line.No].Add(PTTCanvas.Children[PTTCanvas.Children.Count - 1]);
                    }
                }
            }
            if (currentMode == BBSMode.ArticleBrowse || currentMode == BBSMode.PressAnyKey)
            {
                TextBlock tb = getClipTextBlock(Colors.Transparent);
                generateHyperlink(wholeText, tb, Colors.Transparent);
                operationBoard.Children.Add(tb);
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
            char[] forTrim = { '.', 'X', '●', '\xa0', '(', ')' };

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
                        id = x.TrimStart(forTrim).TrimEnd(forTrim);
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

        internal static async void showBBS(Canvas PTTCanvas, List<PTTLine> currPage, Canvas operationBoard)
        {
            //check auto login
            if (TelnetConnect.connection.autoLogin && LoginScreen)
            {
                await TelnetConnect.sendCommand(TelnetConnect.connection.account + "\r" + TelnetConnect.connection.password + "\r");
                TelnetConnect.connection.autoLogin = false;
                LoginScreen = false;
            }
            ProcessCanvas(PTTCanvas, currPage, operationBoard);
        }

        internal static void articleAddToCurrPage()
        {
            if (currentMode == BBSMode.ArticleBrowse)
            {
                int start = Lines.FindLastIndex(x => x.Text == ToShowLines[ToShowLines.Count - 1].Text);
                start++;
                int last = Lines.Count - 1;
                if (start == -1) //can't find in Lines
                {
                    start = 0;
                }
                if (!Lines[lines.Count-1].Text.Contains("100%"))
                {
                    last--;
                }
                for (int index = start; index < last; index++)
                {
                    ToShowLines.Add(Lines[index]);
                }
            }
        }

        public static async Task upOrDown(int count)
        {
            if (count < 0) //go down
            {
                for (int i = 0; i < -count; i++)
                {
                    await TelnetConnect.sendCommand(new byte[] { 27, 91, 66 });
                }
            }
            else //go up
            {
                for (int i = 0; i < count; i++)
                {
                    await TelnetConnect.sendCommand(new byte[] { 27, 91, 65 });
                }
            }
        }
    }
}
