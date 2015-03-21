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
    class TelnetANSIParser
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
            //pointTo = new TelnetData();
            AnsiCode = new List<byte>();
            forAppliedAnsi = new int[] { 37, 40, 255, 255, 255, 255 };
            lastAttr = new AnsiAttr();
            savedPos = new Point(0, 0);
            fg = nGray;
            bg = nBlack;
            isBlinking = false;
            isDualColor = false;
            bright = false;
            IACNego = false;
            highByte = 0;
            beforeSus = new Point(0, 0);
            for (int i = 0; i < ROW; i++)
            {
                for (int j = 0; j < COL; j++)
                {
                    if (BBSPage[i, j] == null)
                    {
                        BBSPage[i, j] = new TelnetData();
                    }
                }
            }
            //startPos = new Point(0, 0);
        }

        const int ROW = 24;
        const int COL = 80;
        public static TelnetData[,] BBSPage = new TelnetData[ROW,COL];
        public static Point curPos = new Point(0, 0);
        static Point beforeMoveCursor = new Point(24,0);
        private static List<byte> AnsiCode = new List<byte>();
        private static bool AnsiZone = false;
        private static int[] forAppliedAnsi = { 37, 40, 255, 255, 255, 255 };
        private static AnsiAttr lastAttr = new AnsiAttr();
        private static Point savedPos = new Point(0, 0);
        public static Color fg = nGray, bg = nBlack;
        static bool isBlinking = false;
        static bool isDualColor = false;
        static bool bright = false;
        static bool IACNego = false;
        static byte highByte = 0;
        static Point hiPosition = new Point(0, 0);
        //public static TelnetData pointTo = new TelnetData();
        public static Point beforeSus = new Point(0, 0);

        public static void HandleAnsiESC(byte[] rawdata)
        {
            #region debug 
            ////raw bytes
            //StringBuilder rdataString = new StringBuilder();
            //for (int i = 0; i < rawdata.Length; i++)
            //{
            //    rdataString.Append(rawdata[i].ToString() + " ");
            //    if (rawdata[i] == 10)
            //    { rdataString.Append("\n"); }
            //}
            //Debug.WriteLine(rdataString.ToString());
            //raw text
            //List<byte> noCmd = new List<byte>();
            //noCmd = TelnetParser.HandleAndRemoveTelnetBytes(rawdata.ToList<byte>());
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
            ////////////////////////////////////

            #endregion

            beforeSus = curPos;
            for (int i = 0; i < ROW; i++)
            {
                for (int j = 0; j < COL; j++)
                {
                    if (BBSPage[i, j] == null)
                    {
                        BBSPage[i, j] = new TelnetData();
                    }
                }
            }

            int currentIndex = 0;
            //start handling ANSI ESC Sequences
            while (currentIndex < rawdata.Length)
            {
                if (rawdata[currentIndex] == 27) //start ESC sequences
                {
                    AnsiZone = true;
                    currentIndex++;
                }
                //if last time ended with ESC(27), this time start with [ (91)
                else if (AnsiZone == true && rawdata[currentIndex] == 91)
                {
                    currentIndex++;
                }

                    //handle IAC code
                else if (rawdata[currentIndex] == (byte)Telnet.IAC)
                {
                    if (currentIndex + 1 < rawdata.Length && rawdata[currentIndex + 1] == 255)
                    {
                        //tempWords.Add(255);
                        currentIndex += 2;
                    }
                    else if (currentIndex + 1 < rawdata.Length && (
                        rawdata[currentIndex + 1] == (byte)Telnet.DO || rawdata[currentIndex + 1] == (byte)Telnet.DONT
                        || rawdata[currentIndex + 1] == (byte)Telnet.WILL || rawdata[currentIndex + 1] == (byte)Telnet.WONT))
                    {
                        currentIndex += 3;
                    }
                    else if (currentIndex + 1 < rawdata.Length && rawdata[currentIndex + 1] == (byte)Telnet.SB)
                    {
                        IACNego = true;
                        currentIndex += 2;
                    }
                    else if (IACNego == true && currentIndex + 1 < rawdata.Length
                        && rawdata[currentIndex + 1] == (byte)Telnet.SE)
                    {
                        IACNego = false;
                        currentIndex += 2;
                    }
                    else if(currentIndex + 1 < rawdata.Length && rawdata[currentIndex + 1] == (byte)Telnet.GoAhead)
                    {
                        currentIndex += 2;
                    }
                }
                else if (AnsiZone == true && rawdata[currentIndex] >= 64 && rawdata[currentIndex] <= 127)
                //end of Ansi zone
                {
                    beforeMoveCursor = new Point(curPos.X, curPos.Y - 1);
                    if (rawdata[currentIndex] == 72) // *[ H
                    {
                        checkHiByte();
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
                    }
                    else if (rawdata[currentIndex] == 74) // *[ J
                    {
                        beforeMoveCursor = new Point(curPos.X, curPos.Y - 1);
                        int[] tempAnsi = new int[6];
                        tempAnsi = analyzeAnsiCode(AnsiCode);
                        //clear entire screen = delete all stored words
                        if (tempAnsi[0] == 2)
                        {
                            curPos.Y = 0; curPos.X = 0;
                            for (int row = 0; row < ROW; row++)
                            {
                                for (int col = 0; col < COL; col++)
                                {
                                    BBSPage[row, col].resetData();
                                }
                            }
                        }
                    }
                    else if (rawdata[currentIndex] == 75) // *[ K
                    {
                        escapeK();
                    }
                    else if (rawdata[currentIndex] == 109) // *[ m
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
                    }
                    else if (rawdata[currentIndex] == 77) // *[ M scroll up one line (line 24 = line 23, line0.reset())
                    {
                        if (AnsiCode.Count == 0)
                        {
                            for (int row = ROW-1; row > 0; row--)
                            {
                                for (int col = 0; col < COL; col++)
                                {
                                    BBSPage[row, col] = BBSPage[row - 1, col];
                                    BBSPage[row, col].Position = BBSPage[row - 1, col].Position;
                                }
                            }
                            for (int col = 0; col < COL; col++)
                            {
                                BBSPage[0, col] = new TelnetData();
                            }
                        }
                    }
                    else if (rawdata[currentIndex] == 115) //*[s save cursor
                    {
                        savedPos = curPos;
                    }
                    else if (rawdata[currentIndex] == 117) //*[u load cursor
                    {
                        checkHiByte();
                        curPos = savedPos;
                    }
                    else if (rawdata[currentIndex] == 65) //*[nA move cursor n up
                    {
                        checkHiByte();
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
                    else if (rawdata[currentIndex] == 66) //*[nB move cursor n down
                    {
                        checkHiByte();
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
                    else if (rawdata[currentIndex] == 67) //*[nC move cursor n right
                    {
                        checkHiByte();
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
                    else if (rawdata[currentIndex] == 68) //*[nD move cursor n left
                    {
                        checkHiByte();
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
                        Debug.WriteLine(rawdata[currentIndex]);
                    }
                    currentIndex++;
                    AnsiZone = false;
                    AnsiCode.Clear();
                }
                else //non-ansi or non-IACNegotiation
                {
                    if (AnsiZone == true && IACNego == false)
                    {
                        AnsiCode.Add(rawdata[currentIndex++]);
                    }
                    else if (IACNego == false && AnsiZone == false)
                    {
                        //store word
                        if (highByte != 0) //dual color low-byte
                        {
                            if (rawdata[currentIndex] > 0x3F) //big5 low-byte
                            {
                                string big5Word = Big5Util.ToUni(new byte[] { highByte, rawdata[currentIndex] });
                                storeDualBytes(big5Word, (int)hiPosition.X, (int)hiPosition.Y, lastAttr);
                                isDualColor = true;
                                storeDualBytes(big5Word, (int)curPos.X, (int)curPos.Y, new AnsiAttr(fg, bg, isBlinking));
                                curPos.Y++;
                            }
                            else //non-big-low-byte
                            {
                                storeASCII(0x3F, (int)hiPosition.X, (int)hiPosition.Y, lastAttr);
                                curPos.Y--;
                                storeASCII(rawdata[currentIndex], (int)curPos.X, (int)curPos.Y,new AnsiAttr(fg, bg, isBlinking)); //0x3F=='?'
                            }
                            highByte = 0;
                            hiPosition = new Point(0, 0);
                            
                            currentIndex++;
                            continue;
                        }
                        if (rawdata[currentIndex] < 128) //ASCII
                        {
                            storeASCII(rawdata[currentIndex], (int)curPos.X, (int)curPos.Y, new AnsiAttr(fg, bg, isBlinking));
                            currentIndex++;
                        }
                        else if (currentIndex + 1 < rawdata.Length)
                        {
                            if (rawdata[currentIndex + 1] != 27) //Big5
                            {
                                if (rawdata[currentIndex + 1] > 0x3F) //big5 low-byte
                                {
                                    string big5Word = Big5Util.ToUni(new byte[] { rawdata[currentIndex], rawdata[currentIndex + 1] });
                                    storeDualBytes(big5Word, (int)curPos.X, (int)curPos.Y, new AnsiAttr(fg, bg, isBlinking));
                                    storeDualBytes("", (int)curPos.X, (int)(curPos.Y + 1), new AnsiAttr(fg, bg, isBlinking));
                                    curPos.Y += 2;
                                }
                                else
                                {
                                    storeASCII(0x3F, (int)curPos.X, (int)curPos.Y, new AnsiAttr(fg, bg, isBlinking));
                                    storeASCII(rawdata[currentIndex + 1], (int)curPos.X, (int)curPos.Y, new AnsiAttr(fg, bg, isBlinking));
                                }
                                
                                currentIndex += 2;
                            }
                            else //Dual color
                            {
                                highByte = rawdata[currentIndex];
                                lastAttr.setAtt(fg, bg, isBlinking);
                                hiPosition = curPos;
                                curPos.Y++;
                                currentIndex++;
                            }
                        }
                        else //if the 79th byte > 128
                        {
                            highByte = rawdata[currentIndex];
                            lastAttr.setAtt(fg, bg, isBlinking);
                            hiPosition = curPos;
                            curPos.Y++;
                            currentIndex++;
                        }
                    }
                    else
                    { currentIndex++; }
                }
            }
            
            //#region debug2
            //StringBuilder sb = new StringBuilder();
            //for (int row = 0; row < ROW; row++)
            //{
            //    for (int col = 0; col < COL; col++)
            //    {
            //        sb.Append(BBSPage[row, col].Text);
            //    }
            //    sb.Append("\n");
            //}
            //Debug.WriteLine(sb.ToString());
            //#endregion
        }

        private static void checkHiByte()
        {
            if (highByte != 0)
            {
                storeASCII(0x3F, (int)hiPosition.X, (int)hiPosition.Y, lastAttr);
                highByte = 0;
                hiPosition = new Point(0, 0);
                curPos.Y--;
            }
        } 

        private static void storeASCII(byte word, int row, int col, AnsiAttr attr)
        {
            if (word == 13) //for "\r"
            {
                checkHiByte();
                curPos.Y = 0;
            }
            else if (word == 10) //for "\n"
            {
                checkHiByte();
                curPos.X++;

                if (curPos.X == 24) //scroll down one line (line24.reset(), line1=line2)
                {
                    curPos.X = 23;
                    for (int r = 0; r < ROW-1; r++)
                    {
                        for (int c = 0; c < COL; c++)
                        {
                            BBSPage[r, c] = BBSPage[r + 1, c];
                            BBSPage[r, c].Position = BBSPage[r + 1, c].Position;
                        }
                    }
                    for (int c = 0; c < COL; c++)
                    {
                        BBSPage[23, c] = new TelnetData();
                    }
                }
            }
            else if (word == 8) //back space "\b"
            {
                checkHiByte();
                curPos.Y--;
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
                BBSPage[row, col].Text = (char)word + "";
                BBSPage[row, col].Count = 1;
                BBSPage[row, col].setData(attr.FgColor, attr.BgColor, attr.IsBlinking, false, new Point(row, col));
                BBSPage[row, col].DualColor = false;
                curPos.Y++;
            }
        }

        private static void escapeK() //erase current line from cursor
        {
            int row = (int)curPos.X;
            for (int col = (int)curPos.Y; col < COL; col++)
            {
                BBSPage[row, col].resetData();
            }
        }

        //private static void findReplace(Canvas BBSCanvas, TelnetData toInsert)
        //{
        //    int line = (int)curPos.X;
        //    if (BBSPage[line].Count == 0 || toInsert.Text == "")
        //        return;
        //    TelnetData toSave;
        //    int count;
        //    int curY, blockY;
        //    string text;
        //    curY = (int)toInsert.Position.Y;
        //    List<TelnetData> toRemove = new List<TelnetData>();
        //    List<TelnetData> toAdd = new List<TelnetData>();
        //    foreach (TelnetData block in BBSPage[line])
        //    { 
        //        blockY = (int)block.Position.Y;
        //        //if (blockY + block.Count - 1 < curY) //reserve
        //        //{
        //        //    toAdd.Add(block);
        //        //    block.BBSBackground = null;
        //        //}
        //        //if (curY + toInsert.Count - 1 < blockY) //reserve
        //        //{
        //        //    toAdd.Add(block);
        //        //    block.BBSBackground = null;
        //        //}
        //        if (blockY < curY && curY <= blockY + block.Count - 1 && blockY + block.Count - 1 <= curY + toInsert.Count - 1)
        //        {
        //            block.Text = CropText(block.Text, 0, curY - blockY - 1);
        //            block.Count = curY - blockY;
        //            //toAdd.Add(block);
        //            foreach (UIElement ui in block.BBSUI)
        //            { BBSCanvas.Children.Remove(ui); }
        //            block.BBSUI.Clear();
        //            continue;
        //        }
        //        if (curY <= blockY && blockY <= curY + toInsert.Count - 1 && curY + toInsert.Count - 1 < blockY + block.Count - 1)
        //        {
        //            block.Text = CropText(block.Text, curY + toInsert.Count - blockY, block.Count - 1);
        //            block.Count = blockY + block.Count - curY - toInsert.Count;
        //            //toSave.Text = text;
        //            //toSave.Count = count;
        //            block.Position = new Point(line, curY + toInsert.Count);
        //            //toAdd.Add(block);
                    
        //            foreach (UIElement ui in block.BBSUI)
        //            { BBSCanvas.Children.Remove(ui); }
        //            block.BBSUI.Clear();
        //            continue;
        //        }
        //        if (blockY < curY && curY + toInsert.Count - 1 < blockY + block.Count - 1)
        //        {
        //            toSave = new TelnetData();
        //            toSave.cloneFrom(block);
        //            text = CropText(block.Text, 0, curY - blockY - 1);
        //            count = curY - blockY;
        //            toSave.Text = text;
        //            toSave.Count = count;
        //            toAdd.Add(toSave);
        //            block.Text = CropText(block.Text, curY + toInsert.Count - blockY, block.Count - 1);
        //            block.Count = blockY + block.Count - curY - toInsert.Count;
        //            block.Position = new Point(line, curY + toInsert.Count);
        //            //toAdd.Add(block);
        //            foreach (UIElement ui in block.BBSUI)
        //            { BBSCanvas.Children.Remove(ui); }
        //            block.BBSUI.Clear();
        //            continue;
        //        }
        //        if (curY <= blockY && blockY + block.Count - 1 <= curY + toInsert.Count - 1)
        //        {
        //            toRemove.Add(block);
        //            foreach (UIElement ui in block.BBSUI)
        //            { BBSCanvas.Children.Remove(ui); }
        //            block.BBSUI.Clear();
        //            continue;
        //        }
        //    }
        //    //BBSPage[line].Clear();
        //    foreach (TelnetData bk in toRemove)
        //    { BBSPage[line].Remove(bk); }
        //    foreach (TelnetData bk in toAdd)
        //    { BBSPage[line].Add(bk); }
        //    toRemove.Clear();
        //    toAdd.Clear();
        //}

        //public static int transferQty(string source)
        //{
        //    int count = 0;
        //    foreach(char ch in source)
        //    {
        //        if(ch < 256)
        //        { count++; }
        //        else
        //        { count += 2; }
        //    }
        //    return count;
        //}

        //public static string CropText(string source, int start, int end)
        //{
        //    StringBuilder result = new StringBuilder();
        //    int current = 0, index = 0;
        //    while(index < source.Length)
        //    {
        //        if (current >= start)
        //        { result.Append(source[index]); }

        //        if(source[index] < 256)
        //        { current++; }
        //        else
        //        { 
        //            current += 2;
        //            if (current - 1 == start)
        //                result.Append((char)0xA0);
        //        }

        //        if (current > end)
        //            break;
                
        //        index++;
        //    }
        //    return result.ToString();
        //}

        private static void storeDualBytes(string word, int row, int col, AnsiAttr attr)
        {
            bool isTwWord;
            if(word == "")
            {
                isTwWord = BBSPage[row, col - 1].TwWord;
            }
            else if (word[0] > 0x3040)
            {
                isTwWord = true;
            }
            else
            { isTwWord = false; }
            BBSPage[row, col].setData(attr.FgColor, attr.BgColor, attr.IsBlinking, isTwWord, new Point(row, col));
            BBSPage[row, col].DualColor = false;
            BBSPage[row, col].Text = word;
            BBSPage[row, col].Count = 1;
            if (isDualColor)
            {
                BBSPage[row, col].DualColor = true;
                isDualColor = false;
            }
        }
    }
}
