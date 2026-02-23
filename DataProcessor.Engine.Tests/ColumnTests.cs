using DataProcessor.Engine;
using System;

namespace DataProcessor.Engine.Tests;

public class ColumnTests
{
    [Fact]
    public void ColumnType_Int_ReturnsInt()
    {
        var col = new Column<int>("x");
        Assert.Equal(ColumnType.Int, col.ColumnType);
    }

    [Fact]
    public void ColumnType_Double_ReturnsDouble()
    {
        var col = new Column<double>("y");
        Assert.Equal(ColumnType.Double, col.ColumnType);
    }

    [Fact]
    public void ColumnType_String_ReturnsString()
    {
        var col = new Column<string>("s");
        Assert.Equal(ColumnType.String, col.ColumnType);
    }

    [Fact]
    public void AddLabel_SetsIsCategorical()
    {
        var col = new Column<int>("cat");
        col.AddLabel(1, "Yes");
        Assert.True(col.IsCategorical);
        Assert.Equal("Yes", col.Labels![1]);
    }

    [Fact]
    public void AddLabel_NoNullRef_WhenCalledTwice()
    {
        var col = new Column<int>("x");
        col.AddLabel(1, "One");
        col.AddLabel(2, "Two"); // must not throw
        Assert.Equal(2, col.Labels!.Count);
    }

    [Fact]
    public void AddLabel_OverwritesDuplicate()
    {
        var col = new Column<int>("x");
        col.AddLabel(1, "First");
        col.AddLabel(1, "Second"); // overwrite, must not throw
        Assert.Equal("Second", col.Labels![1]);
    }

    [Fact]
    public void RemoveLabel_DoesNotThrow_WhenNull()
    {
        var col = new Column<int>("x");
        var ex = Record.Exception(() => col.RemoveLabel(1));
        Assert.Null(ex); // no exception
    }

    [Fact]
    public void RemoveLabel_RemovesCorrectEntry()
    {
        var col = new Column<int>("x");
        col.AddLabel(1, "One");
        col.AddLabel(2, "Two");
        col.RemoveLabel(1);
        Assert.False(col.Labels!.ContainsKey(1));
        Assert.True(col.Labels!.ContainsKey(2));
    }

    [Fact]
    public void Clone_PreservesMetadata_NotDouble()
    {
        var col = new Column<int>("cats");
        col.Description = "My Category";
        col.AddLabel(1, "Yes");
        col.AddLabel(2, "No");

        var cloned = col.Clone(false);
        Assert.Equal("cats", cloned.Name);
        Assert.Equal("My Category", cloned.Description);
        Assert.True(cloned.IsCategorical);
        Assert.Equal("Yes", cloned.Labels![1]);
        Assert.Equal(ColumnType.Int, cloned.ColumnType);
    }

    [Fact]
    public void Clone_ConvertToDouble_ChangesType()
    {
        var col = new Column<int>("x");
        col.Description = "Score";
        var cloned = col.Clone(true);
        Assert.Equal(ColumnType.Double, cloned.ColumnType);
        Assert.Equal("Score", cloned.Description);
    }

    [Fact]
    public void Reorder_ReordersValues()
    {
        var col = new Column<int>("x");
        col.AddRow(); col.SetValue(0, 30);
        col.AddRow(); col.SetValue(1, 10);
        col.AddRow(); col.SetValue(2, 20);

        col.Reorder(new[] { 1, 2, 0 }); // should give 10, 20, 30
        Assert.Equal(10, col.GetValue(0));
        Assert.Equal(20, col.GetValue(1));
        Assert.Equal(30, col.GetValue(2));
    }

    [Fact]
    public void SetValueFromObject_ConvertsCompatibleType()
    {
        var col = new Column<double>("d");
        col.AddRow();
        col.SetValueFromObject(0, 3.14f); // float → double
        Assert.Equal(3.14, col.GetValue(0), 5);
    }
}
