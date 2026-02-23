using DataProcessor.Engine;
using System;
using System.Collections.Generic;

namespace DataProcessor.Engine.Tests;

/// <summary>
/// Tests for key Engine operations using DataStore.
/// </summary>
public class EngineOperationTests
{
    private static Engine BuildEngine(DataStore ds, string dsName = "test")
    {
        var engine = new Engine();
        engine.AddDataStore(ds, dsName);
        return engine;
    }

    private static DataStore MakeIntDs(string varName, params int[] values)
    {
        var ds = new DataStore();
        ds.AddIntColumn(varName);
        foreach (var v in values) {
            int idx = ds.AddRow();
            ds.SetValueObject(idx, varName, v);
        }
        return ds;
    }

    // ── Recode ───────────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteRecode_BasicRecode()
    {
        var ds = MakeIntDs("q1", 1, 2, 3, 1, 2);
        var engine = BuildEngine(ds);

        engine.AddOperation("Recode|test|q1|1=2;2=1;3=3", "script");
        engine.Execute("script");

        Assert.Equal(2, (int)ds.GetValueObject(0, "q1"));
        Assert.Equal(1, (int)ds.GetValueObject(1, "q1"));
        Assert.Equal(3, (int)ds.GetValueObject(2, "q1"));
        Assert.Equal(2, (int)ds.GetValueObject(3, "q1"));
    }

    [Fact]
    public void ExecuteRecode_ElseRecode()
    {
        var ds = MakeIntDs("q1", 1, 2, 5);
        var engine = BuildEngine(ds);

        engine.AddOperation("Recode|test|q1|1=10;else=99", "script");
        engine.Execute("script");

        Assert.Equal(10, (int)ds.GetValueObject(0, "q1"));
        Assert.Equal(99, (int)ds.GetValueObject(1, "q1"));
        Assert.Equal(99, (int)ds.GetValueObject(2, "q1"));
    }

    // ── RecodeInto ────────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteRecodeInto_CreatesNewVar()
    {
        var ds = MakeIntDs("raw", 1, 2, 3);
        var engine = BuildEngine(ds);

        engine.AddOperation("RecodeInto|test|grouped|raw|1=1;2=1;3=2;else=0", "script");
        engine.Execute("script");

        Assert.True(ds.HasColumn("grouped"));
        Assert.Equal(1, (int)ds.GetValueObject(0, "grouped"));
        Assert.Equal(1, (int)ds.GetValueObject(1, "grouped"));
        Assert.Equal(2, (int)ds.GetValueObject(2, "grouped"));
    }

    // ── RecodeRange ───────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteRecodeRange_RangesApplied()
    {
        var ds = MakeIntDs("score", 10, 50, 90, 30);
        var engine = BuildEngine(ds);

        engine.AddOperation("RecodeRange|test|band|score|min~33=1;34~66=2;67~max=3", "script");
        engine.Execute("script");

        Assert.True(ds.HasColumn("band"));
        Assert.Equal(1, (int)ds.GetValueObject(0, "band")); // 10
        Assert.Equal(2, (int)ds.GetValueObject(1, "band")); // 50
        Assert.Equal(3, (int)ds.GetValueObject(2, "band")); // 90
        Assert.Equal(1, (int)ds.GetValueObject(3, "band")); // 30
    }

    // ── Sort ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteSort_SortsAscending()
    {
        var ds = new DataStore();
        ds.AddIntColumn("id");
        ds.AddIntColumn("val");
        int r0 = ds.AddRow(); ds.SetValueObject(r0, "id", 3); ds.SetValueObject(r0, "val", 30);
        int r1 = ds.AddRow(); ds.SetValueObject(r1, "id", 1); ds.SetValueObject(r1, "val", 10);
        int r2 = ds.AddRow(); ds.SetValueObject(r2, "id", 2); ds.SetValueObject(r2, "val", 20);

        var engine = BuildEngine(ds);
        engine.AddOperation("Sort|test|id", "script");
        engine.Execute("script");

        Assert.Equal(1,  (int)ds.GetValueObject(0, "id"));
        Assert.Equal(10, (int)ds.GetValueObject(0, "val"));
        Assert.Equal(2,  (int)ds.GetValueObject(1, "id"));
        Assert.Equal(3,  (int)ds.GetValueObject(2, "id"));
    }

    // ── ComputeSum ────────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteComputeSum_SumsCorrectly()
    {
        var ds = new DataStore();
        ds.AddIntColumn("a"); ds.AddIntColumn("b"); ds.AddIntColumn("c");
        int r0 = ds.AddRow();
        ds.SetValueObject(r0, "a", 10); ds.SetValueObject(r0, "b", 20); ds.SetValueObject(r0, "c", 5);

        var engine = BuildEngine(ds);
        engine.AddOperation("ComputeSum|test|total|a,b,c", "script");
        engine.Execute("script");

        Assert.Equal(35, (int)ds.GetValueObject(0, "total"));
    }

    [Fact]
    public void ExecuteComputeSum_IgnoresMissing()
    {
        var ds = new DataStore();
        ds.AddIntColumn("a"); ds.AddIntColumn("b");
        int r0 = ds.AddRow();
        ds.SetValueObject(r0, "a", 10);
        ds.SetValueObject(r0, "b", int.MinValue); // missing

        var engine = BuildEngine(ds);
        engine.AddOperation("ComputeSum|test|total|a,b", "script");
        engine.Execute("script");

        Assert.Equal(10, (int)ds.GetValueObject(0, "total"));
    }

    // ── KeepVars ──────────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteKeepVars_RemovesOtherColumns()
    {
        var ds = new DataStore();
        ds.AddIntColumn("keep1"); ds.AddIntColumn("remove"); ds.AddIntColumn("keep2");
        ds.AddRow();
        ds.SetValueObject(0, "keep1", 1); ds.SetValueObject(0, "remove", 2); ds.SetValueObject(0, "keep2", 3);

        var engine = BuildEngine(ds);
        engine.AddOperation("KeepVars|test|keep1,keep2", "script");
        engine.Execute("script");

        var updated = engine.GetDataStore("test");
        Assert.True(updated.HasColumn("keep1"));
        Assert.True(updated.HasColumn("keep2"));
        Assert.False(updated.HasColumn("remove"));
    }

    [Fact]
    public void ExecuteKeepVars_CreateNewArray()
    {
        var ds = new DataStore();
        ds.AddIntColumn("a"); ds.AddIntColumn("b");
        ds.AddRow(); ds.SetValueObject(0, "a", 5); ds.SetValueObject(0, "b", 10);

        var engine = BuildEngine(ds);
        engine.AddOperation("KeepVars|test|a|newDs", "script");
        engine.Execute("script");

        var newDs = engine.GetDataStore("newDs");
        Assert.True(newDs.HasColumn("a"));
        Assert.False(newDs.HasColumn("b"));
        Assert.Equal(5, (int)newDs.GetValueObject(0, "a"));
    }

    // ── RemoveRecords ─────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteRemoveRecords_RemovesFlaggedRows()
    {
        var ds = new DataStore();
        ds.AddIntColumn("id"); ds.AddIntColumn("remove_flag");
        int r0 = ds.AddRow(); ds.SetValueObject(r0, "id", 1); ds.SetValueObject(r0, "remove_flag", 0);
        int r1 = ds.AddRow(); ds.SetValueObject(r1, "id", 2); ds.SetValueObject(r1, "remove_flag", 1);
        int r2 = ds.AddRow(); ds.SetValueObject(r2, "id", 3); ds.SetValueObject(r2, "remove_flag", 0);

        var engine = BuildEngine(ds);
        engine.AddOperation("RemoveRecords|test|remove_flag", "script");
        engine.Execute("script");

        Assert.Equal(2, ds.RowCount);
        Assert.Equal(1, (int)ds.GetValueObject(0, "id"));
        Assert.Equal(3, (int)ds.GetValueObject(1, "id"));
    }

    // ── CreateIntVar ──────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteCreateIntVar_FillsAllRows()
    {
        var ds = MakeIntDs("x", 1, 2, 3);
        var engine = BuildEngine(ds);

        engine.AddOperation("CreateIntVar|test|flag|42", "script");
        engine.Execute("script");

        Assert.Equal(42, (int)ds.GetValueObject(0, "flag"));
        Assert.Equal(42, (int)ds.GetValueObject(1, "flag"));
        Assert.Equal(42, (int)ds.GetValueObject(2, "flag"));
    }

    // ── AddVariableLabel ──────────────────────────────────────────────────────

    [Fact]
    public void ExecuteAddVariableLabel_SetsDescription()
    {
        var ds = MakeIntDs("q1", 1);
        var engine = BuildEngine(ds);

        engine.AddOperation("AddVariableLabel|test|q1|My Label Here", "script");
        engine.Execute("script");

        Assert.Equal("My Label Here", ds.GetColumn("q1").Description);
    }

    // ── AddValueLabels ────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteAddValueLabels_SetsLabels()
    {
        var ds = MakeIntDs("cat", 1, 2);
        var engine = BuildEngine(ds);

        engine.AddOperation("AddValueLabels|test|cat|1=Agree;2=Disagree", "script");
        engine.Execute("script");

        var labels = ds.GetColumn("cat").Labels;
        Assert.NotNull(labels);
        Assert.Equal("Agree",    labels![1]);
        Assert.Equal("Disagree", labels![2]);
    }

    // ── Error handling ────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteRecode_MissingDataset_LogsError()
    {
        var engine = new Engine();
        engine.AddOperation("Recode|nonexistent|x|1=2", "script");
        engine.Execute("script");
        Assert.True(engine.CheckErrorStatus());
    }
}
