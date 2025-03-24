using System;
using System.Collections.Generic;
using System.Numerics;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class BouncingBot : Bot {   
    private enum ScanMode {
        Radar,
        Focus
    }

    private const double DOUBLE_TOLERANCE = 0.01;

    // Movement Variables
    #region Movement Variables
    private const double BORDER_CHECK = 30;
    private Vector2 bounceDirection;
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
        new BouncingBot().Start();
    }

    BouncingBot() : base(BotInfo.FromFile("BouncingBot.json")) { }

    public override void Run() {
        // Color setup
        BodyColor = Color.Red;
        TurretColor = Color.Red;
        RadarColor = Color.Red;
        BulletColor = Color.Red;
        ScanColor = Color.Red;

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
        // Move
        MoveToDirection(bounceDirection);
    }

    private void MoveToDirection(Vector2 direction) {
        // Calculate turn needed to face direction
        double moveDirection = (double) (MathF.Atan2(direction.Y, direction.X) * (180 / MathF.PI));
        double turnAmount = -CalcBearing(moveDirection);
        double turnAmountReverse = -CalcBearing((moveDirection + 180) % 360);

        if (Math.Abs(turnAmount) < Math.Abs(turnAmountReverse)) {
            if (turnAmount == 0) TargetSpeed = 8;
            SetTurnRight(turnAmount);
        } else {
            if (turnAmountReverse == 0) TargetSpeed = -8;
            SetTurnRight(turnAmountReverse);
        }
    }

    private void BounceWall() {
        if (Math.Abs(X) < BORDER_CHECK) {
            bounceDirection = new Vector2((float) Math.Abs(bounceDirection.X), (float) bounceDirection.Y);
        } else if (Math.Abs(ArenaWidth - X) < BORDER_CHECK) {
            bounceDirection = new Vector2(-(float) Math.Abs(bounceDirection.X), (float) bounceDirection.Y);
        }

        if (Math.Abs(Y) < BORDER_CHECK) {
            bounceDirection = new Vector2((float) bounceDirection.X, (float) Math.Abs(bounceDirection.Y));
        } else if (Math.Abs(ArenaHeight - Y) < BORDER_CHECK) {
            bounceDirection = new Vector2((float) bounceDirection.X, -(float) Math.Abs(bounceDirection.Y));
        }
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

        double rand = new Random().NextDouble() * 360;
        bounceDirection = new Vector2((float) Math.Cos(rand * (MathF.PI / 180)), (float) Math.Sin(rand * (MathF.PI / 180)));
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

    public override void OnHitWall(HitWallEvent e) {
        BounceWall();
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