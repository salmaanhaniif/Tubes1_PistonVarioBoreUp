using System;
using System.Collections.Generic;
using System.Numerics;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class EasyBot : Bot {   
    private enum ScanMode {
        Radar,
        Focus
    }

    private const double DOUBLE_TOLERANCE = 0.01;

    // Movement Variables
    #region Movement Variables
    private const float BORDER_DESIRE = 1f;
    private const double LAST_SEEN_LIMIT = 30;
    #endregion

    // Shooting Variables
    #region Shooting Variables
    private const double SHOOT_TOLERANCE = 3;
    private const int AIM_AGE_LIMIT = 3;
    private const double BIG_SHOOT_DISTANCE = 50;
    private double aimDirection;
    private int aimLastUpdate;

    #endregion

    // Scan Variables
    #region Scan Variables
    private ScanMode currentScanMode;

    // Radar Variables
    private Dictionary<int, BotIntel> botIntels;
    private int radarScanDirection;
    private bool isRadarCheckpoint;
    private int lastRadarScan;
    private int radarStartTurn;
    private int RADAR_HALF_LIMIT = 10;

    // Focus Variables
    private int UNSEEN_LIMIT = 15;
    private int FOCUS_LIMIT = 10;

    #endregion

    private int targetId;
    private const double WEAK_THRESHOLD = 20;

    static void Main(string[] args) {
        new EasyBot().Start();
    }

    EasyBot() : base(BotInfo.FromFile("EasyBot.json")) { }

    public override void Run() {
        // Color setup
        BodyColor = Color.Orange;
        TurretColor = Color.Orange;
        RadarColor = Color.Orange;
        BulletColor = Color.Orange;
        ScanColor = Color.Orange;

        botIntels = new Dictionary<int, BotIntel>();

        AdjustRadarForBodyTurn = false;
        AdjustRadarForGunTurn = false;
        AdjustGunForBodyTurn = false;

        ResetRound();

        while (IsRunning) {

            HandleMovement();
            HandleShooting();
            HandleScan();
            
            Go();
        }
    }

    private void HandleMovement() {
        // Calculate preferred direction
        Vector2 preferredDirection = CalculateAvoidDirection();
        double preferredDirectionLength = preferredDirection.Length();

        // Normalize
        if (preferredDirection.Length() != 0) preferredDirection = preferredDirection / preferredDirection.Length();

        // Move
        MoveToDirection(preferredDirection, preferredDirectionLength);
    }

    private Vector2 CalculateAvoidDirection() {
        // Calculate preferred direction away from the bots last seen location
        Vector2 preferredDirection = new Vector2(0, 0);
        Vector2 tempDirection = new Vector2(0, 0);
        foreach (BotIntel botIntel in botIntels.Values)
        {
            if (botIntel.botHistory.length > 0)
            {
                BotHistoryEntry lastEntry = botIntel.botHistory.GetMostRecentEntry();
                if (TurnNumber - lastEntry.Time > LAST_SEEN_LIMIT) continue;
                tempDirection.X = (float) (X - lastEntry.Location.X);
                tempDirection.Y = (float) (Y - lastEntry.Location.Y);
                float distance = tempDirection.Length();

                float weight = 1;
                if (lastEntry.Energy <= WEAK_THRESHOLD) {
                    weight = -1;
                }

                preferredDirection += tempDirection * weight / (distance * distance * distance);
            }
        }

        // Calculate preferred direction away from the arena border
        float temp = (float) X;
        preferredDirection += new Vector2(1, 0) / (temp * temp) * BORDER_DESIRE;
        temp = (float) (ArenaWidth - X);
        preferredDirection += new Vector2(-1, 0) / (temp * temp) * BORDER_DESIRE;
        temp = (float) Y;
        preferredDirection += new Vector2(0, 1) / (temp * temp) * BORDER_DESIRE;
        temp = (float) (ArenaHeight - Y);
        preferredDirection += new Vector2(0, -1) / (temp * temp) * BORDER_DESIRE;

        // Calculate preferred direction away from the arena center
        tempDirection.X = (float) (X - (ArenaWidth / 2));
        tempDirection.Y = (float) (Y - (ArenaHeight / 2));
        temp = tempDirection.Length();
        preferredDirection += tempDirection / (temp * temp * temp) * BORDER_DESIRE;

        return preferredDirection;
    }

    private void MoveToDirection(Vector2 direction, double length) {
        // Calculate turn needed to face direction
        double turnAmount = -CalcBearing((double) (MathF.Atan2(direction.Y, direction.X) * (180 / MathF.PI)));

        TargetSpeed = 8 - Math.Abs(turnAmount) * length * 50;
        SetTurnRight(turnAmount);
    }

    private void HandleShooting() {
        if (!idHasHistory(targetId, 1)) {
            return;
        }

        // Calculate aim
        BotHistoryEntry lastEntry = botIntels[targetId].botHistory.GetMostRecentEntry();
        Vector2 aimLocation = new Vector2((float) (lastEntry.Location.X - X), (float) (lastEntry.Location.Y - Y));
        aimDirection = (double) ((MathF.Atan2(aimLocation.Y, aimLocation.X) * (180 / MathF.PI)));

        aimLastUpdate = lastEntry.Time;

        // Aim at direction
        SetTurnGunRight(-CalcGunBearing(aimDirection)+TurnRate);

        if (CalcDeltaAngle(aimDirection, GunDirection) <= SHOOT_TOLERANCE && TurnNumber - aimLastUpdate <= AIM_AGE_LIMIT) {
            if (DistanceTo(lastEntry.Location.X, lastEntry.Location.Y) <= BIG_SHOOT_DISTANCE) {
                SetFire(3);
            } else {
                SetFire(1);
            }
        } else {
            SetFire(0);
        }
    }

    private void HandleScan() {
        // Do the scan based on the mode
        switch (currentScanMode) {
            case ScanMode.Radar:
                RadarScan();
                break;
            case ScanMode.Focus:
                FocusScan();
                break;
        }

        // Reset target if it has not been seen in a while
        if (idHasHistory(targetId, 1)) {
            BotHistoryEntry lastEntry = botIntels[targetId].botHistory.GetMostRecentEntry();
            if (TurnNumber - lastEntry.Time > UNSEEN_LIMIT) {
                targetId = 0;
                currentScanMode = ScanMode.Radar;
            }
        }
    }

    private void StartRadarScan() {
        // Initialization
        currentScanMode = ScanMode.Radar;
        isRadarCheckpoint = false;
        radarStartTurn = TurnNumber;

        // Set radar scan direction (cw or ccw) based on current turn rates
        if (TurnRate + GunTurnRate > 0) {
            radarScanDirection = -1;
        } else {
            radarScanDirection = 1;
        }

        // Move Radar
        SetTurnRadarRight(360 * radarScanDirection);
    }

    private void RadarScan() {
        // Move Radar
        SetTurnRadarRight(360 * radarScanDirection);

        // Check if radar scan is complete/half complete
        if (isRadarCheckpoint && TurnNumber - radarStartTurn >= 2 * RADAR_HALF_LIMIT) {
            ExitRadarScan();
        } else if (!isRadarCheckpoint && TurnNumber - radarStartTurn >= RADAR_HALF_LIMIT) {
            isRadarCheckpoint = true;
        }
    }

    private void ExitRadarScan() {
        currentScanMode = ScanMode.Focus;
        lastRadarScan = TurnNumber;
    }

    private void FocusScan() {
        if (!idHasHistory(targetId, 1)) {
            StartRadarScan();
            return;
        }

        // Calculate radar scan direction to target
        BotHistoryEntry lastEntry = botIntels[targetId].botHistory.GetMostRecentEntry();
        double direction = DirectionTo(lastEntry.Location.X, lastEntry.Location.Y);

        // Move Radar
        if (CalcRadarBearing(direction) < 0) {
            SetTurnRadarRight((-CalcRadarBearing(direction)+TurnRate+GunTurnRate+22.5)%360);
        } else {
            SetTurnRadarRight((-CalcRadarBearing(direction)+TurnRate+GunTurnRate-22.5)%360);
        }

        // Go back to radar scan when focus is too long
        if (TurnNumber - lastRadarScan >= FOCUS_LIMIT && EnemyCount > 1) {
            StartRadarScan();
        }
    }

    private void ResetRound() {
        currentScanMode = ScanMode.Radar;
        isRadarCheckpoint = false;
        lastRadarScan = 0;
        radarStartTurn = 0;
        targetId = 0;
        aimDirection = 0;
        aimLastUpdate = 0;
    }

    private bool idHasHistory(int id, int count) {
        return (botIntels.ContainsKey(id) && botIntels[id].botHistory.length >= count);
    }

    public override void OnScannedBot(ScannedBotEvent e) {
        // Update bot intel
        if (!botIntels.ContainsKey(e.ScannedBotId)) {
            botIntels[e.ScannedBotId] = new BotIntel(e.ScannedBotId);
        }
        botIntels[e.ScannedBotId].botHistory.AddEntry(TurnNumber, e.Energy, e.X, e.Y, e.Direction, e.Speed);

        // Update target
        if (targetId == 0) {
            targetId = e.ScannedBotId;
        }  else if (idHasHistory(targetId, 1)) {
            BotHistoryEntry lastEntry = botIntels[targetId].botHistory.GetMostRecentEntry();
            if (DistanceTo(e.X, e.Y) < DistanceTo(lastEntry.Location.X, lastEntry.Location.Y)) {
                targetId = e.ScannedBotId;
            }
        }

        // Change to focus mode if target seen and radar is atleast half way
        if (currentScanMode == ScanMode.Radar && isRadarCheckpoint && e.ScannedBotId == targetId) {
            ExitRadarScan();
        }
    }

    public override void OnHitBot(HitBotEvent e) {
        // Update bot intel
        if (!botIntels.ContainsKey(e.VictimId)) {
            botIntels[e.VictimId] = new BotIntel(e.VictimId);
        }
        if (idHasHistory(e.VictimId, 1)) {
            BotHistoryEntry lastEntry = botIntels[e.VictimId].botHistory.GetMostRecentEntry();
            botIntels[e.VictimId].botHistory.AddEntry(TurnNumber, e.Energy, e.X, e.Y, lastEntry.Direction, lastEntry.Speed);
            if (DistanceTo(e.X, e.Y) < DistanceTo(lastEntry.Location.X, lastEntry.Location.Y)) {
                targetId = e.VictimId;
                lastRadarScan = TurnNumber - FOCUS_LIMIT / 2;
            }
        }
    }

    public override void OnRoundStarted(RoundStartedEvent roundStartedEvent) {
        ResetRound();
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

    public void AddEntry(int time, double energy, double X, double Y, double direction, double speed)
    {
        BotHistoryEntry newEntry = new BotHistoryEntry(time, energy, X, Y, direction, speed);
        if (length < history.Length) {
            history[length] = newEntry;
            length++;
        } else {
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