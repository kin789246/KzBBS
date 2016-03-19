using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;

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
        public TelnetText number { get; set; }
        public string UniqueId { get; set; }
        public TelnetText isRead { get; set; }
        public TelnetText hot { get; set; }
        public TelnetText date { get; set; }
        public TelnetText Author { get; set; }
        public TelnetText board { get; set; }
        public TelnetText category { get; set; }
        public TelnetText title { get; set; }
        public TelnetText push { get; set; }
        public int pushFloor { get; set; }
        //public bool Changed { get; set; }
        public List<PTTBlock> Blocks { get; set; }

        public PTTLine()
        {
            No = 0;
            Text = "";
            number = new TelnetText();
            UniqueId = "";
            isRead = new TelnetText();
            hot = new TelnetText();
            date = new TelnetText();
            Author = new TelnetText();
            board = new TelnetText();
            category = new TelnetText();
            title = new TelnetText();
            push = new TelnetText();
            pushFloor = 0;
            //Changed = true;
            Blocks = new List<PTTBlock>();
        }

        public PTTLine(PTTLine line)
        {
            No = line.No;
            Text = line.Text;
            UniqueId = line.UniqueId;
            isRead = line.isRead;
            Author = line.Author;
            board = line.board;
            category = line.category;
            title = line.title;
            push = line.push;
            pushFloor = line.pushFloor;
            //Changed = line.Changed;

            Blocks = new List<PTTBlock>();
            foreach (var block in line.Blocks)
            {
                Blocks.Add(block);
            }
        }
    }

    class PTTPage
    {
        public BBSMode Mode { get; set; }
        public string BoardName { get; set; }
        public List<PTTLine> Lines { get; set; }

        public PTTPage()
        {
            Mode = BBSMode.Other;
            BoardName = "";
            Lines = new List<PTTLine>();
        }

        public PTTPage(PTTPage page)
        {
            Mode = page.Mode;
            BoardName = page.BoardName;
            Lines = new List<PTTLine>();
            foreach (var item in page.Lines)
            {
                Lines.Add(item);
            }
        }
        /* 

              func compareList(a:PTTPage) -> Bool {
              var result = false
              //compare 0 to 22st line
              for var i = 0; i<self.Lines.count-1; i++ {
                  if self.Lines[i].Text == a.Lines[i].Text {
                      result = true
                  }
                  else {
                      result = false
                      break
                  }
              }
              return result
          } */
    }
}
