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
    public partial class Server : Form
    {
        int a = 0;
        int b = 0;
        int x = 0;
        int dem = 0;
        int luot = 0;
        public Server()
        {
            InitializeComponent();
            Thread t = new Thread(StartUnsafeThread);
            t.Start();
            n_player.Text = dem.ToString();
            n_luot.Text = luot.ToString();
            Close_Server.Enabled = false;
        }
        //tạo lớp Userinfo để tính điểm cho từng người chơi, lớp này lưu thông tin tên người chơi và điểm của họ
        public class UserInfo
        {
            public string username { get; set; }
            public int diem { get; set; }
            public UserInfo()
            {
                username = "";
                diem = 0;//điểm khởi tạo của từng người chơi là 0, mỗi lượt trả lời đúng sẽ được cộng 5 điểm
            }

        }
        //thêm từng người chơi vào danh sách người chơi
        List<UserInfo> tinhdiem = new List<UserInfo>();
       
        public int addinfo(string name)
        {
            UserInfo u = new UserInfo();
            u.username = name;
            u.diem = 5;
            tinhdiem.Add(u);
            return u.diem;
        }
        public void TinhDiem(Socket c,string name)
        {
            int flag = 0;//xác định người chơi thắng lần đầu
            //tìm trong danh sách người chơi đã có tên người vừa thắng hay chưa, nếu có cập nhật điểm
            //nếu không, thêm người đó vào danh sách và cập nhật điểm
            foreach (UserInfo u in tinhdiem)
            {
                if (u.username == name)
                {
                    u.diem += 5;
                    c.Send(Encoding.UTF8.GetBytes(", " + u.diem.ToString()+"\n"));
                    flag = 1;
                    break;
                }
            }
            if (flag == 0)
            {
                int point = addinfo(name);
                c.Send(Encoding.UTF8.GetBytes(", " + point.ToString() + "\n"));
            }
        }
        int bytesReceived = 0;
        // Khởi tạo mảng byte nhận dữ liệu
        byte[] recv = new byte[1];
        // Tạo socket bên gởi
        Socket clientSocket;
        //Tạo 1 danh sách các Socket chứa các client chat khác nhau
        List<Socket> cacClient = new List<Socket>();
        // Tạo socket bên nhận.
        Socket listenerSocket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
            );
        public void StartUnsafeThread()
        {
            // Gán socket lắng nghe tới địa chỉ IP của máy và port 8080
            IPEndPoint ipepServer = new IPEndPoint(IPAddress.Any, 8080);
            listenerSocket.Bind(ipepServer);

            // bắt đầu lắng nghe. Socket.Listen(int backlog)
            // với backlog: là độ dài tối đa của hàng đợi các kết nối đang chờ xử lý
            listenerSocket.Listen(1);
            while (true)
            {
                clientSocket = listenerSocket.Accept();
                //Lưu client được thêm vào danh sách. Để dễ quản lí
                cacClient.Add(clientSocket);
                InfoMessage("New client connect from: " + clientSocket.RemoteEndPoint);
                //đếm số người chơi mới kết nối vào và cập nhật lên textbox
                dem++;
                NumberOfPlayer(dem.ToString());
                //thêm người chơi mới kết nối vào list người chơi
                addinfo(clientSocket.RemoteEndPoint.ToString());
                //Tạo thread ChatClient mới cho các client mới
                ThreadPool.QueueUserWorkItem(ClientGuess, clientSocket);  //Mỗi client sẽ được xử lí trong 1 luồng (thread) riêng
                //đợi phương thức để thực thi. Phương thức thực thi khi một nhóm thread được tạo.
            }
        }

        //Nhận thông điệp từ client, kiểm tra kết quả và phản hồi, tạo lượt chơi mới nếu có client thắng
        public void ClientGuess(object obj)
        {
            //khởi tạo socket
            Socket clientSocket = obj as Socket;
            while (clientSocket.Connected)
            {
                string Server_text = clientSocket.RemoteEndPoint.ToString() + ": ";
                string Client_text = "";
                string[] s = new string[2];
                do
                {
                    bytesReceived = clientSocket.Receive(recv);
                    //thông điệp nhận từ các client để hiển thị lên listview của server
                    Server_text += Encoding.UTF8.GetString(recv);
                    //thông điệp nhận từ các client và gửi đi
                    Client_text += Encoding.UTF8.GetString(recv);
                } while (Server_text[Server_text.Length - 1] != '\n');
                //hiện thông điệp nhận từ các client lên listview của server
                InfoMessage(Server_text);
                s = Client_text.Split(':');
                string name = "";
                if ((s[1]).Trim().Equals(x.ToString()))//nếu 1 client nào đó thắng
                {
                    name = s[0];//tên người thắng
                    luot++;//tăng số lượt chơi lên
                    if (luot == 8)//nếu lượt hết lượt chơi qui định là 7 thì dừng gửi a,b, công bố người thắng chung cuộc
                    {
                        TinhDiem(clientSocket, name);
                        foreach (Socket client_Socket in cacClient) //duyệt từng client trong list Socket các client
                        {
                            //gửi thông điệp đi
                            client_Socket.Send(Encoding.UTF8.GetBytes("?" + name + " win this turn\n"));
                            client_Socket.Send(Encoding.UTF8.GetBytes("- " + luot.ToString() + "\n"));
                        }
                        InfoMessage(name + " win this turn");
                        FinalWinner();
                        Close_Server.Invoke(new MethodInvoker(delegate ()
                        {
                            Close_Server.Enabled = true;
                        }));
                    }
                    else//ngược lại tiếp tục lượt chơi mới
                    {
                        TinhToan();//tạo a,b mới
                        infoturn(a.ToString(), b.ToString(), x.ToString(), luot.ToString());//cập nhật a,b,x,lượt chơi lên textbox
                        //tính điểm cho người chơi thắng cuộc, gửi điểm cập nhật cho người thắng
                        TinhDiem(clientSocket, name);
                        foreach (Socket client_Socket in cacClient) 
                        {
                            //gửi thông điệp đi
                            client_Socket.Send(Encoding.UTF8.GetBytes("?" + name + " win this turn\n"));
                            client_Socket.Send(Encoding.UTF8.GetBytes("`" + a.ToString() + "\n"));
                            client_Socket.Send(Encoding.UTF8.GetBytes("~" + b.ToString() + "\n"));
                            client_Socket.Send(Encoding.UTF8.GetBytes("- " + luot.ToString() + "\n"));
                        }
                        InfoMessage(name + " win this turn");
                    }
                }
            }
            clientSocket.Close();
            listenerSocket.Close();


        }
        public void infoturn(string a, string b, string x, string l)
        {

            if (nx.InvokeRequired || na.InvokeRequired || nb.InvokeRequired || n_luot.InvokeRequired)
            {
                nx.Invoke(new MethodInvoker(delegate ()
                {
                    nx.Text = x;
                }));
                na.Invoke(new MethodInvoker(delegate ()
                {
                    na.Text = a;
                }));
                nb.Invoke(new MethodInvoker(delegate ()
                {
                    nb.Text = b;
                }));
                n_luot.Invoke(new MethodInvoker(delegate ()
                {
                    n_luot.Text = l;
                }));
            }
            else
            {
                nx.Text = x;
                na.Text = a;
                nb.Text = b;
            }

        }
        public void NumberOfPlayer(string mess)
        {

            if (n_player.InvokeRequired)
            {
                n_player.Invoke(new MethodInvoker(delegate ()
                {
                    n_player.Text = mess;
                }));
            }
            else
            {
                n_player.Text = mess;
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

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
        //tính giá trị a,b,x random
        public void TinhToan()
        {
            Random r = new Random();
            a = r.Next(0, 10);
            b = r.Next(a + 3, 30);
            x = r.Next(a + 1, b - 1);
        }
        //gửi cho các client trong lần đầu mở trò chơi, button sau đó sẽ bị vô hiệu hóa
        private void btn_SendX_Click(object sender, EventArgs e)
        {
            luot++;
            TinhToan();
            na.Text = a.ToString();
            nb.Text = b.ToString();
            nx.Text = x.ToString();
            n_luot.Text = luot.ToString();
            foreach (Socket client in cacClient)
            {
                client.Send(Encoding.UTF8.GetBytes("`" + a.ToString() + "\n"));
                client.Send(Encoding.UTF8.GetBytes("~" + b.ToString() + "\n"));
                client.Send(Encoding.UTF8.GetBytes("**New turn " + luot.ToString() + " started. Let choose a number between " + a.ToString() + " and " + b.ToString() + "\n"));
            }
            btn_SendX.Enabled = false;

        }

        private void Server_Load(object sender, EventArgs e)
        {

        }
        //thông báo người thắng chung cuộc
        public void FinalWinner()
        {
            int maxpoint = 0;
            string name = "";
            foreach (UserInfo u in tinhdiem)
            {
                if (u.diem > maxpoint)
                {
                    maxpoint = u.diem;
                    name = u.username;
                }
            }
            foreach (Socket client in cacClient)
            {
                client.Send(Encoding.UTF8.GetBytes(name + " is the final winner with " + maxpoint.ToString() + " point\n"));
            }
            InfoMessage(name + " is the final winner with " + maxpoint.ToString() + " point");
        }
        //luu lịch sử người chơi xuông
        private void Close_Server_Click(object sender, EventArgs e)
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
