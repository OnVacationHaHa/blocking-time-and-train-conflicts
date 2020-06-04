using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace train_conflicts
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        int flag;
        get_conf c;
        DataTable train_path;
        DataTable zone;

        private void 新建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            train_path = c.new_train_path();
            zone = c.new_zone();
            dataGridView1.DataSource = train_path;
            flag = 1;
        }
        private void 切换显示zonecsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (flag==1)
            {
                train_path = (DataTable)dataGridView1.DataSource;
                dataGridView1.DataSource = zone;
                flag = 2;
            }          
        }

        private void 切换显示trainpathcsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (flag==2)
            {
                zone = (DataTable)dataGridView1.DataSource;
                dataGridView1.DataSource = train_path;
                flag = 1;
            }      
        }

        private void 复制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.GetCellCount(DataGridViewElementStates.Selected) > 0)
            {
                try
                {
                    Clipboard.SetDataObject(dataGridView1.GetClipboardContent());
                }
                catch (Exception)
                {

                }
            }
        }

        private void 粘贴ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                DataGirdViewCellPaste(dataGridView1, row_index, col_index);
            }
            catch (Exception)
            {

            }
        }
        private void 删除该行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ((DataTable)dataGridView1.DataSource).Rows.RemoveAt(row_index);
            }
            catch (Exception)
            {
                
            }
        }
        private void DataGirdViewCellPaste(DataGridView p_Data, int row_index, int col_index)
        {
            try
            {
                // 获取剪切板的内容，并按行分割
                string pasteText = Clipboard.GetText();
                if (string.IsNullOrEmpty(pasteText))
                    return;
                string[] lines = pasteText.Split(new char[] { ' ', ' ' });
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line.Trim()))
                        continue;
                    // 按 Tab 分割数据
                    string[] vals = line.Split('\t');
                    if (row_index + 1 > ((DataTable)p_Data.DataSource).Rows.Count)
                    {
                        DataRow dr = ((DataTable)p_Data.DataSource).NewRow();
                        for (int i = 0; i < vals.Length; i++)
                        {
                            dr[col_index + i] = vals[i];
                        }
                    ((DataTable)p_Data.DataSource).Rows.Add(dr);
                    }
                    else
                    {
                        for (int i = 0; i < vals.Length; i++)
                        {
                            ((DataTable)p_Data.DataSource).Rows[row_index][col_index + i] = vals[i];
                        }
                    }

                }
            }
            catch
            {
                // 不处理
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.DataSource != null)
            {
                if (flag==1)
                {
                    train_path = (DataTable)dataGridView1.DataSource;
                }
                if (flag==2)
                {
                    zone = (DataTable)dataGridView1.DataSource;
                }
                for (int i = 0; i < train_path.Rows.Count; i++)
                {
                    if (train_path.Rows[i][9]==DBNull.Value)
                    {
                        train_path.Rows.RemoveAt(i);
                        i--;
                    }
                }
                for (int i = 0; i < zone.Rows.Count; i++)
                {
                    if (zone.Rows[i][3] == DBNull.Value)
                    {
                        zone.Rows.RemoveAt(i);
                        i--;
                    }
                }
                c.main(train_path, zone);
                MessageBox.Show("计算完毕");
            }
            else
            {
                MessageBox.Show("请打开或输入数据!!!");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            c = new get_conf();
        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderSelectDialog fd = new FolderSelectDialog();
            fd.Title = "请选择输入文件存放路径";
            bool is_show = fd.ShowDialog();
            while (is_show == false)
            {
                MessageBox.Show("请选择输入文件!!!");
                is_show = fd.ShowDialog();
            }
            string input_file_str = fd.FileName;
            train_path = c.read_file(input_file_str + "\\train_path.csv");
            if (train_path==null)
            {
                MessageBox.Show("文件打开错误！！！");
                return;
            }
            zone = c.read_file(input_file_str + "\\zone.csv");
            dataGridView1.DataSource = train_path;
            flag = 1;
        }
        int row_index, col_index;
        private void dataGridView1_CellClick_1(object sender, DataGridViewCellEventArgs e)
        {
            row_index = e.RowIndex;
            col_index = e.ColumnIndex;
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if(Control.ModifierKeys == Keys.Control && e.KeyCode == Keys.V)
            {
                if (sender != null && sender.GetType() == typeof(DataGridView))
                {
                    DataGirdViewCellPaste(dataGridView1, row_index, col_index);
                }

            }
        }

        

        private void DataGridViewEnableCopy(DataGridView dataGridView1)
        {
            Clipboard.SetData(DataFormats.Text, dataGridView1.GetClipboardContent());
        }
    }
}
