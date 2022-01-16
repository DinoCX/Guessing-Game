using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Client : Form
    {
        //1 Tạo đối tượng TcpClient
        TcpClient tcpClient = new TcpClient();
        //3 Tạo luồng để đọc và ghi dữ liệu dựa trên NetworkStream
        NetworkStream ns;
        int a = 0;
        int b = 0;
        int x = 0;
        public Client(string username)
        {
            InitializeComponent();
            //Địa chỉ mặc định
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 8080);
            tcpClient.Connect(ipEndPoint);
            ns = tcpClient.GetStream();
            //Start luồng nhận dữ liệu từ server
            Thread receive = new Thread(Receive);
            receive.Start();
            point.Text = "0";
            Name_ID.Text = username;
            EndGame.Enabled = false;
        }

        public void Receive()
        {
            int bytesReceived = 0;
            // Khởi tạo mảng byte nhận dữ liệu
            byte[] recv = new byte[1];
            while (tcpClient.Connected)
            {
                string text = "";
                do
                {
                    // Dùng phương thức Read để nhận dữ liệu từ Server
                    bytesReceived = ns.Read(recv, 0, 1);
                    text += Encoding.UTF8.GetString(recv);
                } while (text[text.Length - 1] != '\n');
                if (text[0].Equals('`'))//nhận giá trị số a
                {
                    a = Int32.Parse(text.Substring(1));
                    Number_a(a.ToString());
                }
                else if (text[0].Equals('~'))//nhận giá trị số b
                {

                    b = Int32.Parse(text.Substring(1));
                    Number_b(b.ToString());
                }
                else if (text[0].Equals('?'))//nhận thông báo về người thắng trong lượt chơi, ngay khi nhận được thông báo, dừng đồng hồ đếm và cho phep người chơi gửi giá trị cho lượt chơi mới
                {
                    total = 0;
                    string s = text.Substring(1);
                    InfoMessage(s);
                    number = new HashSet<int>();//tạo hashset mới để  lưu giá trị random mới cho lượt mới.
                }
                else if (text[0].Equals(','))//nhận thông báo kết quả đoán đúng, cập nhật điểm số đạt được mới nhất lên textbox, nếu không giữ nguyên điểm trước đó
                {
                    string[] s = text.Split(' ');
                    InfoMessage("Congratulation! You have 5 point");

                    if (point.InvokeRequired)
                    {
                        point.Invoke(new MethodInvoker(delegate ()
                        {
                            point.Text = s[1];
                        }));
                    }
                }
                else if (text[0].Equals('-'))//nhận thông báo kết quả đoán đúng, cập nhật điểm số đạt được mới nhất lên textbox, nếu không giữ nguyên điểm trước đó
                {
                    string[] s = text.Split(' ');
                    if (s[1].Trim().Equals("8"))
                    {
                        MessageBox.Show("Game over");
                        Control();//vô hiệu hóa các button khi trò chơi kết thúc
                    }
                    else
                        InfoMessage("**New turn " + s[1] + " started. Let choose a number between " + a.ToString() + " and " + b.ToString() + "\n");
                }
                else InfoMessage(text);
            }
            ns.Close();
            tcpClient.Close();
        }
        public void Control()
        {
            if (btn_AutoPlay.InvokeRequired || btn_Send.InvokeRequired || EndGame.InvokeRequired)
            {
                btn_AutoPlay.Invoke(new MethodInvoker(delegate ()
                {
                    btn_AutoPlay.Enabled = false;
                }));
                btn_Send.Invoke(new MethodInvoker(delegate ()
                {
                    btn_Send.Enabled = false;
                }));
                EndGame.Invoke(new MethodInvoker(delegate ()
                {
                    EndGame.Enabled = true;
                }));
            }

        }
        public void Number_a(string mess)
        {
            if (na.InvokeRequired)
            {
                na.Invoke(new MethodInvoker(delegate ()
                {
                    na.Text = mess;
                }));
            }
            else
            {
                na.Text = mess;
            }
        }
        public void Number_b(string mess)
        {
            if (nb.InvokeRequired)
            {
                nb.Invoke(new MethodInvoker(delegate ()
                {
                    nb.Text = mess;
                }));
            }
            else
            {
                nb.Text = mess;
            }
        }
        public void InfoMessage(string mess)
        {
            ListViewItem item = new ListViewItem();
            item.Text = mess;
            if (listMesage.InvokeRequired)
            {
                listMesage.Invoke(new MethodInvoker(delegate ()
                {
                    listMesage.Items.Add(item);
                    listMesage.EnsureVisible(listMesage.Items.Count - 1);//tự động kéo xuống
                }));
            }
            else
            {
                listMesage.Items.Add(item);
            }

        }
        //tổng thời gian đếm ngược
        int total;
        int second;
        int mili_second;
        //button gửi số cần đoán cho server, chỉ được phép gửi khi các thông số a,b,x,username có đầy đủ, ngay khi nhấn button, người chơi phải chờ 3 giây để có thể gửi giá trị tiếp theo.
        private void btn_Send_Click(object sender, EventArgs e)
        {
            total = 300;
            second = 3;
            mili_second = 100;
            if (Name_ID.Text == string.Empty || nx.Text == string.Empty)
            {
                MessageBox.Show("Please enter a number!");
            }
            else if (na.Text == string.Empty || nb.Text == string.Empty)
                MessageBox.Show("Game hasn't started yet, please wait!");
            else if (int.Parse(na.Text) > int.Parse(nx.Text) || int.Parse(nx.Text) > int.Parse(nb.Text))
                MessageBox.Show("Please guess numbers in the range!");
            else
            {
                //4 Dùng phương thức Write để gửi dữ liệu đến Server
                Byte[] data = System.Text.Encoding.UTF8.GetBytes(Name_ID.Text + ":" + nx.Text + "\n");
                ns.Write(data, 0, data.Length);
                //InfoMessage("You choose " + nx.Text);
                nx.Clear();
                timer1.Start();
            }
        }
        //đếm giờ
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (total > 0)
            {
                total--;
                second = total / 100;
                mili_second = total - (second * 100);
                timeleft.Text = second.ToString() + ":" + mili_second.ToString();
                btn_Send.Enabled = false;
                btn_AutoPlay.Enabled = false;
            }
            else
            {
                timer1.Stop();
                btn_Send.Enabled = true;
                btn_AutoPlay.Enabled = true;
            }
        }
        //tính giá trị random cho số cần đoán
        public void TinhToan()
        {
            Random r = new Random();
            x = r.Next(a + 1, b - 1);
        }
        //chứa các giá trị đã random trước đó
        HashSet<int> number = new HashSet<int>();
        //Button cho phép tự động random ngẫu nhiên 1 số và gửi đi, nếu số này đã random trước đó thì tạo giá trị random khác.
        //ngay khi nhấn button, người chơi phải chờ 3 giây để có thể gửi giá trị tiếp theo.
        private void btn_AutoPlay_Click(object sender, EventArgs e)
        {
            if (Name_ID.Text == string.Empty)
            {
                MessageBox.Show("Please enter a number!");
            }
            else if (na.Text == string.Empty || nb.Text == string.Empty)
                MessageBox.Show("Game hasn't started yet, please wait!");
            else
            {
            loop:
                total = 300;
                second = 3;
                mili_second = 100;
                int flag = 0;
                TinhToan();
                foreach (int i in number)
                {
                    if (x == i)
                    {
                        flag = 1;
                        break;
                    }
                }
                if (flag == 0)
                {
                    number.Add(x);
                    nx.Text = x.ToString();
                    Byte[] data = System.Text.Encoding.UTF8.GetBytes(Name_ID.Text + ":" + nx.Text + "\n");
                    ns.Write(data, 0, data.Length);
                    //InfoMessage("You choose " + nx.Text);
                    timer1.Start();
                }
                else goto loop;
            }
        }
       
        //lưu lịch sử của client xuống file theo địa chỉ chỉ client mong muốn
        private void EndGame_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            
            sfd.Filter = "Text File (.txt) | *.txt";
            sfd.ShowDialog();
            FileStream fs = new FileStream(sfd.FileName, FileMode.CreateNew);
            StreamWriter s = new StreamWriter(fs);
            StringBuilder sb;

            if (listMesage.Items.Count > 0)
            {
                // the actual data
                foreach (ListViewItem lvi in listMesage.Items)
                {
                    sb = new StringBuilder();

                    foreach (ListViewItem.ListViewSubItem listViewSubItem in lvi.SubItems)
                    {
                        sb.Append(string.Format(listViewSubItem.Text));
                    }
                    s.WriteLine(sb.ToString());
                }
               
                s.Close();
                this.Close();
            }
        }
    }
}
