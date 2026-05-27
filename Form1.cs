using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace _1133327_巫廷祐
{
    public partial class tiktac : Form
    {
        Rectangle[,] board = new Rectangle[3, 3];
        char[,] state = new char[3, 3];
        bool playerTurn = true;
        bool isCheatMode = false;
        bool isGameOver = false;
        Random rnd = new Random();
        private double hue = 0;
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr winHandle);

        // 儲存轉存出來的臨時 MP3 檔案路徑
        private string pathTurn;
        private string pathWin;
        private string pathLose;
        private string pathDraw;
        private string pathCheat;

        public tiktac()
        {
            InitializeComponent();
            this.BackColor = Color.PaleGoldenrod;
            this.Paint += tiktac_Paint;
            this.MouseDown += tiktac_MouseDown;
            restartToolStripMenuItem.Click += RestartToolStripMenuItem_Click;
            ExtractAudioResources();
            InitBoard();
        }

        /// <summary>
        /// 將 Resources 中的 MP3 位元組轉存成實體暫存檔，供 MCI 播放器讀取
        /// </summary>
        private void ExtractAudioResources()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                pathTurn = Path.Combine(tempPath, "ttsmaker1.mp3");
                pathWin = Path.Combine(tempPath, "ttsmaker-win.mp3");
                pathLose = Path.Combine(tempPath, "ttsmaker_lose.mp3");
                pathDraw = Path.Combine(tempPath, "ttsmaker-draw.mp3");
                pathCheat = Path.Combine(tempPath, "ttsmaker-cheat.mp3");

                // 將 byte[] 寫入檔案 (如果資源名稱與下方不同，請自行確認 Resources 內的大小寫)
                if (Properties.Resources.ttsmaker1 != null) File.WriteAllBytes(pathTurn, Properties.Resources.ttsmaker1);
                if (Properties.Resources.ttsmaker_win != null) File.WriteAllBytes(pathWin, Properties.Resources.ttsmaker_win);
                if (Properties.Resources.ttsmaker_lose != null) File.WriteAllBytes(pathLose, Properties.Resources.ttsmaker_lose);
                if (Properties.Resources.ttsmaker_draw != null) File.WriteAllBytes(pathDraw, Properties.Resources.ttsmaker_draw);
                if (Properties.Resources.ttsmaker_cheat != null) File.WriteAllBytes(pathCheat, Properties.Resources.ttsmaker_cheat);
            }
            catch (Exception ex)
            {
                MessageBox.Show("音訊資源載入失敗: " + ex.Message);
            }
        }

        /// <summary>
        /// 播放指定路徑的 MP3 檔案
        /// </summary>
        private void PlayMp3(string filePath)
        {
            if (!File.Exists(filePath)) return;

            // 先關閉上一次的播放以防衝突，再開啟並播放
            mciSendString("close MyMP3", null, 0, IntPtr.Zero);
            mciSendString($"open \"{filePath}\" type mpegvideo alias MyMP3", null, 0, IntPtr.Zero);
            mciSendString("play MyMP3", null, 0, IntPtr.Zero);
        }

        private void InitBoard()
        {
            int size = 60;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    board[i, j] = new Rectangle(j * size + 50, i * size + 50, size, size);
                    state[i, j] = '\0';
                }
            }
            isGameOver = false;
            this.BackColor = Color.PaleGoldenrod;
            result.Text = "Player's Turn";
            PlayMp3(pathTurn); // 遊戲初始化，輪到玩家時播放
            Invalidate();
        }

        private void tiktac_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.Black, 2);
            Font font = new Font("Arial", 24, FontStyle.Bold);


            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    g.DrawRectangle(pen, board[i, j]);


            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Rectangle imgRect = new Rectangle(board[i, j].X + 5, board[i, j].Y + 5, board[i, j].Width - 10, board[i, j].Height - 10);

                    if (state[i, j] == 'O')
                    {
                        if (Properties.Resources.OIK != null)
                        {
                            g.DrawImage(Properties.Resources.OIK, imgRect);
                        }
                    }
                    else if (state[i, j] == 'X')
                    {
                        if (Properties.Resources.OIP != null)
                        {
                            g.DrawImage(Properties.Resources.OIP, imgRect);
                        }
                    }
                }
            }
        }

        private void tiktac_MouseDown(object sender, MouseEventArgs e)
        {
            if (isGameOver || !playerTurn) return;


            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j].Contains(e.Location) && state[i, j] == '\0')
                    {
                        state[i, j] = 'O';
                        playerTurn = false;
                        Invalidate();

                        if (CheckWin('O'))
                        {
                            this.BackColor = Color.LightGreen;
                            result.Text = "You Win!";
                            PlayMp3(pathWin);
                            playerTurn = false;
                            isGameOver = true;
                            return;
                        }
                        else if (IsDraw())
                        {
                            this.BackColor = Color.LightBlue;
                            result.Text = "Draw!";
                            isGameOver = true;
                            PlayMp3(pathDraw);
                            return;
                        }

                        if (isCheatMode)
                        {
                            this.BackColor = Color.WhiteSmoke;
                            playerTurn = true;
                            result.Text = "Cheat Mode: Player's Turn";
                        }
                        else
                        {
                            ComputerMove();
                        }
                        return;
                    }
                }
            }
        }
        private bool TryMove(char symbol)
        {

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (state[i, j] == '\0')
                    {
                        state[i, j] = symbol;
                        bool win = CheckWin(symbol);
                        state[i, j] = '\0';
                        if (win)
                        {
                            state[i, j] = 'X';
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private void ComputerMove()
        {

            if (TryMove('X')) { HandleComputerEnd(); return; }

            if (TryMove('O')) { HandleComputerEnd(); return; }


            if (state[1, 1] == '\0')
            {
                state[1, 1] = 'X';
            }
            else
            {

                int[,] corners = { { 0, 0 }, { 0, 2 }, { 2, 0 }, { 2, 2 } };
                bool moved = false;


                for (int k = 0; k < 4; k++)
                {
                    int i = corners[k, 0];
                    int j = corners[k, 1];
                    if (state[i, j] == '\0')
                    {
                        state[i, j] = 'X';
                        moved = true;
                        break;
                    }
                }


                if (!moved)
                {
                    int i, j;
                    do
                    {
                        i = rnd.Next(3);
                        j = rnd.Next(3);
                    } while (state[i, j] != '\0');
                    state[i, j] = 'X';
                }
            }


            playerTurn = true;
            Invalidate();

            if (CheckWin('X'))
                result.Text = "You Lose!";
            else if (IsDraw())
                result.Text = "Draw!";
            else
                result.Text = "Player's Turn";
            HandleComputerEnd();
        }

        /// <summary>
        /// 抽離電腦下完棋後的狀態判定與音效播放邏輯
        /// </summary>
        private void HandleComputerEnd()
        {
            playerTurn = true;
            Invalidate();

            if (CheckWin('X'))
            {
                this.BackColor = Color.MistyRose;
                result.Text = "You Lose!";
                PlayMp3(pathLose); // 玩家輸了
                playerTurn = false;
                isGameOver = true;
            }
            else if (IsDraw())
            {
                this.BackColor = Color.AliceBlue;
                result.Text = "Draw!";
                PlayMp3(pathDraw); // 平局
                isGameOver = true;
            }
            else
            {
                result.Text = "Player's Turn";
                PlayMp3(pathTurn); // 輪到玩家
                playerTurn = true;
            }
        }

        private bool CheckWin(char symbol)
        {
            for (int i = 0; i < 3; i++)
            {
                if (state[i, 0] == symbol && state[i, 1] == symbol && state[i, 2] == symbol) return true;
                if (state[0, i] == symbol && state[1, i] == symbol && state[2, i] == symbol) return true;
            }
            if (state[0, 0] == symbol && state[1, 1] == symbol && state[2, 2] == symbol) return true;
            if (state[0, 2] == symbol && state[1, 1] == symbol && state[2, 0] == symbol) return true;

            return false;
        }

        private bool IsDraw()
        {
            foreach (char c in state)
                if (c == '\0') return false;
            return true;
        }

        private void RestartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitBoard();
            playerTurn = true;
            isCheatMode = false;
        }

        private void cheat_Click(object sender, EventArgs e)
        {
            this.BackColor = Color.WhiteSmoke;
            if (isGameOver) return;
            // 切換作弊狀態
            isCheatMode = !isCheatMode;
            PlayMp3(pathCheat);
            if (isCheatMode)
            {
                playerTurn = true; // 確保點擊時玩家可以馬上下棋
                result.Text = "Cheat Mode On! Player's Turn";
            }
            else
            {
                this.BackColor = SystemColors.Control;
                result.Text = "Cheat Mode Off! Player's Turn";
            }
        }
    }
}
