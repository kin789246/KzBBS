using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace KzBBS
{
    class TelnetANSIParserCanvas
    {
        enum Telnet : byte
        {
            //escape
            IAC = 255,

            //commands
            SE = 240,
            NoOperation = 241,
            DataMark = 242,
            Break = 243,
            InterruptProcess = 244,
            AbortOutput = 245,
            AreYouThere = 246,
            EraseCharacter = 247,
            EraseLine = 248,
            GoAhead = 249,
            SB = 250,

            //negotiation
            WILL = 251,
            WONT = 252,
            DO = 253,
            DONT = 254,

            //options (common)
            SuppressGoAhead = 3,
            Status = 5,
            Echo = 1,
            TimingMark = 6,
            TerminalType = 24,
            TerminalSpeed = 32,
            RemoteFlowControl = 33,
            LineMode = 34,
            EnvironmentVariables = 36,
            NAWS = 31,

            //options (MUD-specific)
            /*MSDP = 69,
            MXP = 91,
            MCCP1 = 85,
            MCCP2 = 86,
            MSP = 90*/
        };

        private static int[] analyzeAnsiCode(List<byte> AnsiCode)
        {
            int ANSIIndex = 0;
            int[] ANSI = {255,255,255,255,255,255};
            string temp = "";
            
            if (AnsiCode[0] == 59)
            {
                ANSI[ANSIIndex] = 0;
                ANSIIndex++;
            }
            
            for(int i=0; i<AnsiCode.Count; i++)
            {
                if(AnsiCode[i] != 59)
                {
                    temp = temp + (char)AnsiCode[i];
                }
                else
                {
                    if (string.IsNullOrEmpty(temp)) continue;
                    ANSI[ANSIIndex] = Convert.ToInt32(temp);
                    temp = "";
                    ANSIIndex++;
                }
            }
            if (temp != "")
            { ANSI[ANSIIndex] = Convert.ToInt32(temp); }
            return ANSI;
        }

        public static Color nBlack = Color.FromArgb(255, 0, 0, 0);
        public static Color nRed = Color.FromArgb(255, 128, 0, 0);
        public static Color nGreen = Color.FromArgb(255, 0, 128, 0);
        public static Color nYellow = Color.FromArgb(255, 128, 128, 0);
        public static Color nBlue = Color.FromArgb(255, 0, 0, 128);
        public static Color nMagenta = Color.FromArgb(255, 128, 0, 128);
        public static Color nCyan = Color.FromArgb(255, 0, 128, 128);
        public static Color nGray = Color.FromArgb(255, 192, 192, 192);
        public static Color bDarkgray = Color.FromArgb(255, 128, 128, 128);
        public static Color bRed = Color.FromArgb(255, 255, 0, 0);
        public static Color bGreen = Color.FromArgb(255, 0, 255, 0);
        public static Color bYellow = Color.FromArgb(255, 255, 255, 0);
        public static Color bBlue = Color.FromArgb(255, 0, 0, 255);
        public static Color bMagenta = Color.FromArgb(255, 255, 0, 255);
        public static Color bCyan = Color.FromArgb(255, 0, 255, 255);
        public static Color bWhite = Color.FromArgb(255, 255, 255, 255);

        private static void analyzeWordAttr(int[] ansiCode)
        {
            int[] arrangedAnsi = { 255, 255};
            for (int i = 0; i < ansiCode.Length; i++)
            {
                if (ansiCode[i] == 0)
                {
                    fg = nGray;
                    bg = nBlack;
                    isBlinking = false;
                    bright = false;
                }
                if(ansiCode[i] == 1)
                {
                    bright = true;
                }
                if(ansiCode[i] == 5)
                {
                    isBlinking = true;
                }
                if (ansiCode[i] == 7)
                {
                    Color temp;
                    temp = fg;
                    fg = bg;
                    bg = temp;
                }
                if (ansiCode[i] / 10 == 3)
                {
                    arrangedAnsi[0] = ansiCode[i];
                }
                if(ansiCode[i] /10 == 4)
                {
                    arrangedAnsi[1] = ansiCode[i];
                }
            }
            #region Set foreground and background Color

            if (bright)
            {
                if (arrangedAnsi[0] == 30) fg = bDarkgray;
                else if (arrangedAnsi[0] == 31) fg = bRed;
                else if (arrangedAnsi[0] == 32) fg = bGreen;
                else if (arrangedAnsi[0] == 33) fg = bYellow;
                else if (arrangedAnsi[0] == 34) fg = bBlue;
                else if (arrangedAnsi[0] == 35) fg = bMagenta;
                else if (arrangedAnsi[0] == 36) fg = bCyan;
                else if (arrangedAnsi[0] == 37) fg = bWhite;

                else if (fg == nBlack) fg = bDarkgray;
                else if (fg == nRed) fg = bRed;
                else if (fg == nGreen) fg = bGreen;
                else if (fg == nYellow) fg = bYellow;
                else if (fg == nBlue) fg = bBlue;
                else if (fg == nMagenta) fg = bMagenta;
                else if (fg == nCyan) fg = bCyan;
                else if (fg == nGray) fg = bWhite;              
            }
            else
            {
                if (arrangedAnsi[0] == 30) fg = nBlack;
                else if (arrangedAnsi[0] == 31) fg = nRed;
                else if (arrangedAnsi[0] == 32) fg = nGreen;
                else if (arrangedAnsi[0] == 33) fg = nYellow;
                else if (arrangedAnsi[0] == 34) fg = nBlue;
                else if (arrangedAnsi[0] == 35) fg = nMagenta;
                else if (arrangedAnsi[0] == 36) fg = nCyan;
                else if (arrangedAnsi[0] == 37) fg = nGray;
            }
            if (arrangedAnsi[1] == 40) bg = nBlack;
            else if (arrangedAnsi[1] == 41) bg = nRed;
            else if (arrangedAnsi[1] == 42) bg = nGreen;
            else if (arrangedAnsi[1] == 43) bg = nYellow;
            else if (arrangedAnsi[1] == 44) bg = nBlue;
            else if (arrangedAnsi[1] == 45) bg = nMagenta;
            else if (arrangedAnsi[1] == 46) bg = nCyan;
            else if (arrangedAnsi[1] == 47) bg = nGray;
            #endregion
        }

        public static void resetAllSetting()
        {
            curPos = new Point(0, 0);
            pointTo = new TelnetData();
            AnsiCode = new List<byte>();
            forAppliedAnsi = new int[] { 37, 40, 255, 255, 255, 255 };
            lastAttr = new AnsiAttr();
            savedPos = new Point(0, 0);
            fg = nGray;
            bg = nBlack;
            isBlinking = false;
            bright = false;
            IACNego = false;
            highByte = 0;
            beforeSus = new Point(0, 0);
            for (int i = 0; i < BBSPage.Length; i++)
            { BBSPage[i] = new List<TelnetData>(); }
        }

        public static List<TelnetData>[] BBSPage = new List<TelnetData>[24];
        public static Point curPos = new Point(0, 0);
        static Point beforeMoveCursor = new Point(24,0);
        //public static double fontSize = 30;
        //public static string fontFamily = PTTDisplay._fontFamily;
        private static List<byte> AnsiCode = new List<byte>();
        private static bool AnsiZone = false;
        private static int[] forAppliedAnsi = { 37, 40, 255, 255, 255, 255 };
        private static AnsiAttr lastAttr = new AnsiAttr();
        private static Point savedPos = new Point(0, 0);
        public static Color fg = nGray, bg = nBlack;
        static bool isBlinking = false;
        static bool bright = false;
        static bool IACNego = false;
        static byte highByte = 0;
        public static TelnetData pointTo = new TelnetData();
        public static Point beforeSus = new Point(0, 0);

        public static void HandleAnsiESC(byte[] withoutIAC, Canvas BBSCanvas)
        {
            #region debug 
            //StringBuilder rdataString = new StringBuilder();
            //for (int i = 0; i < withoutIAC.Length; i++)
            //{
            //    rdataString.Append(withoutIAC[i].ToString() + " ");
            //    if (withoutIAC[i] == 10)
            //    { rdataString.Append("\n"); }
            //}
            //Debug.WriteLine(rdataString.ToString());

            //List<byte> noCmd = new List<byte>();
            //noCmd = TelnetParser.HandleAndRemoveTelnetBytes(withoutIAC.ToList<byte>());
            //string removeIAC = Big5Util.ToUni(noCmd.ToArray());
            //if (!string.IsNullOrEmpty(removeIAC))
            //{
            //    string printIt = "";
            //    foreach (char byteword in removeIAC)
            //    {
            //        if (byteword == '\n')
            //        {
            //            printIt += "\\n\n";
            //        }
            //        else if (byteword == '\r')
            //        { printIt += "\\r"; }
            //        else if (byteword == '\b')
            //        {
            //            printIt += "\\b";
            //        }
            //        else
            //        {
            //            printIt += byteword;
            //        }
            //    }
            //    printIt += "-received.";
            //    Debug.WriteLine(printIt);
            //}
            //////////////////////////////////

            #endregion

            beforeSus = curPos;
            for(int i=0; i<BBSPage.Length; i++)
            {
                if (BBSPage[i] == null)
                    BBSPage[i] = new List<TelnetData>();
            }

            int currentIndex = 0;
            //start handling ANSI ESC Sequences
            while (currentIndex < withoutIAC.Length)
            {
                if (withoutIAC[currentIndex] == 27) //start ESC sequences
                {
                    AnsiZone = true;
                    currentIndex++;
                }
                //if last time ended with ESC(27), this time start with [ (91)
                else if (AnsiZone == true && withoutIAC[currentIndex] == 91)
                {
                    currentIndex++;
                }

                    //handle IAC code
                else if (withoutIAC[currentIndex] == (byte)Telnet.IAC)
                {
                    if (currentIndex + 1 < withoutIAC.Length && withoutIAC[currentIndex + 1] == 255)
                    {
                        //tempWords.Add(255);
                        currentIndex += 2;
                    }
                    else if (currentIndex + 1 < withoutIAC.Length && (
                        withoutIAC[currentIndex + 1] == (byte)Telnet.DO || withoutIAC[currentIndex + 1] == (byte)Telnet.DONT
                        || withoutIAC[currentIndex + 1] == (byte)Telnet.WILL || withoutIAC[currentIndex + 1] == (byte)Telnet.WONT))
                    {
                        currentIndex += 3;
                    }
                    else if (currentIndex + 1 < withoutIAC.Length && withoutIAC[currentIndex + 1] == (byte)Telnet.SB)
                    {
                        IACNego = true;
                        currentIndex += 2;
                    }
                    else if (IACNego == true && currentIndex + 1 < withoutIAC.Length
                        && withoutIAC[currentIndex + 1] == (byte)Telnet.SE)
                    {
                        IACNego = false;
                        currentIndex += 2;
                    }
                    else if(currentIndex + 1 < withoutIAC.Length && withoutIAC[currentIndex + 1] == (byte)Telnet.GoAhead)
                    {
                        currentIndex += 2;
                    }
                }
                else if (AnsiZone == true && withoutIAC[currentIndex] >= 64 && withoutIAC[currentIndex] <= 127)
                //end of Ansi zone
                {
                    beforeMoveCursor = new Point(curPos.X, curPos.Y - 1);
                    if (withoutIAC[currentIndex] == 72) // *[ H
                    {
                        if (pointTo.Text != "")
                        {
                            findReplace(BBSCanvas, pointTo);
                            //BBSPage[(int)curPos.X].Add(pointTo);
                            addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                            pointTo = new TelnetData();
                        }
                        if (AnsiCode.Count == 0) // move cursor to (0,0)
                        {
                            curPos.X = 0;
                            curPos.Y = 0;
                        }
                        else  // move cursor to *[ Y; X H
                        {
                            int[] tempAnsi = new int[6];
                            tempAnsi = analyzeAnsiCode(AnsiCode);
                            if (tempAnsi[1] == 255)
                            {
                                curPos.X = 0;
                                curPos.Y = 0;
                            }
                            else
                            {
                                curPos.X = tempAnsi[0] - 1;
                                curPos.Y = tempAnsi[1] - 1;
                            }
                        }
                        
                        pointTo.setData(fg, bg, isBlinking, curPos);
                    }
                    else if (withoutIAC[currentIndex] == 74) // *[ J
                    {
                        beforeMoveCursor = new Point(curPos.X, curPos.Y - 1);
                        int[] tempAnsi = new int[6];
                        tempAnsi = analyzeAnsiCode(AnsiCode);
                        //clear entire screen = delete all stored words
                        if (tempAnsi[0] == 2)
                        {
                            curPos.Y = 0; curPos.X = 0;
                            //BBSCanvas.Children.Clear();
                            for (int i = 0; i < 24; i++)
                            {
                                foreach (TelnetData block in BBSPage[i])
                                {
                                    foreach (UIElement ui in block.BBSUI)
                                    { BBSCanvas.Children.Remove(ui); }
                                }
                            }
                            for(int i=0; i<BBSPage.Length; i++)
                            { BBSPage[i].Clear(); }
                        }
                    }
                    else if (withoutIAC[currentIndex] == 75) // *[ K
                    {
                        if (pointTo.Text != "")
                        {
                            findReplace(BBSCanvas, pointTo);
                            //BBSPage[(int)curPos.X].Add(pointTo);
                            addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                            pointTo = new TelnetData();
                        }
                        pointTo.setData(fg, bg, isBlinking, curPos);
                        escapeK(BBSCanvas);
                    }
                    else if (withoutIAC[currentIndex] == 109) // *[ m
                    {
                        if (AnsiCode.Count != 0)
                        {
                            forAppliedAnsi = analyzeAnsiCode(AnsiCode);
                        }
                        else
                        {
                            int[] resetColor = { 0, 255, 255, 255, 255, 255 };
                            forAppliedAnsi = resetColor;
                        }
                        analyzeWordAttr(forAppliedAnsi);
                        if (pointTo.Text != "")
                        {
                            findReplace(BBSCanvas, pointTo);
                            //BBSPage[(int)curPos.X].Add(pointTo);
                            addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                            pointTo = new TelnetData();
                        }
                        pointTo.setData(fg, bg, isBlinking, curPos);
                    }
                    else if (withoutIAC[currentIndex] == 77) // *[ M scroll up one line
                    {
                        if (AnsiCode.Count == 0)
                        {
                            foreach(TelnetData block in BBSPage[BBSPage.Length-1])
                            { 
                                foreach (UIElement ui in block.BBSUI)
                                { BBSCanvas.Children.Remove(ui); }
                            }

                            for (int i = BBSPage.Length - 1; i > 0; i--)
                            { 
                                BBSPage[i] = BBSPage[i - 1];
                                foreach(TelnetData block in BBSPage[i])
                                { 
                                    foreach (UIElement ui in block.BBSUI)
                                    { BBSCanvas.Children.Remove(ui); }
                                    block.BBSUI.Clear();
                                }
                            }
                            BBSPage[0] = new List<TelnetData>();
                        }
                    }
                    else if (withoutIAC[currentIndex] == 115) //*[s save cursor
                    {
                        savedPos = curPos;
                    }
                    else if (withoutIAC[currentIndex] == 117) //*[u load cursor
                    {
                        curPos = savedPos;
                    }
                    else if (withoutIAC[currentIndex] == 65) //*[nA move cursor n up
                    {
                        beforeMoveCursor = new Point(curPos.X, curPos.Y - 1);
                        if (AnsiCode.Count == 0) //*[A
                        {
                            curPos.X--;
                            if (curPos.X < 0) curPos.X = 0;
                        }
                        else
                        {
                            int[] tempAnsi = new int[6];
                            tempAnsi = analyzeAnsiCode(AnsiCode);
                            curPos.X -= tempAnsi[0];
                            if (curPos.X < 0) curPos.X = 0;
                        }
                    }
                    else if (withoutIAC[currentIndex] == 66) //*[nB move cursor n down
                    {
                        beforeMoveCursor = new Point(curPos.X, curPos.Y - 1);
                        if (AnsiCode.Count == 0) //*[B
                        {
                            curPos.X++;
                            if (curPos.X >= 24) curPos.X = 23;
                        }
                        else
                        {
                            int[] tempAnsi = new int[6];
                            tempAnsi = analyzeAnsiCode(AnsiCode);
                            curPos.X += tempAnsi[0];
                            if (curPos.X >= 24) curPos.X = 23;
                        }
                    }
                    else if (withoutIAC[currentIndex] == 67) //*[nC move cursor n right
                    {
                        beforeMoveCursor = new Point(curPos.X, curPos.Y - 1);
                        if (AnsiCode.Count == 0) //*[C
                        {
                            curPos.Y++;
                            if (curPos.Y >= 80) curPos.Y = 79;
                        }
                        else
                        {
                            int[] tempAnsi = new int[6];
                            tempAnsi = analyzeAnsiCode(AnsiCode);
                            curPos.Y += tempAnsi[0];
                            if (curPos.Y >= 80) curPos.Y = 79;
                        }
                    }
                    else if (withoutIAC[currentIndex] == 68) //*[nD move cursor n left
                    {
                        beforeMoveCursor = new Point(curPos.X, curPos.Y - 1);
                        if (AnsiCode.Count == 0) //*[D
                        {
                            curPos.Y--;
                            if (curPos.Y < 0) curPos.Y = 0;
                        }
                        else
                        {
                            int[] tempAnsi = new int[6];
                            tempAnsi = analyzeAnsiCode(AnsiCode);
                            curPos.Y -= tempAnsi[0];
                            if (curPos.Y < 0) curPos.Y = 0;
                        }
                    }
                    else
                    {
                        Debug.WriteLine(withoutIAC[currentIndex]);
                    }
                    currentIndex++;
                    AnsiZone = false;
                    AnsiCode.Clear();
                }
                else
                {
                    if (AnsiZone == true && IACNego == false)
                    {
                        AnsiCode.Add(withoutIAC[currentIndex++]);
                    }
                    else if (IACNego == false && AnsiZone == false)
                    {
                        //store word
                        if (highByte != 0) //dual color low-byte
                        {
                            if (withoutIAC[currentIndex] > 0x3F)
                            {
                                string big5Word = Big5Util.ToUni(new byte[] { highByte, withoutIAC[currentIndex] });
                                pointTo.setData(lastAttr.fgColor, lastAttr.bgColor, lastAttr.isBlinking,
                                new Point(curPos.X, curPos.Y - 1));
                                pointTo.Text = big5Word;
                                pointTo.Count = 2;
                                pointTo.DualColor = true;
                                findReplace(BBSCanvas, pointTo);
                                //BBSPage[(int)curPos.X].Add(pointTo);
                                addToBBSPage(BBSPage[(int)curPos.X], pointTo);

                                pointTo = new TelnetData();
                                pointTo.setData(fg, bg, isBlinking, curPos);
                                pointTo.Text = big5Word;
                                pointTo.Count = 2;
                                pointTo.DualColor = true;
                                pointTo.Position.Y--;
                                //findReplace(BBSCanvas, pointTo);
                                //BBSPage[(int)curPos.X].Add(pointTo);
                                addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                                highByte = 0;
                                curPos.Y++;

                                pointTo = new TelnetData();
                                pointTo.setData(fg, bg, isBlinking, curPos);

                                currentIndex++;
                                continue;
                            }
                            else
                            {
                                pointTo.Text += "?";
                                pointTo.Count++;
                                pointTo.setData(lastAttr.fgColor, lastAttr.bgColor, lastAttr.isBlinking,
                                    beforeMoveCursor);
                                findReplace(BBSCanvas, pointTo);
                                //BBSPage[(int)curPos.X].Add(pointTo);
                                addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                                
                                pointTo = new TelnetData();
                                pointTo.setData(fg, bg, isBlinking, curPos);
                                highByte = 0;
                            }
                        }
                        if (withoutIAC[currentIndex] < 128) //ASCII
                        {                            
                            storeASCII(BBSCanvas, withoutIAC[currentIndex]);
                            currentIndex++;
                        }
                        else if (currentIndex + 1 < withoutIAC.Length)
                        {
                            if (withoutIAC[currentIndex + 1] != 27) //Big5
                            {
                                if (withoutIAC[currentIndex + 1] > 0x3F)
                                {
                                    pointTo.Text += Big5Util.ToUni(new byte[] { withoutIAC[currentIndex], withoutIAC[currentIndex + 1] });
                                    pointTo.Count += 2;
                                    curPos.Y += 2;
                                    currentIndex += 2;
                                }
                                else
                                {
                                    pointTo.Text += "?";
                                    pointTo.Count++;
                                    curPos.Y++;
                                    currentIndex++;
                                }
                            }
                            else //Dual color
                            {
                                if(pointTo.Text != "")
                                {
                                    findReplace(BBSCanvas, pointTo);
                                    //BBSPage[(int)curPos.X].Add(pointTo);
                                    addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                                    pointTo = new TelnetData();
                                }
                                pointTo.setData(fg, bg, isBlinking, curPos);
                                highByte = withoutIAC[currentIndex];
                                lastAttr.setAtt(fg, bg, isBlinking);
                                curPos.Y++;
                                currentIndex++;
                            }
                        }
                        else //if the 79th byte > 128
                        {
                            if (pointTo.Text != "")
                            {
                                findReplace(BBSCanvas, pointTo);
                                //BBSPage[(int)curPos.X].Add(pointTo);
                                addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                                pointTo = new TelnetData();

                            }
                            pointTo.setData(fg, bg, isBlinking, curPos);
                            highByte = withoutIAC[currentIndex];
                            lastAttr.setAtt(fg, bg, isBlinking);
                            curPos.Y++;

                            currentIndex++;
                        }
                    }
                    else
                    { currentIndex++; }
                }
            }
            //store the last part
            if (pointTo.Text != "")
            {
                findReplace(BBSCanvas, pointTo);
                //BBSPage[(int)curPos.X].Add(pointTo);
                addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                pointTo = new TelnetData();
            }
            pointTo.setData(fg, bg, isBlinking, curPos);
        } 

        private static void storeASCII(Canvas BBSCanvas, byte word)
        {
            if (word == 13) //for "\r"
            {
                curPos.Y = 0;
                if (pointTo.Text != "")
                {
                    findReplace(BBSCanvas, pointTo);
                    //BBSPage[(int)curPos.X].Add(pointTo);
                    addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                    pointTo = new TelnetData();
                }
                pointTo.setData(fg, bg, isBlinking, curPos);
            }
            else if (word == 10) //for "\n"
            {
                if (pointTo.Text != "")
                {
                    findReplace(BBSCanvas, pointTo);
                    //BBSPage[(int)curPos.X].Add(pointTo);
                    addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                    pointTo = new TelnetData();
                }
                curPos.X++;
                pointTo.setData(fg, bg, isBlinking, curPos);

                if (curPos.X == 24) //scroll down one line
                {
                    curPos.X = 23;
                    pointTo.Position.X = 23;
                    foreach (TelnetData block in BBSPage[0])
                    { 
                        foreach (UIElement ui in block.BBSUI)
                        { BBSCanvas.Children.Remove(ui); }
                        block.BBSUI.Clear();
                    }

                    for (int i = 0; i < BBSPage.Length-1; i++)
                    {
                        BBSPage[i] = BBSPage[i + 1];
                        foreach (TelnetData block in BBSPage[i])
                        {
                            foreach (UIElement ui in block.BBSUI)
                            { BBSCanvas.Children.Remove(ui); }
                            block.BBSUI.Clear();
                        }
                    }
                    BBSPage[BBSPage.Length - 1] = new List<TelnetData>();
                    
                }
            }
            else if (word == 8) //back space "\b"
            {
                if (pointTo.Text != "")
                {
                    findReplace(BBSCanvas, pointTo);
                    //BBSPage[(int)curPos.X].Add(pointTo);
                    addToBBSPage(BBSPage[(int)curPos.X], pointTo);
                    pointTo = new TelnetData();
                }
                curPos.Y--;
                pointTo.setData(fg, bg, isBlinking, curPos);
            }
            else if (word == 0)
            {
                Debug.WriteLine(word);
            }
            else if (word == 7) //bel code "\a"
            {
                Debug.WriteLine(word);
            }
            else if (word == 9)
            {
                Debug.WriteLine(word);
            }
            else if (word == 11)
            {
                Debug.WriteLine(word);
            }
            else if (word == 12)
            {
                Debug.WriteLine(word);
            }
            else
            {
                if (word == 32) //non-breaking space
                    word = 0xA0;
                pointTo.Text += (char)word;
                pointTo.Count++;
                curPos.Y++;
            }
        }

        private static void escapeK(Canvas BBSCanvas)
        {
            int line = (int)curPos.X;

            if (BBSPage[line].Count == 0)
                return;
            //TelnetData toSave;
            List<TelnetData> toRemove = new List<TelnetData>();
            foreach (TelnetData block in BBSPage[line])
            {
                //if (block.Position.Y + block.Count - 1 < curPos.Y) //reserve
                //{
                //    toAdd.Add(block);
                //    block.BBSBackground = null;
                //}
                if (curPos.Y <= block.Position.Y)
                {
                    toRemove.Add(block);
                    foreach (UIElement ui in block.BBSUI)
                    { BBSCanvas.Children.Remove(ui); }
                    block.BBSUI.Clear();
                    continue;
                }
                if (block.Position.Y < curPos.Y && curPos.Y <= block.Position.Y + block.Count - 1)
                {
                    block.Text = CropText(block.Text, 0, (int)curPos.Y - (int)block.Position.Y - 1);
                    block.Count = (int)curPos.Y - (int)block.Position.Y;
                    //toAdd.Add(block);
                    foreach (UIElement ui in block.BBSUI)
                    { BBSCanvas.Children.Remove(ui); }
                    block.BBSUI.Clear();
                    continue;
                }
            }
            //BBSPage[line].Clear();
            foreach (TelnetData bk in toRemove)
            { BBSPage[line].Remove(bk); }
            toRemove.Clear();
        }

        private static void findReplace(Canvas BBSCanvas, TelnetData toInsert)
        {
            int line = (int)curPos.X;
            if (BBSPage[line].Count == 0 || toInsert.Text == "")
                return;
            TelnetData toSave;
            int count;
            int curY, blockY;
            string text;
            curY = (int)toInsert.Position.Y;
            List<TelnetData> toRemove = new List<TelnetData>();
            List<TelnetData> toAdd = new List<TelnetData>();
            foreach (TelnetData block in BBSPage[line])
            { 
                blockY = (int)block.Position.Y;
                //if (blockY + block.Count - 1 < curY) //reserve
                //{
                //    toAdd.Add(block);
                //    block.BBSBackground = null;
                //}
                //if (curY + toInsert.Count - 1 < blockY) //reserve
                //{
                //    toAdd.Add(block);
                //    block.BBSBackground = null;
                //}
                if (blockY < curY && curY <= blockY + block.Count - 1 && blockY + block.Count - 1 <= curY + toInsert.Count - 1)
                {
                    block.Text = CropText(block.Text, 0, curY - blockY - 1);
                    block.Count = curY - blockY;
                    //toAdd.Add(block);
                    foreach (UIElement ui in block.BBSUI)
                    { BBSCanvas.Children.Remove(ui); }
                    block.BBSUI.Clear();
                    continue;
                }
                if (curY <= blockY && blockY <= curY + toInsert.Count - 1 && curY + toInsert.Count - 1 < blockY + block.Count - 1)
                {
                    block.Text = CropText(block.Text, curY + toInsert.Count - blockY, block.Count - 1);
                    block.Count = blockY + block.Count - curY - toInsert.Count;
                    //toSave.Text = text;
                    //toSave.Count = count;
                    block.Position = new Point(line, curY + toInsert.Count);
                    //toAdd.Add(block);
                    
                    foreach (UIElement ui in block.BBSUI)
                    { BBSCanvas.Children.Remove(ui); }
                    block.BBSUI.Clear();
                    continue;
                }
                if (blockY < curY && curY + toInsert.Count - 1 < blockY + block.Count - 1)
                {
                    toSave = new TelnetData();
                    toSave.cloneFrom(block);
                    text = CropText(block.Text, 0, curY - blockY - 1);
                    count = curY - blockY;
                    toSave.Text = text;
                    toSave.Count = count;
                    toAdd.Add(toSave);
                    block.Text = CropText(block.Text, curY + toInsert.Count - blockY, block.Count - 1);
                    block.Count = blockY + block.Count - curY - toInsert.Count;
                    block.Position = new Point(line, curY + toInsert.Count);
                    //toAdd.Add(block);
                    foreach (UIElement ui in block.BBSUI)
                    { BBSCanvas.Children.Remove(ui); }
                    block.BBSUI.Clear();
                    continue;
                }
                if (curY <= blockY && blockY + block.Count - 1 <= curY + toInsert.Count - 1)
                {
                    toRemove.Add(block);
                    foreach (UIElement ui in block.BBSUI)
                    { BBSCanvas.Children.Remove(ui); }
                    block.BBSUI.Clear();
                    continue;
                }
            }
            //BBSPage[line].Clear();
            foreach (TelnetData bk in toRemove)
            { BBSPage[line].Remove(bk); }
            foreach (TelnetData bk in toAdd)
            { BBSPage[line].Add(bk); }
            toRemove.Clear();
            toAdd.Clear();
        }

        public static int transferQty(string source)
        {
            int count = 0;
            foreach(char ch in source)
            {
                if(ch < 256)
                { count++; }
                else
                { count += 2; }
            }
            return count;
        }

        public static string CropText(string source, int start, int end)
        {
            StringBuilder result = new StringBuilder();
            int current = 0, index = 0;
            while(index < source.Length)
            {
                if (current >= start)
                { result.Append(source[index]); }

                if(source[index] < 256)
                { current++; }
                else
                { 
                    current += 2;
                    if (current - 1 == start)
                        result.Append((char)0xA0);
                }

                if (current > end)
                    break;
                
                index++;
            }
            return result.ToString();
        }

        private static void addToBBSPage(List<TelnetData> list, TelnetData pointTo)
        {
            TelnetData toSave = new TelnetData();
            StringBuilder charList = new StringBuilder();
            bool isAnsi = true; //less than 0x3040
            bool lastIsAnsi = true;
            double lastY = pointTo.Position.Y;

            foreach(char ch in pointTo.Text)
            {
                if(ch > 0x3040)
                { isAnsi = false; }
                else
                { isAnsi = true; }

                if (lastIsAnsi != isAnsi && charList.Length != 0)
                {
                    toSave.cloneFrom(pointTo);
                    toSave.Text = charList.ToString();
                    toSave.Count = transferQty(toSave.Text);
                    toSave.Position = new Point(pointTo.Position.X, lastY);
                    lastY += toSave.Count;
                    list.Add(toSave);
                    toSave = new TelnetData();
                    charList.Clear();
                }
                charList.Append(ch); 
                lastIsAnsi = isAnsi;
            }
            //save the rest text
            toSave.cloneFrom(pointTo);
            toSave.Text = charList.ToString();
            toSave.Count = transferQty(toSave.Text);
            toSave.Position = new Point(pointTo.Position.X, lastY);
            list.Add(toSave);
        }
    }
}
