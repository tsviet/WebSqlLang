﻿/* Copyright © 2017 Mykhailo Tsvietukhin. This program is released under the "GPL-3.0" lisense. Please see LICENSE for license terms. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSqlLang.LanguageImplementation;

namespace WebSqlLang
{
    public partial class MainWindow : Form
    {

        public List<IData> DataCollected = null;

        public MainWindow()
        {
            InitializeComponent();
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

            var box = new TextBox
            {
                Multiline = true,
                Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Regular, GraphicsUnit.Point, ((byte) (0))),
                Text = @"SELECT [URL, NAME] using LINKS FROM https://stackoverflow.com/questions/25688847/html-agility-pack-get-all-urls-on-page"
            };

            mainInputTabControl.TabPages[0].Controls.Add(box);

            box.Height = box.Parent.Bottom;
            box.Width = box.Parent.Width;
        }

        private void csvToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            tabControl1.TabPages[0].Controls.Clear();

            DataCollected = new List<IData>();
            var table = new DataTable();
            // Main function that will start interpretation of input text and shoving results to a table.
            var programText = mainInputTabControl.SelectedTab.Controls[0] as TextBox;

            //Generate output grid
            var grid = new DataGridView
            {
                BackgroundColor = Color.White,
                AutoSize = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AutoSizeColumnsMode = (DataGridViewAutoSizeColumnsMode) DataGridViewAutoSizeColumnMode.AllCells

            };

            List<IData> finalData = null;
            InputContainer container;
            if (Tokenizer.IsTokenizeble(programText?.Text))
            {
                container = Tokenizer.Parse(programText?.Text);
                var web = new WebRequest(container);
                web.GetHtml();
                web.PropertyChanged += (sender1, e1) =>
                {
                    //Some code from here https://stackoverflow.com/questions/13294662/propertychangedeventhandler-how-to-get-value
                    switch (e1.PropertyName)
                    {
                        case "Html":
                            var html = (sender1 as WebRequest)?.Html;
                            finalData = HtmlHelper.Parse(container, html);
                            UpdateTableAndGrid(finalData, container, grid);
                            break;
                    }
                };
            }
            else
            {
                table.Columns.Add("Error");
                table.Rows.Add("Something goes wrong! Check if your program starts with SELECT!");
                grid.DataSource = table;
                tabControl1.TabPages[0].VerticalScroll.Enabled = true;
                tabControl1.TabPages[0].Controls.Add(grid);
                tabControl1.Refresh();
            }
        }

        public DataTable ConvertToDataTable<T>(IList<T> data, InputContainer container)
        {
            //https://stackoverflow.com/questions/29898412/convert-listt-to-datatable-including-t-customclass-properties
            var properties = TypeDescriptor.GetProperties(typeof(T));
            var outputTable = new DataTable();
            if (container.ColumnsMap.FirstOrDefault().Value.Contains("*"))
            {
                foreach (PropertyDescriptor prop in properties)
                {
                    outputTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }
            }
            foreach (PropertyDescriptor prop in properties)
            {
                //Will work only for sinle method in query will need to be rebuilded when JOIN will be designed
                if (container.ColumnsMap.FirstOrDefault().Value.Contains(prop.Name.ToLower()))
                {
                    var name = prop.Name;
                    if (outputTable.Columns.Contains(prop.Name))
                    {
                        name = prop.Name + "_1";
                    }
                    outputTable.Columns.Add(name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }
            }

            foreach (var item in data)
            {
                var row = outputTable.NewRow();
                foreach (DataColumn column in outputTable.Columns)
                {
                    var prop = properties.Find(column.ColumnName.Replace("_1", ""), false);
                    row[column.ColumnName] = prop.GetValue(item) ?? DBNull.Value;
                }
                outputTable.Rows.Add(row);
            }
            return outputTable;

        }

        private void UpdateTableAndGrid(List<IData> finalData, InputContainer container, DataGridView grid)
        {
            try
            {
                var convertedList = finalData.ConvertAll(x => (Links) x);
                var resultTable = ConvertToDataTable(convertedList, container);

                grid.DataSource = resultTable;
                tabControl1.TabPages[0].VerticalScroll.Enabled = true;
                tabControl1.TabPages[0].Controls.Add(grid);
                tabControl1.Refresh();
            }
            catch (Exception)
            {
                container.Errors.Add("Current method provided doesn't exist!");
            }
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            //https://www.dotnetperls.com/savefiledialog

            var name = saveFileDialog1.FileName;
            File.WriteAllText(name, baseInputTabPage.Text);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: Add save to current file if exist or call savedialog
            
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }
    }
}
