using System;
using UnityEngine;

[Serializable]
public class EndingLogPayload
{
    public string apiKey;
    public string eventId;

    public string playerKey;
    public string playerName;
    public string catName;

    public string endingType;
    public string memoryPuzzleStatesJson;
}

[Serializable]
public class PendingLogQueue
{
    public EndingLogPayload[] items = Array.Empty<EndingLogPayload>();
}
