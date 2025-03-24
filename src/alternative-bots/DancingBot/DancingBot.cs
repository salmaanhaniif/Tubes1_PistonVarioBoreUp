using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class DancingBot : Bot
{
    private Dictionary<int, BotIntel> botIntels;
    private int targetId = 0;
    // Random kirikanan = new Random();
    bool isforward;
    int turnDirection = 1; // clockwise (-1) or counterclockwise (1)
    bool isDancing = false;
    int danceCount = 3;
    int enemiesRemaining;
    /* A bot that drives forward and backward, and fires a bullet */
    static void Main(string[] args)
    {
        new DancingBot().Start();
    }

    DancingBot() : base(BotInfo.FromFile("DancingBot.json")) { }

    public override void Run()
    {
        /* Customize bot colors, read the documentation for more information */
        BodyColor = Color.Black;
        TurretColor = Color.Black;
        RadarColor = Color.Black;
        BulletColor = Color.Red;
        ScanColor = Color.Gray;

        targetId = 0;
        botIntels = new Dictionary<int, BotIntel>();

        while (IsRunning)
        {
            // Forward(200);
            // int kanan = kirikanan.Next(0, 2);
            enemiesRemaining = EnemyCount;

            if (enemiesRemaining >= 3)
            { //enemiesRemaining >= 3
                isforward = true;
                TurnRight(200);
                SetTurnGunLeft(360 * 200);
                SetForward(200);
                Dancing();
            }
            else if (enemiesRemaining >= 2)
            {
                isDancing = false;
                TurnRight(360);
                Dancing();
            }
            else if (targetId == 0)
            {
                AdjustGunForBodyTurn = false;
                AdjustRadarForBodyTurn = false;
                TurnLeft(360);
            }
            else
            {
                AdjustRadarForBodyTurn = true;
                AdjustRadarForGunTurn = true;
                FocusScan();
                Go();
                AdjustRadarForBodyTurn = false;
                AdjustRadarForGunTurn = false;
                TurnLeft(360);
            }
            AdjustRadarForGunTurn = false;
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        if (!botIntels.ContainsKey(e.ScannedBotId))
        {
            botIntels[e.ScannedBotId] = new BotIntel(e.ScannedBotId);
        }
        botIntels[e.ScannedBotId].botHistory.AddEntry(e, TurnNumber);

        var distance = DistanceTo(e.X, e.Y);
        // FireTactics(distance);
        if (enemiesRemaining >= 4) {
            
            Fire(2);}
        else if (enemiesRemaining < 4 && !isDancing ) {
            TurnToFaceTarget(e.X, e.Y);
            if (distance > 200)
            {
                FireTactics(distance);
                Forward(150);
            }
            else { FireTactics(distance); Back(50); }
        }
        else if (enemiesRemaining < 2)
        {
            targetId = e.ScannedBotId;
            TurnToFaceTarget(e.X, e.Y);
            SetTurnGunRight(-CalcGunBearing(DirectionTo(e.X, e.Y)));
            FireTactics(distance);
            SetForward(1000);
        } else {
            FireTactics(distance);
        }
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // TurnToFaceTarget(e.X, e.Y);
        // Fire(5);
        // TurnLeft(200);
        // SetBack(200);
    }

    public override void OnHitWall(HitWallEvent e)
    {
        if (isforward)
        { SetBack(250);}
        else { SetForward(250); }
    }

    private void Dancing()//int kanan)
    {
        isDancing = true;
        for (int i = 0; i < danceCount; i++)
        {
            isforward = true;
            TurnRight(100);
            SetForward(100);
            isforward = false;
            TurnRight(100);
            SetBack(100);
        }
    }

    private void FireTactics(double distance)
    {
        if (distance <= 150){
            Fire(3); }
        else if (distance <= 500)
        { Fire(1);}
    }

    private void TurnToFaceTarget(double x, double y)
    {
        var bearing = BearingTo(x, y);
        if (bearing >= 0)
            turnDirection = 1;
        else
            turnDirection = -1;

        TurnLeft(bearing);
    }

    private void FocusScan()
    {
        if (botIntels.ContainsKey(targetId) && botIntels[targetId].botHistory.length > 0)
        {
            BodyColor = Color.Red;
            BotHistoryEntry lastEntry = botIntels[targetId].botHistory.GetMostRecentEntry();
            double direction = DirectionTo(lastEntry.Location.X, lastEntry.Location.Y);
            if (CalcRadarBearing(direction) < 0)
            {
                SetTurnRadarRight((-CalcRadarBearing(direction) + 22.5) % 360);
            }
            else
            {
                SetTurnRadarRight((-CalcRadarBearing(direction) - 22.5) % 360);
            }
        }
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
        }
        else
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