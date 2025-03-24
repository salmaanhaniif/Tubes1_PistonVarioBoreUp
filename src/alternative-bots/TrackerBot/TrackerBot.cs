using System;
using System.Drawing;
using System.Linq.Expressions;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class TrackerBot : Bot
{
    bool hit = false;
    bool wall = false;
    bool runFromWall = false;
    int tries = 1;
    int turnDirection = 1;
    Double dir;

    bool first = true;
    double eX = 900000;
    double eY = 900000;

    double x4;
    double y4;
    
    static void Main(string[] args)
    {
        new TrackerBot().Start();
    }

    TrackerBot() : base(BotInfo.FromFile("TrackerBot.json")) { }

    public override void Run()
    {   
        
        AdjustRadarForGunTurn = true;
        AdjustRadarForBodyTurn = true;
        SetFireAssist(true);
        BodyColor = Color.Black;   
        TurretColor = Color.Red; 
        RadarColor = Color.Gray; 
        x4 = ArenaWidth/5;
        y4 = ArenaHeight/5; 
        double distance;

        while (IsRunning)
        {
            SetFire(0.1);
            SetTurnRadarLeft(1000_00000);
            if (first || eX == 900000) SetTurnLeft(10_000 * turnDirection);
            else SetTurnLeft(CalcBearing(DirectionTo(eX, eY)));
            distance = DistanceTo(eX, eY);
            if (distance > 160) SetForward(DistanceTo(eX, eY)/3);
            Go();
            TurnRadarRight(1000_00000);
        }
    }
    bool isNearWall() => X < 150 || Y < 150 || X > ArenaWidth - 150 || Y > ArenaHeight - 150;

    double shortDistanceWallX() => Math.Min(X, ArenaWidth - X);
    double shortDistanceWallY() => Math.Min(Y, ArenaHeight - Y);
    bool isFacingUp => Direction > 45 && Direction < 135;
    bool isFacingLeft => Direction > 135 && Direction < 225;
    bool isFacingDown => Direction > 225 && Direction < 315;
    bool isFacingRight => Direction > 315 || Direction < 45;

    public override void OnScannedBot(ScannedBotEvent e)
    {
        dir = e.Direction;
        eX = e.X;
        eY = e.Y;
        if (first) first = false;
        if ((90 < Direction) && (Direction < 270)) {
            if (e.Direction < Direction ) {
                turnDirection = -1;
            } else {
                turnDirection = 1;
            }
        } else {
            if ((Direction < 90) && (e.Direction > 270)) {
                turnDirection = -1;
            } else if ((Direction < 90) && (e.Direction < 90)) {
                if (e.Direction < Direction) turnDirection = -1;
                else turnDirection = 1;
            } else if ((Direction > 270) && (e.Direction > 270)) {
                if (e.Direction < Direction) turnDirection = -1;
                else turnDirection = 1;
            } else {
                turnDirection = 1;
            }
        }
        TurnToFaceTargetWithLeadPrediction(e.X, e.Y, e.Speed, e.Direction);
        var distance = DistanceTo(e.X, e.Y);
        if (distance < 115) Fire(3);
        else if (distance < 145) Fire(2);
        else Fire(1);
        if (distance <= 125) 
        {
            SetForward(distance - 125);
        } else {
            SetForward(distance/3);
            Go();
        }
        // Rescan();
        TurnLeft(4 * turnDirection);
    }

    // public override void OnHitWall(HitWallEvent botHitWallEvent)
    // {
    //     Forward(150);
    // }

    // public override void OnBulletFired(BulletFiredEvent bulletFiredEvent)
    // {
    //     SetTurnLeft(100 * tries);
    //     Back(200);
    //     tries = -tries;
    // }

    public override void OnHitBot(HitBotEvent e)
    {
        hit = true;
        TurnToFaceTarget(e.X, e.Y);

        if (Energy > 16)
            Fire(3);
        else if (Energy > 10)
            Fire(2);
        else if (Energy > 4)
            Fire(1);
        else if (Energy > 2)
            Fire(0.5);
        else if (Energy > .4)
            Fire(0.1);
        SetTurnLeft(150);
        SetBack(200);
        Go();
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

    private void TurnToFaceTargetWithLeadPrediction(double x, double y, double enemyVelocity, double enemyHeading) {
        double distance = DistanceTo(x, y);
        double bulletSpeed = 8; 
        double timeToReachTarget = distance / bulletSpeed;
        
        
        double futureX = x + Math.Sin(ToRadians(enemyHeading)) * enemyVelocity * timeToReachTarget;
        double futureY = y + Math.Cos(ToRadians(enemyHeading)) * enemyVelocity * timeToReachTarget;
        
        
        var bearing = BearingTo(futureX, futureY);
        
        
        if (bearing >= 0)
            turnDirection = 1;
        else
            turnDirection = -1;
            
        TurnLeft(bearing);
    }
    private double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}
