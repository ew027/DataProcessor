using DataProcessor.Engine;
using System;
using System.IO;
using System.Collections.Generic;

namespace DataProcessor.Engine.Tests;

public class DataStoreTests
{
    // ── Column add / get / set ──────────────────────────────────────────────

    [Fact]
    public void AddIntColumn_HasColumn_True()
    {
        var ds = new DataStore();
        ds.AddIntColumn("Age");
        Assert.True(ds.HasColumn("Age"));
    }

    [Fact]
    public void AddIntColumn_CaseInsensitiveLookup()
    {
        var ds = new DataStore();
        ds.AddIntColumn("Age");
        Assert.True(ds.HasColumn("age"));
        Assert.True(ds.HasColumn("AGE"));
    }

    [Fact]
    public void AddRow_IncreasesRowCount()
    {
        var ds = new DataStore();
        ds.AddIntColumn("x");
        ds.AddRow();
        ds.AddRow();
        Assert.Equal(2, ds.RowCount);
    }

    [Fact]
    public void SetGetValue_RoundTrip()
    {
        var ds = new DataStore();
        ds.AddIntColumn("Score");
        ds.AddRow();
        ds.SetValueObject(0, "Score", 42);
        Assert.Equal(42, (int)ds.GetValueObject(0, "Score"));
    }

    [Fact]
    public void AddIntColumn_DefaultsMissingValue()
    {
        var ds = new DataStore();
        ds.AddRow(); // add row before column
        // Adding a column after rows: column should have one default row
        var col = ds.AddIntColumn("x");
        Assert.Equal(1, col.Count);
        Assert.Equal(0, col.GetValue(0)); // default int = 0
    }

    [Fact]
    public void AddIntColumnWithData_FillsRows()
    {
        var ds = new DataStore();
        ds.AddRow();
        ds.AddRow();
        ds.AddIntColumnWithData("Flag", 99);
        Assert.Equal(99, (int)ds.GetValueObject(0, "Flag"));
        Assert.Equal(99, (int)ds.GetValueObject(1, "Flag"));
    }

    [Fact]
    public void RemoveRow_DecreasesRowCount()
    {
        var ds = new DataStore();
        ds.AddIntColumn("x");
        ds.AddRow(); ds.AddRow(); ds.AddRow();
        ds.SetValueObject(1, "x", 55);
        ds.RemoveRow(0);
        Assert.Equal(2, ds.RowCount);
        Assert.Equal(55, (int)ds.GetValueObject(0, "x"));
    }

    [Fact]
    public void Extract_ReturnsSubsetColumns()
    {
        var ds = new DataStore();
        ds.AddIntColumn("A");
        ds.AddIntColumn("B");
        ds.AddIntColumn("C");
        ds.AddRow();
        ds.SetValueObject(0, "A", 1);
        ds.SetValueObject(0, "B", 2);
        ds.SetValueObject(0, "C", 3);

        var sub = ds.Extract(new[] { "A", "C" });
        Assert.True(sub.HasColumn("A"));
        Assert.True(sub.HasColumn("C"));
        Assert.False(sub.HasColumn("B"));
        Assert.Equal(1, (int)sub.GetValueObject(0, "A"));
        Assert.Equal(3, (int)sub.GetValueObject(0, "C"));
    }

    [Fact]
    public void ReorderRows_SortsCorrectly()
    {
        var ds = new DataStore();
        ds.AddIntColumn("x");
        ds.AddRow(); ds.SetValueObject(0, "x", 30);
        ds.AddRow(); ds.SetValueObject(1, "x", 10);
        ds.AddRow(); ds.SetValueObject(2, "x", 20);

        ds.ReorderRows(new[] { 1, 2, 0 }); // 10,20,30
        Assert.Equal(10, (int)ds.GetValueObject(0, "x"));
        Assert.Equal(20, (int)ds.GetValueObject(1, "x"));
        Assert.Equal(30, (int)ds.GetValueObject(2, "x"));
    }

    [Fact]
    public void RenameColumn_UpdatesLookup()
    {
        var ds = new DataStore();
        ds.AddIntColumn("OldName");
        ds.AddRow();
        ds.SetValueObject(0, "OldName", 7);
        ds.RenameColumn("OldName", "NewName");
        Assert.False(ds.HasColumn("OldName"));
        Assert.True(ds.HasColumn("NewName"));
        Assert.Equal(7, (int)ds.GetValueObject(0, "NewName"));
    }

    [Fact]
    public void RenameColumn_PreservesColumnOrder()
    {
        var ds = new DataStore();
        ds.AddIntColumn("A");
        ds.AddIntColumn("B");
        ds.AddIntColumn("C");
        ds.RenameColumn("B", "X");
        Assert.Equal(new[] { "A", "X", "C" }, ds.ColumnNames);
    }

    [Fact]
    public void ColumnNames_PreservesInsertionOrder()
    {
        var ds = new DataStore();
        ds.AddIntColumn("First");
        ds.AddDoubleColumn("Second");
        ds.AddStringColumn("Third");
        Assert.Equal(new[] { "First", "Second", "Third" }, ds.ColumnNames);
    }

    // ── CSV export / import round-trip ───────────────────────────────────────

    [Fact]
    public void Export_Import_RoundTrip()
    {
        var ds = new DataStore();
        ds.AddIntColumn("Id");
        ds.AddDoubleColumn("Score");
        ds.AddStringColumn("Name");

        ds.AddRow(); ds.SetValueObject(0, "Id", 1);   ds.SetValueObject(0, "Score", 3.14); ds.SetValueObject(0, "Name", "Alice");
        ds.AddRow(); ds.SetValueObject(1, "Id", 2);   ds.SetValueObject(1, "Score", double.NaN); ds.SetValueObject(1, "Name", "Bob");
        ds.AddRow(); ds.SetValueObject(2, "Id", int.MinValue); ds.SetValueObject(2, "Score", 2.72); ds.SetValueObject(2, "Name", "Carol");

        string path = Path.GetTempFileName();
        try {
            ds.Export(path);
            var imported = CsvImporter.FromCsv(path);

            Assert.Equal(3, imported.RowCount);
            Assert.Equal(1,          (int)imported.GetValueObject(0, "Id"));
            Assert.Equal("Alice",    (string)imported.GetValueObject(0, "Name"));

            // Missing int → imported as int.MinValue or empty parsed
            // Missing double → imported as NaN
            double importedScore1 = (double)imported.GetValueObject(1, "Score");
            Assert.True(double.IsNaN(importedScore1));

            Assert.Equal("Carol", (string)imported.GetValueObject(2, "Name"));
        } finally {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetColumnIndex_ReturnsCorrectIndex()
    {
        var ds = new DataStore();
        ds.AddIntColumn("A");
        ds.AddIntColumn("B");
        ds.AddIntColumn("C");
        Assert.Equal(0, ds.GetColumnIndex("A"));
        Assert.Equal(1, ds.GetColumnIndex("B"));
        Assert.Equal(2, ds.GetColumnIndex("C"));
        Assert.Equal(-1, ds.GetColumnIndex("Z"));
    }
}
