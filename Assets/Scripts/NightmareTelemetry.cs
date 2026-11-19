using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class NightmareTelemetry
{
    [Serializable]
    public sealed class Event
    {
        public float elapsed;
        public string kind;
        public string detail;
        public float value;
    }

    [Serializable]
    public sealed class Report
    {
        public string session;
        public string profile;
        public string outcome;
        public float duration;
        public float firstEncounter = -1f;
        public int pursuits;
        public int looksUsed;
        public int deaths;
        public int victories;
        public float keypadSeconds;
        public int wrongSubmissions;
        public List<float> clueIntervals = new();
        public List<Event> timeline = new();
    }

    public Report Data { get; private set; }
    public bool Started => Data != null;
    private double startedAt;
    private float previousClue;
    private float keypadStart = -1f;
    private float lastPursuitEnd = -10f;
    private bool pursuing;

    private float Now => (float)(Time.timeAsDouble - startedAt);

    public void Begin(NightmareDifficulty profile)
    {
        if (Started)
            return;
        startedAt = Time.timeAsDouble;
        Data = new Report { session = Guid.NewGuid().ToString("N"), profile = profile.ToString() };
        Record("play");
    }

    public void Record(string kind, string detail = "", float value = 0f)
    {
        if (!Started || Data.outcome != null)
            return;
        Data.timeline.Add(new Event { elapsed = Now, kind = kind, detail = detail, value = value });
    }

    public void Observe(Creature.BehaviourState state, bool encounter)
    {
        if (!Started || Data.outcome != null)
            return;
        if (encounter && Data.firstEncounter < 0f)
        {
            Data.firstEncounter = Now;
            Record("first_encounter");
        }

        bool chase = state is Creature.BehaviourState.Pursuing or Creature.BehaviourState.Approaching or Creature.BehaviourState.Rushing;
        if (chase && !pursuing && Now - lastPursuitEnd > 0.9f)
        {
            Data.pursuits++;
            Record("pursuit");
        }
        if (!chase && pursuing)
            lastPursuitEnd = Now;
        pursuing = chase;
    }

    public void Clue(int index)
    {
        if (!Started || Data.outcome != null)
            return;
        float elapsed = Now;
        Data.clueIntervals.Add(elapsed - previousClue);
        previousClue = elapsed;
        Record("clue", (index + 1).ToString());
    }

    public void Look(int remaining)
    {
        if (!Started || Data.outcome != null)
            return;
        Data.looksUsed++;
        Record("look", "", remaining);
    }

    public void Keypad(Keypad.ActionKind action)
    {
        if (!Started || Data.outcome != null)
            return;
        if (keypadStart < 0f && action != global::Keypad.ActionKind.Cleared)
            keypadStart = Now;
        if (action == global::Keypad.ActionKind.Rejected)
            Data.wrongSubmissions++;
        if (action != global::Keypad.ActionKind.Digit)
            Record("keypad", action.ToString());
        if (action == global::Keypad.ActionKind.Accepted)
            FinishKeypadTime();
    }

    private void FinishKeypadTime()
    {
        if (keypadStart < 0f)
            return;
        Data.keypadSeconds = Now - keypadStart;
        keypadStart = -1f;
    }

    public void Complete(string outcome)
    {
        if (!Started || Data.outcome != null)
            return;
        FinishKeypadTime();
        Record("finish", outcome);
        Data.outcome = outcome;
        Data.duration = Now;
        Data.deaths = outcome == "death" ? 1 : 0;
        Data.victories = outcome == "victory" ? 1 : 0;

        bool editor = Application.isEditor;
        if (!editor && !Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-nightmareTelemetry"))
            return;
        try
        {
            string folder = editor ? Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/NightmareTelemetry")) :
                Path.Combine(Application.persistentDataPath, "NightmareTelemetry");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, Data.session + ".json");
            File.WriteAllText(file + ".tmp", JsonUtility.ToJson(Data, true));
            File.Move(file + ".tmp", file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Debug.LogWarning("No se pudo guardar la sesión de pruebas. " + exception.Message);
        }
    }
}
