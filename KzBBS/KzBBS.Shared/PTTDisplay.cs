using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Media.Animation;

namespace KzBBS
{
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
        public static BBSMode lastMode = BBSMode.Other;
        public static BBSMode currentMode = BBSMode.Other;
        public static bool LoginScreen = false;
        public static bool accPwdKeyIn = false;
        //public static List<UIElement>[] toRemove = new List<UIElement>[24];
        public static bool PTTMode;
        public static string User = "";
        public static char lastChar = char.MinValue;
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

        private static PTTPage lastPage = new PTTPage();
        /// <summary>
        /// record the last page
        /// </summary>
        public static PTTPage LastPage
        {
            get { return lastPage; }
        }

        private static PTTPage currentPage = new PTTPage();
        /// <summary>
        /// For the page load from server
        /// </summary>
        public static PTTPage CurrentPage
        {
            get { return currentPage; }
        }

        internal static void resetAllSetting()
        {
            User = "";
            lastMode = BBSMode.Other;
            currentMode = BBSMode.Other;
            LoginScreen = false;
            accPwdKeyIn = false;
            currentPage = new PTTPage();
            //toRemove = new List<UIElement>[24];
        }

        public void LoadFromSource(TelnetData[,] BBSPage)
        {
            lastPage = new PTTPage(currentPage);
            lastMode = currentMode;

            //check page mode
            //check line 0 and line 23, if matched, set BBSmode
            string textL23 = getLineText(BBSPage, 23, 0, 79);
            string textL0 = getLineText(BBSPage, 0, 0, 20);
            BBSMode modeL0 = checkMode(textL0);
            BBSMode modeL23 = checkMode(textL23);

            if (modeL0 == modeL23)
            {
                string text22 = getLineText(BBSPage, 22, 0, 10);
                string text1 = getLineText(BBSPage, 1, 0, 10);
                if (text22.Contains("▲ 回應至") || text22.Contains("請輸入看板"))
                {
                    currentMode = BBSMode.Other;
                }
                else if (text1.Contains("請確定刪除"))
                {
                    currentMode = BBSMode.Other;
                }
                else
                {
                    currentMode = modeL0;
                }
            }
            else if (modeL23 == BBSMode.PressAnyKey || modeL23 == BBSMode.AnimationPlay || modeL23 == BBSMode.Editor)
            {
                currentMode = modeL23;
            }
            else if (modeL0 == BBSMode.Other && modeL23 == BBSMode.ArticleBrowse)
            {
                currentMode = modeL23;
            }
            else if (modeL23 == BBSMode.Other)
            {
                if (modeL0 == BBSMode.ClassBoard)
                {
                    currentMode = modeL0;
                    string text22 = getLineText(BBSPage, 22, 0, 10);
                    if (text22.Contains("▲ 回應至") || text22.Contains("請輸入看板"))
                    {
                        currentMode = BBSMode.Other;
                    }
                }
                else
                {
                    currentMode = modeL23;
                }
            }
            else
            {
                currentMode = BBSMode.Other;
            }

            //check partial data received at article browse mode
            if (currentMode == BBSMode.ArticleBrowse)
            {
                string currentPageLastLine = getLineText(BBSPage, 23, 0, 79);
                if (currentPageLastLine == LastPage.Lines.Last().Text)
                {
                    currentMode = lastMode;
                    return;
                }
            }

            //#region debug
            //if (currentMode == BBSMode.AnimationPlay)
            //{
            //    Debug.WriteLine(lastChar.ToString());
            //}
            //#endregion

            if (!checkIfKeepPage(lastChar))
            {
                currentMode = lastMode;
                return;
            }

            //start to analyze data
            currentPage = new PTTPage();
            currentPage.Mode = currentMode;

            lastID = "";
            TelnetData point;
            for (int row = 0; row < 24; row++)
            {
                PTTLine pttline = new PTTLine();
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
                    //save the block with same attribute
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
                getLineProp(pttline, BBSPage, row);
                currentPage.Lines.Add(pttline);
            }
            //inform the BBS data has changed
            if (TelnetPage.Current != null)
            { TelnetPage.Current.onDataChange(); }
            #region debug
            //foreach (var line in Lines)
            //{
            //    Debug.WriteLine(line.Text);
            //}
            #endregion
            //Debug.WriteLine("mode: {0}", currentMode.ToString());
        }

        private bool checkIfKeepPage(char lChar)
        {
            bool pageInJudge = true;
            switch (currentMode)
            {
                case BBSMode.BoardList:
                case BBSMode.ArticleList:
                case BBSMode.MailList:
                case BBSMode.Essence:
                    pageInJudge = true;
                    break;
                default:
                    pageInJudge = false;
                    break;
            }
            if (pageInJudge && PTTMode)
            {
                if (lChar != 'H')
                {
                    return false;
                }
            }
            return true;
        }

        private string getLineText(TelnetData[,] BBSPage, int line, int start, int end)
        {
            string result = "";
            for (int i = start; i <= end; i++)
            {
                result += BBSPage[line, i].Text;
            }
            if (result.Trim() == "")
            {
                result = "";
            }
            //result may have duplicate words because of dual color word
            return result;
        }
        
        string lastID = "";
        private void getLineProp(PTTLine pttline, TelnetData[,] onePage, int line)
        {
            //get line text
            foreach (PTTBlock item in pttline.Blocks)
            {
                pttline.Text += item.Text;
            }

            // todo: remove "●" for compare page Text only


            //check login screen
            if (pttline.No == 22 || pttline.No == 20 || pttline.No == 23 || pttline.No == 21)
            {
                if (pttline.Text.Contains("請輸入代號") || pttline.Text.Contains("請輸入勇者代號") || pttline.Text.Contains("您的帳號"))
                { LoginScreen = true; }
            }

            //get line's special properties
            char[] forTrim = { '.', 'X', '●', '\xA0', '(', ')' };
            if (currentMode == BBSMode.MainList)
            {
                //find login user, 48th to 51th == "我是", user = 52th to 65th
                if (pttline.No == 23 && pttline.Text.Contains("我是"))
                {
                    User = getLineText(onePage, line, 52, 65).TrimEnd('\xA0');
                }
                //get uniqueId = the 23th text
                if (pttline.No > 11 && pttline.No != 23)
                {
                    pttline.UniqueId = onePage[line, 23].Text;
                    pttline.number.text = getLineText(onePage, line, 22, 34);
                    pttline.title.text = getLineText(onePage, line, 36, 79).TrimEnd('\xA0');
                    if (!Regex.IsMatch(pttline.UniqueId, @"[A-Z0-9]"))
                    {
                        pttline.UniqueId = "";
                        pttline.number.text = "";
                        pttline.title.text = "";
                    }
                }
                //debug
                //Debug.WriteLine("User: "+ User);
                //Debug.WriteLine("UId: " + pttline.UniqueId + " number: " + pttline.number.text + "\ntitle: " + pttline.title.text);
            }
            else if (currentMode == BBSMode.Essence)
            {
                if (pttline.No > 1 && pttline.No != 23)
                {
                    pttline.UniqueId = getLineText(onePage, line, 0, 6).Trim(forTrim);
                    pttline.number.text = pttline.UniqueId;
                    pttline.isRead.text = getLineText(onePage, line, 8, 9);
                    pttline.isRead.fgColor = onePage[line, 8].ForeColor;
                    pttline.title.text = getLineText(onePage, line, 11, 53).TrimEnd('\xA0');
                    pttline.Author.text = getLineText(onePage, line, 55, 66).TrimEnd('\xA0');
                    pttline.date.text = getLineText(onePage, line, 68, 79);
                }
                //debug
                //Debug.WriteLine("UId: " + pttline.UniqueId + " number: " + pttline.number.text + " isRead: " + pttline.isRead.text 
                  //  + "\ntitle: " + pttline.title.text + "\nAuthor: " + pttline.Author.text + " date: " + pttline.date.text);
            }
            else if (currentMode == BBSMode.BoardList)
            {
                if (pttline.No > 2 && pttline.No != 23)
                {
                    pttline.UniqueId = getLineText(onePage, line, 0, 6).Trim(forTrim);
                    pttline.number.text = pttline.UniqueId;
                    pttline.isRead.text = getLineText(onePage, line, 8, 9);
                    pttline.isRead.fgColor = onePage[line, 8].ForeColor;
                    pttline.board.text = getLineText(onePage, line, 10, 21).TrimEnd('\xA0');
                    pttline.board.fgColor = onePage[line, 10].ForeColor;
                    pttline.category.text = getLineText(onePage, line, 23, 26).TrimEnd('\xA0');
                    pttline.category.fgColor = onePage[line, 23].ForeColor;
                    pttline.title.text = getLineText(onePage, line, 28, 63).TrimEnd('\xA0');
                    pttline.hot.text = getLineText(onePage, line, 64, 66);
                    pttline.hot.fgColor = onePage[line, 64].ForeColor;
                    pttline.Author.text = getLineText(onePage, line, 67, 79).TrimEnd('\xA0');
                }
                //debug
                //Debug.WriteLine("UId: " + pttline.UniqueId + " number: " + pttline.number.text + " isRead: " + pttline.isRead.text + " board: " + pttline.board.text
                  //  + " Category: " + pttline.category.text + "\ntitle: " + pttline.title.text + " hot: " + pttline.hot.text + "\nAuthor: " + pttline.Author.text);
            }
            else if (currentMode == BBSMode.ClassBoard)
            {
                if (pttline.No > 6)
                {
                    pttline.UniqueId = getLineText(onePage, line, 15, 16).Trim(forTrim);
                    pttline.number.text = pttline.UniqueId;
                    pttline.isRead.text = getLineText(onePage, line, 18, 19);
                    pttline.isRead.fgColor = onePage[line, 18].ForeColor;
                    pttline.title.text = getLineText(onePage, line, 20, 48).TrimEnd('\xA0');
                    pttline.Author.text = getLineText(onePage, line, 61, 79).TrimEnd('\xA0');
                }
                //debug
                //Debug.WriteLine("UId: " + pttline.UniqueId + " number: " + pttline.number.text + " isRead: " + pttline.isRead.text
                  //  + "\ntitle: " + pttline.title.text + "\nAuthor: " + pttline.Author.text);
            }
            else if (currentMode == BBSMode.ArticleList)
            {
                if (pttline.No > 2 && pttline.No != 23)
                {
                    pttline.UniqueId = getLineText(onePage, line, 0, 6).Trim(forTrim);
                    pttline.number.text = pttline.UniqueId;
                    //for article no. > 100,000 ex. Gossiping board
                    int lastIDtoInt;
                    int uIDtoInt;
                    if (int.TryParse(pttline.UniqueId, out uIDtoInt) && int.TryParse(lastID, out lastIDtoInt))
                    {
                        if (uIDtoInt < lastIDtoInt)
                        {
                            int result = uIDtoInt + lastIDtoInt / 100000 * 100000;
                            pttline.UniqueId = result.ToString();
                            pttline.number.text = pttline.UniqueId;
                        }
                    }
                    //for bottom title
                    if (pttline.UniqueId == "★" && int.TryParse(lastID, out lastIDtoInt))
                    {
                        pttline.UniqueId = (lastIDtoInt + 1).ToString();
                        pttline.number.text = "★";
                        pttline.number.fgColor = onePage[line, 4].ForeColor;
                    }
                    if (int.TryParse(pttline.UniqueId, out uIDtoInt))
                    {
                        lastID = pttline.UniqueId;
                    }
                    pttline.isRead.text = onePage[line, 8].Text;
                    pttline.isRead.fgColor = onePage[line, 8].ForeColor;
                    pttline.hot.text = getLineText(onePage, line, 9, 10);
                    pttline.hot.fgColor = onePage[line, 9].ForeColor;
                    pttline.date.text = getLineText(onePage, line, 12, 15);
                    pttline.Author.text = getLineText(onePage, line, 17, 28).TrimEnd('\xA0');
                    pttline.Author.fgColor = onePage[line, 17].ForeColor;
                    pttline.title.text = getLineText(onePage, line, 30, 79).TrimEnd('\xA0');
                    pttline.title.fgColor = onePage[line, 30].ForeColor;
                }
                //debug
                //Debug.WriteLine("UId: " + pttline.UniqueId + " number: " + pttline.number.text + " isRead: " + pttline.isRead.text + " hot: " + pttline.hot.text
                  //  + " date: " + pttline.date.text + "\nAuthor: " + pttline.Author.text + "\ntitle: " + pttline.title.text);
            }
            else if (currentMode == BBSMode.MailList)
            {
                if (pttline.No > 2 && pttline.No != 23)
                {
                    pttline.UniqueId = getLineText(onePage, line, 0, 6).Trim(forTrim);
                    pttline.number.text = pttline.UniqueId;
                    pttline.isRead.text = onePage[line, 7].Text;
                    pttline.isRead.fgColor = onePage[line, 7].ForeColor;
                    pttline.date.text = getLineText(onePage, line, 9, 13);
                    pttline.Author.text = getLineText(onePage, line, 15, 28).TrimEnd('\xA0');
                    pttline.title.text = getLineText(onePage, line, 30, 79).TrimEnd('\xA0');
                }
                //debug
                //Debug.WriteLine("UId: " + pttline.UniqueId + " number: " + pttline.number.text + " isRead: " + pttline.isRead.text
                  //  + " date: " + pttline.date.text + "\nAuthor: " + pttline.Author.text + "\ntitle: " + pttline.title.text);
            }
            else if (currentMode == BBSMode.ArticleBrowse)
            {
                string firstText = getLineText(onePage, line, 0, 1);
                if (firstText == "推" || firstText == "噓" || firstText == "→")
                {
                    pttline.push.text = firstText;
                    pttline.push.fgColor = onePage[line, 0].ForeColor;
                    pttline.date.text = getLineText(onePage, line, 67, 79);
                    pttline.title.text = getLineText(onePage, line, 3, 65);
                }
                //don't record the non-push line
                //date format = 08/23 10:51
                if (!Regex.IsMatch(pttline.date.text, @"\d{2}/\d{2}\s\d{2}:\d{2}"))
                {
                    pttline.push.text = "";
                    pttline.title.text = "";
                    pttline.date.text = "";
                }
                //debug
                //Debug.WriteLine("push: " + pttline.push.text + " date: " + pttline.date.text + "\ntitle: " + pttline.title.text);
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


        public static Windows.UI.Xaml.LineStackingStrategy lineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        public static string forClipBoard = "";
        public BBSMode checkMode(string text)
        {
            BBSMode result = BBSMode.Other;
            if (text.Contains("動畫播放中"))
            {
                result = BBSMode.AnimationPlay;
            }
            else if (text.Contains("任意鍵繼續") || text.Contains("開始播放") || text.Contains("空白鍵"))
            {
                result = BBSMode.PressAnyKey;
            }
            else if (text.Contains("編輯文章"))
            {
                result = BBSMode.Editor;
            }
            else if (text.Contains("看板列表") || text.Contains("選擇看板"))
            {
                result = BBSMode.BoardList;
            }
            else if (text.Contains("文章選讀") || text.Contains("【板主:") || text.Contains("徵求中"))
            {
                result = BBSMode.ArticleList;
            }
            else if (text.Contains("郵件選單") || text.Contains("鴻雁往返"))
            {
                result = BBSMode.MailList;
            }
            else if (text.Contains("呼叫") || text.Contains("星期") || text.Contains("電子郵件") || text.Contains("聊天說話")
                || text.Contains("個人") || text.Contains("工具程式") || text.Contains("熱門話題") || text.Contains("使用者統計")
                || text.Contains("網路遊樂場") || text.Contains("Ｐtt量販店") || text.Contains("Ｐtt棋院")
                || text.Contains("名單編輯") || text.Contains("主功能表"))
            {
                result = BBSMode.MainList;
            }
            else if (text.Contains("功能鍵") || text.Contains("精華文章"))
            {
                result = BBSMode.Essence;
            }
            else if (text.Contains("分類看板"))
            {
                result = BBSMode.ClassBoard;
            }
            else if (text.Contains("瀏覽") || text.Contains("作者"))
            {
                result = BBSMode.ArticleBrowse;
            }
            else
            {
                result = BBSMode.Other;
            }
            return result;
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
        
        private static bool isChineseWord(string p)
        {
            if (p[p.Length - 1] > 0x3040)
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
            if (MainPage.connection.autoLogin && LoginScreen)
            {
                await MainPage.connection.sendCommand(MainPage.connection.account + "\r" + MainPage.connection.password + "\r");
                MainPage.connection.autoLogin = false;
                LoginScreen = false;
            }
            //top.Children.Clear();
            //list.Items.Clear();
            //bottom.Children.Clear();
            //operationBoard.Children.Clear();
            if (currentMode == BBSMode.MainList || currentMode == BBSMode.ClassBoard || currentMode == BBSMode.BoardList
                || currentMode == BBSMode.Essence || currentMode == BBSMode.ArticleList || currentMode == BBSMode.MailList)
            {
                int topCount = 0;
                int bottomCount = 0;
                bool articleTitle = false;
                foreach (var line in currPage)
                {
                    Canvas lineCanvas = getCanvas(list.Width, _fontSize);
                    //if (currentMode == BBSMode.MainList && line.No < 12)
                    //{
                    //    line.UniqueId = "";
                    //}
                    //if (currentMode == BBSMode.ArticleList && line.No < 2)
                    //{
                    //    line.UniqueId = "";
                    //}

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
            //operationBoard.Children.Clear();
            //PTTCanvas.Children.Clear();
            string wholeText = "";
            foreach (var line in currPage)
            {
                wholeText += line.Text + "\n";
                foreach (var block in line.Blocks)
                {
                    saveToCanvas(PTTCanvas, block, block.TopPoint);
                }
            }
            if (currentMode == BBSMode.ArticleBrowse || currentMode == BBSMode.PressAnyKey)
            {
                TextBlock tb = getClipTextBlock(Colors.Transparent);
                generateHyperlink(wholeText, tb, Colors.Transparent);
                operationBoard.Children.Add(tb);
            }
            //Debug.WriteLine("children count = {0}", PTTCanvas.Children.Count);
        }

        public event EventHandler LinesPropChanged;
        public virtual void onLinesChanged(EventArgs e)
        {
            if (LinesPropChanged != null)
            {
                LinesPropChanged(this, e);
            }
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
            if (r.Text[0] > 0x3040)
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
                    catch (Exception)
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
            if (MainPage.connection.autoLogin && LoginScreen)
            {
                await MainPage.connection.sendCommand(MainPage.connection.account + "\r" + MainPage.connection.password + "\r");
                MainPage.connection.autoLogin = false;
                LoginScreen = false;
            }
            ProcessCanvas(PTTCanvas, currPage, operationBoard);
        }

        //internal static void articleAddToCurrPage()
        //{
        //    if (currentMode == BBSMode.ArticleBrowse)
        //    {
        //        int start = Lines.FindLastIndex(x => x.Text == ToShowLines[ToShowLines.Count - 1].Text);
        //        start++;
        //        int last = Lines.Count - 1;
        //        if (start == -1) //can't find in Lines
        //        {
        //            start = 0;
        //        }
        //        if (!Lines[lines.Count - 1].Text.Contains("100%"))
        //        {
        //            last--;
        //        }
        //        for (int index = start; index < last; index++)
        //        {
        //            ToShowLines.Add(Lines[index]);
        //        }
        //    }
        //}

        public static async Task upOrDown(int count)
        {
            if (count < 0) //go down
            {
                for (int i = 0; i < -count; i++)
                {
                    await MainPage.connection.sendCommand(new byte[] { 27, 91, 66 });
                }
            }
            else //go up
            {
                for (int i = 0; i < count; i++)
                {
                    await MainPage.connection.sendCommand(new byte[] { 27, 91, 65 });
                }
            }
        }
    }
}
