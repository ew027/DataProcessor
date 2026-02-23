using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DataProcessor.Engine;

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
            InitializeGrid();
            EngineContext.ExecutionCompleted += OnExecutionCompleted;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            EngineContext.ExecutionCompleted -= OnExecutionCompleted;
            base.OnFormClosed(e);
        }

        private void OnExecutionCompleted(object? sender, ExecutionCompletedEventArgs e)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnExecutionCompleted(sender, e))); return; }

            // Load the first dataset available
            if (e.DataStoreNames.Count > 0)
            {
                var ds = EngineContext.Engine.GetDataStore(e.DataStoreNames[0]);
                LoadData(ds);
            }
        }

        public void InitializeGrid()
        {
            dataGridView.Columns.Clear();
            dataGridView.Rows.Clear();
        }

        /// <summary>Loads a DataStore into the grid.</summary>
        public void LoadData(DataStore ds)
        {
            dataGridView.Columns.Clear();
            dataGridView.Rows.Clear();

            foreach (var name in ds.ColumnNames)
                dataGridView.Columns.Add(name, name);

            for (int r = 0; r < ds.RowCount; r++)
            {
                var row = new object[ds.ColumnNames.Count];
                for (int c = 0; c < ds.ColumnNames.Count; c++)
                {
                    object val = ds.GetValueObject(r, ds.ColumnNames[c]);
                    // Display missing values as empty string
                    if (val is int iv && iv == int.MinValue) row[c] = "";
                    else if (val is double dv && double.IsNaN(dv)) row[c] = "";
                    else row[c] = val;
                }
                dataGridView.Rows.Add(row);
            }
        }

        /// <summary>Loads data from a System.Data.DataTable (kept for compatibility).</summary>
        public void LoadData(System.Data.DataTable dataTable)
        {
            dataGridView.Columns.Clear();
            foreach (System.Data.DataColumn column in dataTable.Columns)
                dataGridView.Columns.Add(column.ColumnName, column.ColumnName);
            foreach (System.Data.DataRow row in dataTable.Rows)
                dataGridView.Rows.Add(row.ItemArray);
        }

        public void LoadData(List<string> columnNames, List<List<string>> rows)
        {
            dataGridView.Columns.Clear();
            foreach (string columnName in columnNames)
                dataGridView.Columns.Add(columnName, columnName);
            foreach (List<string> row in rows)
                dataGridView.Rows.Add(row.ToArray());
        }

        private void addColumnButton_Click(object sender, EventArgs e)
        {
            int columnCount = dataGridView.Columns.Count + 1;
            dataGridView.Columns.Add($"Column{columnCount}", $"Column {columnCount}");
        }

        private void addRowButton_Click(object sender, EventArgs e)
        {
            object[] rowData = new object[dataGridView.Columns.Count];
            for (int i = 0; i < rowData.Length; i++)
                rowData[i] = $"Data {dataGridView.Rows.Count + 1}-{i + 1}";
            dataGridView.Rows.Add(rowData);
        }

        private void removeRowButton_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                    dataGridView.Rows.Remove(row);
            }
            else if (dataGridView.Rows.Count > 0)
            {
                dataGridView.Rows.RemoveAt(dataGridView.Rows.Count - 1);
            }
        }

        private void removeColumnButton_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedColumns.Count > 0)
            {
                foreach (DataGridViewColumn column in dataGridView.SelectedColumns)
                    dataGridView.Columns.Remove(column);
            }
            else if (dataGridView.Columns.Count > 0)
            {
                dataGridView.Columns.RemoveAt(dataGridView.Columns.Count - 1);
            }
        }
    }
}
