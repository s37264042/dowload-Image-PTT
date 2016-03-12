using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Data.SQLite;

namespace crawler_ptt
{
    public partial class Form1 : Form
    {
        private SQLiteConnection sqlite_conn;
        private SQLiteCommand sqlite_cmd;

        public Form1()
        {
            InitializeComponent();
            backgroundWorker1.WorkerReportsProgress = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            board_TB.AutoCompleteMode = AutoCompleteMode.Suggest;
            board_TB.AutoCompleteSource = AutoCompleteSource.CustomSource;
            AutoCompleteStringCollection DataCollection = new AutoCompleteStringCollection();
            getData(DataCollection);
            board_TB.AutoCompleteCustomSource = DataCollection;
        }

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
                Console.WriteLine(link_index);
                Application.DoEvents();
            }
            return indexlist;
        }

        private List<string> getTitleURL(List<string> links)
        {
            List<string> titlelist = new List<string>();
            int Progress = 0;

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
                    Thread.Sleep(500);
                }
                else {
                    HtmlNodeCollection titlelinks = doc.DocumentNode.SelectNodes("//body//div[contains(@class,'title')]//a");
                    Console.WriteLine("成功:" + link);
                    foreach (HtmlNode img in titlelinks)
                    {
                        try
                        {
                            foreach (HtmlNode titlelink in titlelinks)
                            {
                                //Console.WriteLine(titlelink.InnerText);
                                //Console.WriteLine("https://www.ptt.cc" + titlelink.Attributes["href"].Value);
                                titlelist.Add("https://www.ptt.cc" + titlelink.Attributes["href"].Value);
                                Application.DoEvents();
                            }
                        }
                        catch
                        {
                            Console.WriteLine("=========================" + head.InnerText);
                        }
                        Application.DoEvents();
                    }
                    Progress++;
                    backgroundWorker1.ReportProgress(Progress, "文章URL分析中... (1/3)");
                }
                Application.DoEvents();
            }
            return titlelist;
            /*
            foreach (string link in links)
            {
                HtmlWeb webClient = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = webClient.Load(link);
                HtmlNodeCollection titlelinks = doc.DocumentNode.SelectNodes("//body//div[contains(@class,'title')]//a");
                try
                {
                    foreach (HtmlNode titlelink in titlelinks)
                    {
                        //Console.WriteLine(titlelink.InnerText);
                        //Console.WriteLine("https://www.ptt.cc" + titlelink.Attributes["href"].Value);
                        titlelist.Add("https://www.ptt.cc" + titlelink.Attributes["href"].Value);
                        Application.DoEvents();
                    }
                }
                catch
                {
                    var head = doc.DocumentNode.SelectSingleNode("//head//title");

                    Console.WriteLine("========================="+head.InnerText);
                    Thread.Sleep(10000);
                }
            }*/

        }


        private List<string> getPicURL(List<string> links)
        {
            List<string> PicURL = new List<string>();

            int Progress = 0;

            while (links.Count != 0)
            {
                string link = links[0];
                links.RemoveAt(0);
                HtmlWeb webClient = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = webClient.Load(link);
                var head = doc.DocumentNode.SelectSingleNode("//head//title");
                if (head.InnerText.IndexOf("Service Temporarily") > -1)
                {
                    ///Console.WriteLine("Service Temporarily:" + link);
                    links.Add(link);
                    Thread.Sleep(500);
                }
                else {
                    HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a");
                    //Console.WriteLine("成功:" + link);
                    foreach (HtmlNode img in nodes)
                    {
                        if (img.InnerText.IndexOf(".png", StringComparison.CurrentCultureIgnoreCase) > -1 ||
                             img.InnerText.IndexOf(".jpg", StringComparison.CurrentCultureIgnoreCase) > -1 ||
                             img.InnerText.IndexOf(".jpeg", StringComparison.CurrentCultureIgnoreCase) > -1 ||
                             img.InnerText.IndexOf(".gif", StringComparison.CurrentCultureIgnoreCase) > -1)
                        {
                            Console.WriteLine(img.InnerText);
                            PicURL.Add(img.InnerText);
                        }

                    }
                    Progress++;
                    backgroundWorker1.ReportProgress(Progress, "圖片URL分析中... (2/3)");
                }
                Application.DoEvents();
            }
            return PicURL;
        }
        /*
        private bool RemoteFileExists(string url)
        {
            try
            {
                //Creating the HttpWebRequest
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //Setting the Request method HEAD, you can also use GET too.
                request.Method = "HEAD";
                //Getting the Web Response.
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //Returns TRUE if the Status code == 200
                response.Close();
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                //Any exception will returns false.
                return false;
            }
        }*/
        private void LoadPicture(string imgURL, string Dir)
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

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            groupBox3.Enabled = false;
            this.Text = "前置作業...";
            backgroundWorker1.RunWorkerAsync();
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



        private void getData(AutoCompleteStringCollection dataCollection)
        {
            sqlite_conn = new SQLiteConnection("Data source=database.db");
            sqlite_conn.Open();
            sqlite_cmd = sqlite_conn.CreateCommand();

            // 查詢剛新增的表test
            sqlite_cmd.CommandText = "SELECT * FROM PTT_Board";
            // 執行查詢塞入 sqlite_datareader
            SQLiteDataReader sqlite_datareader = sqlite_cmd.ExecuteReader();
            // 一筆一筆列出查詢的資料
            try
            {
                while (sqlite_datareader.Read())
                {
                    String content = sqlite_datareader["board"].ToString();
                    dataCollection.Add(content.ToString());
                    //MessageBox.Show(content);
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Can not open connection ! ");
            }
            //結束
            sqlite_conn.Close();
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            List<string> imgURLs = new List<string>();
            if (radioButton1.Checked)
            {
                var links = getALLindex(Int32.Parse(page_TB.Text), board_TB.Text);
                this.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Maximum = links.Count;
                });
                var titles = getTitleURL(links);

                this.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Maximum = titles.Count;
                    imgURLs = getPicURL(titles);
                    progressBar1.Maximum = 0;
                });

            }

            if (radioButton2.Checked)
            {
                List<string> PageURL = new List<string>();
                PageURL.Add(URL.Text);
                this.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Maximum = PageURL.Count;
                    imgURLs = getPicURL(PageURL);
                    progressBar1.Maximum = 0;
                });

            }

            this.Invoke((MethodInvoker)delegate
            {
                progressBar1.Maximum = imgURLs.Count;
            });

            var Dir = "image_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            System.IO.Directory.CreateDirectory(Dir);
            int i = 0;

            foreach (string imgURL in imgURLs)
            {
                LoadPicture(imgURL, Dir);
                i++;
                backgroundWorker1.ReportProgress(i, "下載圖片... (3/3)");
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            Console.WriteLine("e.UserState:" + e.UserState);
            //Console.WriteLine(progressBar1.Value);      
            this.Text = e.UserState.ToString() + " " + (int)((float)e.ProgressPercentage / progressBar1.Maximum * 100) + "%";
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("下載完成");
            Application.Exit();
        }

        private void page_TB_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((int)e.KeyChar < 48 | (int)e.KeyChar > 57)
            {
                e.Handled = true;
            }
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
    }
}
