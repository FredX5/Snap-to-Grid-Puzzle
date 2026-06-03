using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        Bitmap originalImage;

        int gridSize = 2;
        int score = 0;
        int timeLeft = 0;
        int totalTime = 120;
        bool gameStarted = false;

        Bitmap[,] correctPieces;
        int[,] correctIndexes;
        int[,] userBoard;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cmbGridSize.Items.Add("2x2");
            cmbGridSize.Items.Add("4x4");
            cmbGridSize.SelectedIndex = 0;

            lblScore.Text = "Score: 0";
            lblTime.Text = "Time: 0";

            timer1.Interval = 1000;

            pnlSource.AutoScroll = true;
        }

        private void btnSelectImage_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                originalImage = new Bitmap(openFileDialog1.FileName);
                picPreview.Image = originalImage;
                MessageBox.Show("Resim seçildi.");
            }
        }

        private void btnCreatePuzzle_Click(object sender, EventArgs e)
        {
            if (originalImage == null)
            {
                MessageBox.Show("Önce resim seçin.");
                return;
            }

            gameStarted = false;
            timer1.Stop();

            string selectedValue = cmbGridSize.SelectedItem.ToString();
            gridSize = int.Parse(selectedValue.Split('x')[0]);

            SliceImage();
            InitializeArrays();
            CreateTargetGrid();
            ShuffleAndShowPieces();

            lblScore.Text = "Score: 0";
            lblTime.Text = "Time: 0";

            MessageBox.Show("Puzzle oluşturuldu.");
        }

        private void InitializeArrays()
        {
            correctIndexes = new int[gridSize, gridSize];
            userBoard = new int[gridSize, gridSize];

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    int index = row * gridSize + col;
                    correctIndexes[row, col] = index;
                    userBoard[row, col] = -1;
                }
            }
        }

        private void SliceImage()
        {
            correctPieces = new Bitmap[gridSize, gridSize];

            int pieceWidth = originalImage.Width / gridSize;
            int pieceHeight = originalImage.Height / gridSize;

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    Bitmap piece = new Bitmap(pieceWidth, pieceHeight);

                    using (Graphics g = Graphics.FromImage(piece))
                    {
                        g.DrawImage(
                            originalImage,
                            new Rectangle(0, 0, pieceWidth, pieceHeight),
                            new Rectangle(col * pieceWidth, row * pieceHeight, pieceWidth, pieceHeight),
                            GraphicsUnit.Pixel
                        );
                    }

                    correctPieces[row, col] = piece;
                }
            }
        }

        private void CreateTargetGrid()
        {
            pnlTarget.Controls.Clear();
            pnlTarget.RowStyles.Clear();
            pnlTarget.ColumnStyles.Clear();

            pnlTarget.RowCount = gridSize;
            pnlTarget.ColumnCount = gridSize;

            for (int i = 0; i < gridSize; i++)
            {
                pnlTarget.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / gridSize));
                pnlTarget.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / gridSize));
            }

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    Panel cell = new Panel();
                    cell.Dock = DockStyle.Fill;
                    cell.BorderStyle = BorderStyle.FixedSingle;
                    cell.AllowDrop = true;
                    cell.Tag = new Point(row, col);

                    cell.DragEnter += Cell_DragEnter;
                    cell.DragDrop += Cell_DragDrop;

                    pnlTarget.Controls.Add(cell, col, row);
                }
            }
        }

        private void ShuffleAndShowPieces()
        {
            pnlSource.Controls.Clear();

            int totalPieces = gridSize * gridSize;

            List<int> indexes = new List<int>();
            for (int i = 0; i < totalPieces; i++)
            {
                indexes.Add(i);
            }

            Random rnd = new Random();
            indexes = indexes.OrderBy(x => rnd.Next()).ToList();

            for (int i = 0; i < totalPieces; i++)
            {
                int pieceIndex = indexes[i];
                int row = pieceIndex / gridSize;
                int col = pieceIndex % gridSize;

                PictureBox pb = new PictureBox();
                pb.Width = 80;
                pb.Height = 80;
                pb.BorderStyle = BorderStyle.FixedSingle;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Image = correctPieces[row, col];
                pb.Tag = pieceIndex;
                pb.MouseDown += Piece_MouseDown;

                pnlSource.Controls.Add(pb);
            }
        }

        private void Piece_MouseDown(object sender, MouseEventArgs e)
        {
            if (!gameStarted)
            {
                MessageBox.Show("Önce oyunu başlat.");
                return;
            }

            PictureBox pb = sender as PictureBox;

            if (pb != null)
            {
                pb.DoDragDrop(pb, DragDropEffects.Move);
            }
        }

        private void Cell_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PictureBox)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void Cell_DragDrop(object sender, DragEventArgs e)
        {
            if (!gameStarted) return;

            PictureBox dragged = e.Data.GetData(typeof(PictureBox)) as PictureBox;
            Panel cell = sender as Panel;

            if (dragged == null || cell == null)
                return;

            int row = pnlTarget.GetRow(cell);
            int col = pnlTarget.GetColumn(cell);

            int pieceIndex = (int)dragged.Tag;

            if (userBoard[row, col] != -1)
            {
                MessageBox.Show("Bu hücre dolu.");
                return;
            }

            if (pieceIndex == correctIndexes[row, col])
            {
                userBoard[row, col] = pieceIndex;

                pnlSource.Controls.Remove(dragged);
                dragged.Dock = DockStyle.Fill;
                cell.Controls.Add(dragged);

                score += 50;
            }
            else
            {
                score -= 10;
                MessageBox.Show("Yanlış yer!");
            }

            lblScore.Text = "Score: " + score;
            CheckGameCompleted();
        }

        private void CheckGameCompleted()
        {
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    if (userBoard[row, col] != correctIndexes[row, col])
                    {
                        return;
                    }
                }
            }

            timer1.Stop();
            gameStarted = false;

            int usedTime = totalTime - timeLeft;

            MessageBox.Show(
                "Tebrikler! Puzzle tamamlandı.\n" +
                "Geçen süre: " + usedTime + " saniye\n" +
                "Skor: " + score,
                "Oyun Bitti",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void btnStartGame_Click(object sender, EventArgs e)
        {
            if (originalImage == null)
            {
                MessageBox.Show("Önce resim seçin.");
                return;
            }

            if (correctPieces == null)
            {
                MessageBox.Show("Önce puzzle oluşturun.");
                return;
            }

            timeLeft = totalTime;
            lblTime.Text = "Time: " + timeLeft;

            score = 0;
            lblScore.Text = "Score: 0";

            gameStarted = true;
            timer1.Start();

            MessageBox.Show("Oyun başladı.");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timeLeft--;
            lblTime.Text = "Time: " + timeLeft;

            if (timeLeft <= 0)
            {
                timer1.Stop();
                gameStarted = false;
                lblTime.Text = "Time: 0";

                MessageBox.Show(
                    "Süre bitti!\nFinal skor: " + score,
                    "Oyun Bitti",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        private void picPreview_Click(object sender, EventArgs e)
        {
        }
    }
}