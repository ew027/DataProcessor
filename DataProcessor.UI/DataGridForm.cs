using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataProcessor
{
    public partial class DataGridForm : Form
    {
        public DataGridForm()
        {
            InitializeComponent();
        }

        private void DataGridForm_Load(object sender, EventArgs e)
        {
            // Initialize the grid with some columns
            InitializeGrid();
        }

        public void InitializeGrid()
        {
            // Clear existing columns
            dataGridView.Columns.Clear();

            // Add default columns
            dataGridView.Columns.Add("Column1", "Column 1");
            dataGridView.Columns.Add("Column2", "Column 2");
            dataGridView.Columns.Add("Column3", "Column 3");

            // Add some sample rows
            dataGridView.Rows.Add("Data 1-1", "Data 1-2", "Data 1-3");
            dataGridView.Rows.Add("Data 2-1", "Data 2-2", "Data 2-3");
        }

        // Method to load dynamic data into the grid
        public void LoadData(DataTable dataTable)
        {
            // Clear existing columns
            dataGridView.Columns.Clear();

            // Add columns based on the DataTable schema
            foreach (DataColumn column in dataTable.Columns)
            {
                dataGridView.Columns.Add(column.ColumnName, column.ColumnName);
            }

            // Add rows from the DataTable
            foreach (DataRow row in dataTable.Rows)
            {
                dataGridView.Rows.Add(row.ItemArray);
            }
        }

        // Method to load dynamic data with custom columns
        public void LoadData(List<string> columnNames, List<List<string>> rows)
        {
            // Clear existing columns
            dataGridView.Columns.Clear();

            // Add columns based on the provided names
            foreach (string columnName in columnNames)
            {
                dataGridView.Columns.Add(columnName, columnName);
            }

            // Add rows
            foreach (List<string> row in rows)
            {
                dataGridView.Rows.Add(row.ToArray());
            }
        }

        private void addColumnButton_Click(object sender, EventArgs e)
        {
            // Add a new column
            int columnCount = dataGridView.Columns.Count + 1;
            dataGridView.Columns.Add($"Column{columnCount}", $"Column {columnCount}");
        }

        private void addRowButton_Click(object sender, EventArgs e)
        {
            // Create a new row with default values
            object[] rowData = new object[dataGridView.Columns.Count];
            for (int i = 0; i < rowData.Length; i++)
            {
                rowData[i] = $"Data {dataGridView.Rows.Count + 1}-{i + 1}";
            }

            dataGridView.Rows.Add(rowData);
        }

        private void removeRowButton_Click(object sender, EventArgs e)
        {
            // Remove the selected row or the last row if none selected
            if (dataGridView.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                {
                    dataGridView.Rows.Remove(row);
                }
            }
            else if (dataGridView.Rows.Count > 0)
            {
                dataGridView.Rows.RemoveAt(dataGridView.Rows.Count - 1);
            }
        }

        private void removeColumnButton_Click(object sender, EventArgs e)
        {
            // Remove the selected column or the last column if none selected
            if (dataGridView.SelectedColumns.Count > 0)
            {
                foreach (DataGridViewColumn column in dataGridView.SelectedColumns)
                {
                    dataGridView.Columns.Remove(column);
                }
            }
            else if (dataGridView.Columns.Count > 0)
            {
                dataGridView.Columns.RemoveAt(dataGridView.Columns.Count - 1);
            }
        }
    }
}
