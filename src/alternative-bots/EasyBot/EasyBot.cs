using System;
using System.Collections.Generic;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class EasyBot : Bot
{   
    private enum ScanMode
    {
        Radar,
        Search,
        Focus,
        Meta
    }

    private const double TOLERANCE = 0.01;

    private Dictionary<int, BotIntel> botIntels;

    private ScanMode currentScanMode;
    private bool isRadarScanning;
    private int lastRadarScan = 0;
    private int radarRate = 75;
    private bool isSearchScanning;
    private double MetaField = 80;
    private int MetaDirection = 1;

    private int targetId = 0;

    static void Main(string[] args)
    {
        new EasyBot().Start();
    }

    EasyBot() : base(BotInfo.FromFile("EasyBot.json")) { }

    public override void Run()
    {
        /* Customize bot colors, read the documentation for more information */
        BodyColor = Color.Gray;

        botIntels = new Dictionary<int, BotIntel>();

        currentScanMode = ScanMode.Radar;
        isRadarScanning = false;
        isSearchScanning = false;

        while (IsRunning)
        {
            HandleScan();
            
            Go();
        }
    }

    private void HandleScan()
    {
        switch (currentScanMode) {
            case ScanMode.Radar:
                RadarScan();
                break;
            case ScanMode.Focus:
                FocusScan();
                break;
            case ScanMode.Search:
                break;
            case ScanMode.Meta:
                MetaScan();
                break;
        }
    }

    private void RadarScan()
    {
        if (isRadarScanning) {
            if (RadarTurnRate == 0) {
                isRadarScanning = false;
                currentScanMode = ScanMode.Meta;
            }
        } else {
            isRadarScanning = true;
            lastRadarScan = TurnNumber;
            SetTurnRadarRight(360);
        }
    }

    private void FocusScan()
    {
        if (botIntels.ContainsKey(targetId) && botIntels[targetId].botHistory.length > 0)
        { 
            BotHistoryEntry lastEntry = botIntels[targetId].botHistory.GetMostRecentEntry();
            double direction = DirectionTo(lastEntry.Location.X, lastEntry.Location.Y);
            if (CalcRadarBearing(direction) < 0) 
            {
                SetTurnRadarRight((-CalcRadarBearing(direction)+22.5)%360);
            } 
            else 
            {
                SetTurnRadarRight((-CalcRadarBearing(direction)-22.5)%360);
            }
        } 
        else
        {
            currentScanMode = ScanMode.Radar;
        }
    }

    private void SearchScan()
    {
        SetTurnRadarRight(360);
    }

    private void MetaScan()
    {
        if (botIntels.ContainsKey(targetId) && botIntels[targetId].botHistory.length > 0)
        { 
            BotHistoryEntry lastEntry = botIntels[targetId].botHistory.GetMostRecentEntry();
            double direction = DirectionTo(lastEntry.Location.X, lastEntry.Location.Y);
            SetTurnRadarRight((-CalcRadarBearing(direction)+MetaDirection*MetaField)%360);
            if (CalcRadarBearing(direction) >= MetaField - TOLERANCE)
            {
                MetaDirection = -1;
            }
            else if (CalcRadarBearing(direction) <= -MetaField + TOLERANCE)
            {
                MetaDirection = 1;
            } 
            else if (Math.Abs(CalcRadarBearing(direction)) <= TOLERANCE)
            {
                MetaDirection *= -1;
            }

            if (TurnNumber - lastRadarScan > radarRate)
            {
                currentScanMode = ScanMode.Radar;
            }
        } 
        else
        {
            currentScanMode = ScanMode.Radar;
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        if (!botIntels.ContainsKey(e.ScannedBotId))
        {
            botIntels[e.ScannedBotId] = new BotIntel(e.ScannedBotId);
        }
        botIntels[e.ScannedBotId].botHistory.AddEntry(e, TurnNumber);
        if (targetId == 0) targetId = e.ScannedBotId;
        if (currentScanMode == ScanMode.Search && targetId == e.ScannedBotId) currentScanMode = ScanMode.Meta;
    }

    public override void OnHitBot(HitBotEvent e)
    {
        Console.WriteLine("Ouch! I hit a bot at " + e.X + ", " + e.Y);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        Console.WriteLine("Ouch! I hit a wall, must turn back!");
    }
}

class BotIntel
{
    public int botId;
    public BotHistory botHistory;

    public BotIntel(int id)
    {
        botId = id;
        botHistory = new BotHistory();
    }
}

class BotHistory
{
    public BotHistoryEntry[] history;
    public int length;

    public BotHistory()
    {
        history = new BotHistoryEntry[5];
        length = 0;
    }

    public void AddEntry(ScannedBotEvent e, int time)
    {
        BotHistoryEntry newEntry = new BotHistoryEntry(time, e.Energy, e.X, e.Y, e.Direction, e.Speed);
        if (length < history.Length)
        {
            history[length] = newEntry;
            length++;
        } else
        {
            for (int i = 0; i < history.Length - 1; i++)
            {
                history[i] = history[i + 1];
            }
            history[history.Length - 1] = newEntry;
        }
    }

    public void ResetHistory()
    {
        length = 0;
    }

    public BotHistoryEntry GetMostRecentEntry()
    {
        return history[length - 1];
    }
}

class BotHistoryEntry 
{
    public int Time;
    public double Energy;
    public Point Location;
    public double Direction;
    public double Speed;

    public BotHistoryEntry(int time, double energy, double x, double y, double direction, double speed) 
    { 
        this.Time = time;
        this.Energy = energy;
        this.Location = new Point(x, y);
        this.Direction = direction;
        this.Speed = speed;
    }
}

class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }
}