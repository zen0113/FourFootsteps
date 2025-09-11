using System.Collections.Generic;

public class EventLine
{
    public string Logic { get; private set; }
    public List<Condition> Conditions { get; private set; }
    public List<Result> Results { get; private set; }

    public string ExecutionMode { get; private set; }

    // initialize function
    public EventLine(string logic, List<Condition> conditions, List<Result> results, string executionMode)
    {
        Logic = logic;
        Conditions = conditions;
        Results = results;
        ExecutionMode = executionMode;
    }
}