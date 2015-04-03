using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Linq;

namespace KzBBS
{
    class Big5Util
    {
        private static Dictionary<int, int> mBIG5_Unicode_MAP = new Dictionary<int, int>();
        private static Dictionary<int, int> mUnicode_BIG5_MAP = new Dictionary<int, int>();
        public static bool TableSeted = false;
        //public static Dictionary<int, int> Big5UnicodeTable
        //{
        //    get { return mBIG5_Unicode_MAP; }
        //    set { mBIG5_Unicode_MAP = value; }
        //}

        public static async Task generateTable()
        {
            if (TableSeted)
            {
                return;
            }
            Windows.ApplicationModel.Package package = Windows.ApplicationModel.Package.Current;
            Windows.Storage.StorageFolder installedLocation = package.InstalledLocation;
            StorageFile readBig5toUni = await installedLocation.GetFileAsync("moz18-b2u.txt");
            StorageFile readUnitoBig5 = await installedLocation.GetFileAsync("moz18-u2b.txt");
            IList<string> temp = await FileIO.ReadLinesAsync(readBig5toUni);
            IList<string> temp2 = await FileIO.ReadLinesAsync(readUnitoBig5);

            String line = null;
            for (int i = 0; i < temp.Count; i++ )
            {
                line = temp[i];
                if (line.StartsWith("#"))
                    continue; // Comments
                string[] lTokens = line.Split(new char[] { ' ' });
                if (lTokens.Length < 2)
                    continue; // Not enough tokens
                try
                {
                    mBIG5_Unicode_MAP.Add(int.Parse(lTokens[0].Substring(2), NumberStyles.HexNumber),
                        int.Parse(lTokens[1].Substring(2), NumberStyles.HexNumber));
                }
                catch (Exception)
                {
                    throw; // No mapping
                }
            }

            for (int i = 0; i < temp2.Count; i++)
            {
                line = temp2[i];
                if (line.StartsWith("#"))
                    continue; // Comments
                string[] lTokens = line.Split(new char[] { ' ' });
                if (lTokens.Length < 2)
                    continue; // Not enough tokens
                try
                {
                    mUnicode_BIG5_MAP.Add(int.Parse(lTokens[1].Substring(2), NumberStyles.HexNumber),
                        int.Parse(lTokens[0].Substring(2), NumberStyles.HexNumber));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    throw; // No mapping
                }
            }

            TableSeted = true;
        }

        public static string ToUni(byte[] pureText)
        {
            StringBuilder _StringBuilder = new StringBuilder();
            byte[] big5Buffer = new byte[2];
            byte input = 0;
            foreach(byte word in pureText)
            {
                input = word;
                if (input >= 0x81 && input <= 0xFE && big5Buffer[0] == 0) //lead byte of big 5 high bits
                {
                    big5Buffer[0] = (byte)input;
                }
//0x8140-0xA0FE 保留給使用者自定義字元（造字區） 
//0xA140-0xA3BF 標點符號、希臘字母及特殊符號，
//              包括在0xA259-0xA261，安放了九個計量用漢字：兙兛兞兝兡兣嗧瓩糎。 
//0xA3C0-0xA3FE 保留。此區沒有開放作造字區用。 
//0xA440-0xC67E 常用漢字，先按筆劃再按部首排序。 
//0xC6A1-0xC8FE 保留給使用者自定義字元（造字區） 
//0xC940-0xF9D5 次常用漢字，亦是先按筆劃再按部首排序。 
//0xF9D6-0xFEFE 保留給使用者自定義字元（造字區） 
                else if (big5Buffer[0] != 0)
                {
                    if ((input >= 0x40 && input <= 0x7E) || (input >= 0xA1 && input <= 0xFE))
                    {
                        big5Buffer[1] = (byte)input; // big 5 low bits
                        {
                            int Big5Char = (big5Buffer[0] << 8) + big5Buffer[1];
                            try
                            {
                                int UTF8Char = mBIG5_Unicode_MAP[Big5Char];
                                _StringBuilder.Append((char)UTF8Char);
                            }
                            catch (Exception)
                            {
                                _StringBuilder.Append((char)mBIG5_Unicode_MAP[0xA148]); 
                                // No mapping, use replacement character
                            }
                        }
                    }
                    else
                    {
                           _StringBuilder.Append((char)big5Buffer[0]);
                           _StringBuilder.Append((char)input);
                    }               
                    big5Buffer = new byte[2];
                    
                }
                else
                {
                    if (input != 0)
                        _StringBuilder.Append((char)input);
                       
                    else
                        Debug.WriteLine("0");
                }
            }
            return _StringBuilder.ToString();
        }

        public static byte[] ToBig5Bytes(string unicodeWords)
        {
            List<byte> final = new List<byte>();

            for (int i = 0; i < unicodeWords.Length; i++)
            {
                int unicodeValue = (int)unicodeWords[i];
                if (0 < unicodeValue && unicodeValue < 256)
                {
                    byte[] asciiArray = BitConverter.GetBytes(unicodeValue);
                    final.Add(asciiArray[0]);
                }
                else
                {
                    int big5Key = 0;
                    big5Key = mUnicode_BIG5_MAP[unicodeValue];
                    if (big5Key != 0)
                    {
                        byte[] big5Array = BitConverter.GetBytes(big5Key);
                        final.Add(big5Array[1]);
                        final.Add(big5Array[0]);
                    }
                }
            }
            return final.ToArray();
        }
    }
}
