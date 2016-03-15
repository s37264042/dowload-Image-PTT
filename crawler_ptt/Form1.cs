using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.IO;

namespace crawler_ptt
{
    public partial class Form1 : Form
    {
        int progress = 0;
        Thread Crawler = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //board(TextBox) AutoComplete 
            board_TB.AutoCompleteMode = AutoCompleteMode.Suggest;
            board_TB.AutoCompleteSource = AutoCompleteSource.CustomSource;
            AutoCompleteStringCollection DataCollection = new AutoCompleteStringCollection();
            string[] boards = File.ReadAllText("database.csv").Split(',');
            foreach (string board in boards)
            {
                DataCollection.Add(board);
            }
            board_TB.AutoCompleteCustomSource = DataCollection;
        }

        //Get PTT全部的index
        private List<string> getALLindex(int index, string board)
        {
            HtmlWeb webClient = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = webClient.Load("https://www.ptt.cc/bbs/" + board + "/index.html");
            var link = doc.DocumentNode.SelectSingleNode("//body//a[contains(@class,'btn wide')][2]");
            var hrefstring = link.Attributes["href"].Value;
            var href = hrefstring.Split('/');
            int maxindex = Int32.Parse(Regex.Match(href[href.Length - 1], @"\d+").Value) + 1;
            List<string> indexlist = new List<string>();

            for (int page = maxindex; page + index > maxindex; page--)
            {
                var link_index = "https://www.ptt.cc/bbs/" + href[2] + "/index" + page + ".html";
                indexlist.Add(link_index);
                Console.WriteLine("Get PTT全部的index:" + link_index);
            }
            return indexlist;
        }

        //Get PTT每篇文章的URL
        private List<string> getTitleURL(List<string> links)
        {
            List<string> titleURLs = new List<string>();
            while (links.Count != 0)
            {
                string link = links[0];
                links.RemoveAt(0);
                HtmlWeb webClient = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = webClient.Load(link);
                var head = doc.DocumentNode.SelectSingleNode("//head//title");

                if (head.InnerText.IndexOf("Service Temporarily") > -1)
                {
                    Console.WriteLine("Service Temporarily:" + link);
                    links.Add(link);
                    //如伺服器忙線中，sleep 0.5s
                    Thread.Sleep(500);
                }
                else {
                    HtmlNodeCollection titlelinks = doc.DocumentNode.SelectNodes("//body//div[contains(@class,'title')]//a");
                    Console.WriteLine("=========PTT每篇文章URL的成功================:" + link);
                    foreach (HtmlNode titlelink in titlelinks)
                    {
                        try
                        {
                            //Console.WriteLine("https://www.ptt.cc" + titlelink.Attributes["href"].Value);
                            titleURLs.Add("https://www.ptt.cc" + titlelink.Attributes["href"].Value);
                        }
                        catch
                        {
                            Console.WriteLine("=========PTT每篇文章的URL ERROR================" + head.InnerText);
                        }
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        progress++;
                        progressBar1.Value = progress;
                        this.Text = "文章URL分析中... (1/3) " + (int)((float)progressBar1.Value / progressBar1.Maximum * 100) + " %";
                    });
                }
                //避免被認定為攻擊網站
                Thread.Sleep(50);
            }
            return titleURLs;
        }

        //Get PTT每篇文章內的圖片，抓取標籤<img>
        private List<string> getPicURL(List<string> links)
        {
            List<string> PicURL = new List<string>();
            List<string> imageFormat = new List<string>();
            imageFormat.Add(".jpg");
            imageFormat.Add(".png");
            imageFormat.Add(".gif");
            imageFormat.Add(".jpeg");

            while (links.Count != 0)
            {
                string link = links[0];
                links.RemoveAt(0);
                HtmlWeb webClient = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = webClient.Load(link);
                var head = doc.DocumentNode.SelectSingleNode("//head//title");

                if (head.InnerText.IndexOf("Service Temporarily") > -1)
                {
                    Console.WriteLine("Service Temporarily:" + link);
                    links.Add(link);
                    //如伺服器忙線中，sleep 0.5s
                    Thread.Sleep(500);
                }
                else {
                    //LINQ                      
                    var imgs = (from node in doc.DocumentNode.Descendants("a")
                                let att = node.GetAttributeValue("href", "")
                                where isImageFormat(att, imageFormat)
                                select node.GetAttributeValue("href", "")).ToList<string>();

                    foreach (string img in imgs)
                    {
                        Console.WriteLine("Get PTT每篇文章內的圖片，抓取標籤<img>" + img);
                        PicURL.Add(img);
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        progress++;
                        progressBar1.Value = progress;
                        this.Text = "圖片URL分析中... (2/3) " + (int)((float)progressBar1.Value / progressBar1.Maximum * 100) + " %";
                    });
                }
                //避免被認定為攻擊網站
                Thread.Sleep(50);
            }
            return PicURL;
        }

        //檢查是否為圖片格式
        private bool isImageFormat(string content, List<string> imageformats)
        {
            foreach (string imageformat in imageformats)
            {
                var check = content.IndexOf(imageformat, StringComparison.OrdinalIgnoreCase) >= 0;
                if (check) return true;
            }
            return false;
        }

        //下載圖片
        private void DownloadPicture(string imgURL, string Dir)
        {
            try
            {
                WebRequest requestPic = WebRequest.Create(imgURL);
                WebResponse responsePic = requestPic.GetResponse();
                Image webImage = Image.FromStream(responsePic.GetResponseStream());
                string[] filename = imgURL.Split('/');
                webImage.Save(Dir + '\\' + filename[filename.Length - 1]);
            }
            catch
            {
                Console.WriteLine("download error:" + imgURL);
            }
        }

        private void crawler()
        {
            List<string> imgURLs = new List<string>();
            if (radioButton1.Checked)
            {
                var links = getALLindex(Int32.Parse(page_TB.Text.Trim()), board_TB.Text.Trim());
                this.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Maximum = links.Count;

                });
                var titles = getTitleURL(links);
                progress = 0;
                this.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Maximum = titles.Count;
                });
                imgURLs = getPicURL(titles);
            }

            if (radioButton2.Checked)
            {
                List<string> PageURL = new List<string>();
                PageURL.Add(URL.Text.Trim());
                this.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Maximum = PageURL.Count;
                });
                imgURLs = getPicURL(PageURL);
            }

            progress = 0;
            var Dir = "image_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            System.IO.Directory.CreateDirectory(Dir);

            this.Invoke((MethodInvoker)delegate
            {
                progressBar1.Maximum = imgURLs.Count;
            });

            foreach (string imgURL in imgURLs)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    progress++;
                    progressBar1.Value = progress;
                    this.Text = "下載圖片... (3/3) " + (int)((float)progressBar1.Value / progressBar1.Maximum * 100) + "%";
                });
                DownloadPicture(imgURL, Dir);
                //避免被認定為攻擊網站
                Thread.Sleep(50);
            }
            MessageBox.Show("下載完成");
            Application.ExitThread();
            Environment.Exit(0);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            groupBox3.Enabled = false;
            this.Text = "前置作業...";
            Crawler = new Thread(new ThreadStart(crawler));
            Crawler.Start();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                groupBox1.Enabled = true;
                groupBox2.Enabled = false;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                groupBox1.Enabled = false;
                groupBox2.Enabled = true;
            }
        }

        //設定 抓取頁數TextBox 只可以輸入 數字
        private void page_TB_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void URL_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(sender, e);
            }
        }

        private void page_TB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(sender, e);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //避免視窗關閉了，thread還在執行
            if (Crawler != null)
            {
                if (Crawler.IsAlive)
                {
                    Crawler.Abort();
                }
            }
        }
    }
}
