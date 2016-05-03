using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Windows;
using System.Runtime.InteropServices;

namespace TimeSplitter
{
    public partial class TimeSplitter : Form
    {
        bool paused = true;
        DateTime time_start;
        bool idleTime = false;
        bool msgBoxConfirmAnswer = true;
        string notesFileName;
        TimeSpan idleTimeCorrection = new TimeSpan(0, 10, 0);

        public TimeSplitter()
        {
            InitializeComponent();

            string today = DateTime.Today.ToShortDateString();
            notesFileName = "TimeSplitter_" + today + ".txt";

            load_records_from_file();

            textNotes.Text = "Idle Time";
            start_timer();
        }

        private void msgBoxExt_msgBoxResultEvent(object sender, MsgBoxResultEventArgs e)
        {
            if (e.resultButton == DialogResult.None)
            {
                msgBoxConfirmAnswer = false;
            }

            if (e.resultButton == DialogResult.None || e.resultButton == DialogResult.No)
            {
                idleTime = true;
            }
        }

        private void btn_start_Click(object sender, EventArgs e)
        {
            if (paused)
            {
                timer1.Enabled = false;
                timer1.Enabled = true;
                start_timer();
            }
            else
            {
                stop_timer();
            }
        }

        private void start_timer()
        {
            paused = false;
            btn_start.Text = "Stop";
            textNotes.Enabled = false;
            time_start = DateTime.Now;
            label_time.Text = "00:00:00";

            int rowCount = grid.Rows.Count;

            try
            {
                for (int curRow = 0; curRow < rowCount; curRow++)
                {
                    String clmn_notes = (String)grid["clmn_notes", curRow].Value;

                    if (textNotes.Text == clmn_notes)
                    {
                        string stringTime = (String)grid["clmn_timer", curRow].Value;
                        string[] timerValues = stringTime.Split(':');

                        TimeSpan timerSpan = new TimeSpan(Int32.Parse(timerValues[0]), Int32.Parse(timerValues[1]), Int32.Parse(timerValues[2]));

                        time_start = time_start - timerSpan;

                        if (msgBoxConfirmAnswer == false)
                        {
                            time_start = time_start - idleTimeCorrection;
                            msgBoxConfirmAnswer = true;
                        }

                        label_time.Text = time_start.ToString("HH:mm:ss");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void stop_timer()
        {
            paused = true;
            //timer.Enabled = false;
            add_record_to_grid();
            //save_records_to_file();

            if (textNotes.Text != "Idle Time")
            {
                textNotes.Text = "Idle Time";
                start_timer();
                return;
            }

            textNotes.Enabled = true;
            label_time.Text = "00:00:00";
            btn_start.Text = "Start";
            textNotes.Focus();
        }

        private void save_records_to_file()
        {
            try
            {
                File.Delete(notesFileName);

                for (int curRow = 0; curRow < grid.Rows.Count; curRow++)
                {
                    File.AppendAllText(notesFileName, grid["Clmn_start", curRow].Value + "\t"
                                    + grid["clmn_notes", curRow].Value + "\t"
                                    + grid["clmn_timer", curRow].Value
                                    + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void add_record_to_grid()
        {
            bool recordNameAlreadyPresent = false;

            for (int curRow = 0; curRow < grid.Rows.Count; curRow++)
            {
                if ((string)(grid["clmn_notes", curRow].Value) == textNotes.Text)
                {
                    grid["Clmn_start", curRow].Value = time_start.ToString("yyyy-MM-dd HH:mm");                    
                    grid["clmn_notes", curRow].Value = textNotes.Text.TrimEnd();
                    grid["clmn_timer", curRow].Value = label_time.Text;

                    recordNameAlreadyPresent = true;
                }
            }

            if (!recordNameAlreadyPresent)
            {
                int row = grid.Rows.Count;
                grid.Rows.Add();
                grid["Clmn_start", row].Value = time_start.ToString("yyyy-MM-dd HH:mm");
                grid["clmn_notes", row].Value = textNotes.Text.TrimEnd();
                grid["clmn_timer", row].Value = label_time.Text;
            }
        }

        private void TimeSplitter_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!paused)
            {
                stop_timer();
            }

            save_records_to_file();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (paused)
            {
                return;
            }

            if (idleTime && textNotes.Text != "Idle Time")
            {
                idleTime = false;
                stop_timer();
            }

            TimeSpan time_duration = (DateTime.Now - time_start).Duration();
            string time_str = time_duration.ToString();

            string[] Split = time_str.ToString().Split('.');
            label_time.Text = Split[0];
        }

        private void grid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex == -1 || e.ColumnIndex != 1)
            {
                return;
            }

            add_record_to_grid();
            //save_records_to_file();
            textNotes.Text = grid.SelectedCells[0].Value.ToString();
            start_timer();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            msgBoxExt.msgBoxResultEvent += new EventHandler<MsgBoxResultEventArgs>(msgBoxExt_msgBoxResultEvent);

            //timeout in ms
            int timeout = 60000; //1 min

            // is model
            bool model = false;

            //new MsgBoxExtOptions
            MsgBoxExtOptions options = new MsgBoxExtOptions
               ("\nCurrent pastime:\t" + textNotes.Text + "\n\n" + " Do you want to continue ?",
               "Time Splitter: Pastime confirmation", MsgBoxResultReferences.EMPTY,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, timeout, model);

            //show messagebox
            msgBoxExt.Show(options);
        }

        private void textNotes_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btn_start_Click(null, null);
            }
        }

        private void load_records_from_file()
        {
            try
            {
                if (!File.Exists(notesFileName))
                {
                    MessageBox.Show("File " + notesFileName + " doesn't exist", "Error");
                    return;
                }

                string[] lines = File.ReadAllLines(notesFileName);

                foreach (string curLine in lines)
                {
                    string[] cellContent = curLine.Split('\t');
                    grid.Rows.Add(cellContent[0], cellContent[1], cellContent[2]);
                }                
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
