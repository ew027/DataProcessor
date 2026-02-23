using DataProcessor.Engine;
using System;
using System.IO;

namespace DataProcessor.Engine.Tests;

public class CsvImporterTests
{
    private static string WriteTempCsv(string content)
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void FromCsv_ReadsHeaders()
    {
        string path = WriteTempCsv("Id,Name,Score\n1,Alice,3.14\n2,Bob,\n");
        try {
            var ds = CsvImporter.FromCsv(path);
            Assert.True(ds.HasColumn("Id"));
            Assert.True(ds.HasColumn("Name"));
            Assert.True(ds.HasColumn("Score"));
        } finally { File.Delete(path); }
    }

    [Fact]
    public void FromCsv_InfersIntColumn()
    {
        string path = WriteTempCsv("x\n1\n2\n3\n");
        try {
            var ds = CsvImporter.FromCsv(path);
            Assert.Equal(ColumnType.Int, ds.GetColumn("x").ColumnType);
        } finally { File.Delete(path); }
    }

    [Fact]
    public void FromCsv_InfersDoubleColumn()
    {
        string path = WriteTempCsv("x\n1.1\n2.2\n");
        try {
            var ds = CsvImporter.FromCsv(path);
            Assert.Equal(ColumnType.Double, ds.GetColumn("x").ColumnType);
        } finally { File.Delete(path); }
    }

    [Fact]
    public void FromCsv_InfersStringColumn()
    {
        string path = WriteTempCsv("x\nhello\nworld\n");
        try {
            var ds = CsvImporter.FromCsv(path);
            Assert.Equal(ColumnType.String, ds.GetColumn("x").ColumnType);
        } finally { File.Delete(path); }
    }

    [Fact]
    public void FromCsv_MissingInt_MapsToMinValue()
    {
        string path = WriteTempCsv("x\n1\n\n3\n");
        try {
            var ds = CsvImporter.FromCsv(path);
            Assert.Equal(int.MinValue, (int)ds.GetValueObject(1, "x"));
        } finally { File.Delete(path); }
    }

    [Fact]
    public void FromCsv_MissingDouble_MapsToNaN()
    {
        string path = WriteTempCsv("x\n1.5\n\n3.5\n");
        try {
            var ds = CsvImporter.FromCsv(path);
            Assert.True(double.IsNaN((double)ds.GetValueObject(1, "x")));
        } finally { File.Delete(path); }
    }

    [Fact]
    public void FromCsv_QuotedFields_ParsedCorrectly()
    {
        string path = WriteTempCsv("name\n\"Smith, John\"\n\"Jones, Mary\"\n");
        try {
            var ds = CsvImporter.FromCsv(path);
            Assert.Equal("Smith, John", (string)ds.GetValueObject(0, "name"));
        } finally { File.Delete(path); }
    }

    [Fact]
    public void FromCsv_NoHeaders_GeneratesColumnNames()
    {
        string path = WriteTempCsv("1,2,3\n4,5,6\n");
        try {
            var ds = CsvImporter.FromCsv(path, hasHeaders: false);
            Assert.True(ds.HasColumn("Col1"));
            Assert.True(ds.HasColumn("Col2"));
            Assert.True(ds.HasColumn("Col3"));
        } finally { File.Delete(path); }
    }

    [Fact]
    public void FromCsv_RoundTrip_PreservesData()
    {
        var original = new DataStore();
        original.AddIntColumn("Id");
        original.AddStringColumn("Name");
        original.AddRow(); original.SetValueObject(0, "Id", 1); original.SetValueObject(0, "Name", "Alice");
        original.AddRow(); original.SetValueObject(1, "Id", 2); original.SetValueObject(1, "Name", "Bob");

        string path = Path.GetTempFileName();
        try {
            original.Export(path);
            var imported = CsvImporter.FromCsv(path);
            Assert.Equal(2, imported.RowCount);
            Assert.Equal(1,       (int)imported.GetValueObject(0, "Id"));
            Assert.Equal("Bob",   (string)imported.GetValueObject(1, "Name"));
        } finally { File.Delete(path); }
    }
}
