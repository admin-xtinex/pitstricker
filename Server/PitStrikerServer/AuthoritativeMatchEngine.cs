using System;
using System.Threading.Tasks;
using PitStriker.Networking.Shared;

namespace PitStrikerServer
{
    public class AuthoritativeMatchEngine
    {
        public Room Room { get; }
        public CloudMatchPhase Phase { get; private set; } = CloudMatchPhase.WaitingForPlayers;

        public int ActivePlayerIndex { get; private set; } = 0;
        public float TurnTimerRemaining { get; private set; } = NetworkProtocol.DefaultTurnDuration;
        public uint ServerTick { get; private set; } = 0;
        public int WinnerPlayerIndex { get; private set; } = -1;

        // Player statistics
        public CompactPlayerData[] Players = new CompactPlayerData[2];

        // Marble states
        public CompactMarbleState[] Marbles = new CompactMarbleState[2];

        // Turn limits & bonus tracking
        private int _shotsTakenThisTurn = 0;
        private const int MaxShotsPerTurn = 3;
        private bool _hitOpponentThisTurn = false;
        private bool _pitConqueredThisTurn = false;

        // Pit Locations
        private static readonly NetVector3 Pit1Pos = new NetVector3(0f, 0.25f, 4.0f);
        private static readonly NetVector3 Pit2Pos = new NetVector3(0f, 0.25f, 16.0f);
        private static readonly NetVector3 Pit3Pos = new NetVector3(0f, 0.25f, 31.0f);
        private const float PitCatchRadius = 0.52f;

        // Physics constants
        private const float MarbleRadius = 0.16f;
        private const float LinearDamping = 0.18f;
        private const float StopThreshold = 0.10f;
        private const float FairwayMinX = -2.5f;
        private const float FairwayMaxX = 2.5f;
        private const float FairwayMinZ = -8.5f;
        private const float FairwayMaxZ = 35.0f;

        // Rematch votes
        public bool Player0WantsRematch { get; set; } = false;
        public bool Player1WantsRematch { get; set; } = false;

        public AuthoritativeMatchEngine(Room room)
        {
            Room = room;
            ResetMatch();
        }

        public void ResetMatch()
        {
            Phase = CloudMatchPhase.WaitingForPlayers;
            ActivePlayerIndex = 0;
            TurnTimerRemaining = NetworkProtocol.DefaultTurnDuration;
            WinnerPlayerIndex = -1;
            _shotsTakenThisTurn = 0;
            _hitOpponentThisTurn = false;
            _pitConqueredThisTurn = false;
            Player0WantsRematch = false;
            Player1WantsRematch = false;

            Players[0] = new CompactPlayerData(0, Room.Player0?.PlayerName ?? "Player 1", 0, 1, false, true);
            Players[1] = new CompactPlayerData(1, Room.Player1?.PlayerName ?? "Player 2", 0, 1, false, true);

            // Starting Tee Positions
            Marbles[0] = new CompactMarbleState(new NetVector3(-0.4f, 0.25f, -6.0f), NetVector3.Zero, false, false);
            Marbles[1] = new CompactMarbleState(new NetVector3(0.4f, 0.25f, -6.0f), NetVector3.Zero, false, false);
        }

        public void StartMatch()
        {
            ResetMatch();
            Phase = CloudMatchPhase.ReadyToAim;
            TurnTimerRemaining = NetworkProtocol.DefaultTurnDuration;
            Console.WriteLine($"[ROOM {Room.RoomCode}] Match started! Active player: P{ActivePlayerIndex + 1}");
        }

        public bool SubmitShot(int playerIndex, ShotIntentData intent)
        {
            if (Phase != CloudMatchPhase.ReadyToAim)
            {
                Console.WriteLine($"[ROOM {Room.RoomCode}] Shot rejected: Not in ReadyToAim phase (currently {Phase}).");
                return false;
            }

            if (playerIndex != ActivePlayerIndex)
            {
                Console.WriteLine($"[ROOM {Room.RoomCode}] Shot rejected: Not Player {playerIndex}'s turn (Active is {ActivePlayerIndex}).");
                return false;
            }

            // Force validation
            float clampedForce = Math.Clamp(intent.Force, NetworkProtocol.MinAllowedForce, NetworkProtocol.MaxAllowedForce);
            NetVector3 dir = intent.Direction;
            dir.y = Math.Clamp(dir.y, 0f, NetworkProtocol.MaxAllowedPitch);
            NetVector3 normalizedDir = dir.Normalized;

            // Apply impulse
            NetVector3 initialVel = new NetVector3(normalizedDir.x * clampedForce, 0f, normalizedDir.z * clampedForce);
            Marbles[playerIndex].Velocity = initialVel;
            Marbles[playerIndex].IsMoving = true;

            Players[playerIndex].TotalStrokes++;
            _shotsTakenThisTurn++;
            _hitOpponentThisTurn = false;
            _pitConqueredThisTurn = false;

            Phase = CloudMatchPhase.Rolling;
            Console.WriteLine($"[ROOM {Room.RoomCode}] P{playerIndex + 1} fired shot: force={clampedForce:F1}, strokes={Players[playerIndex].TotalStrokes}");
            return true;
        }

        public void Tick(float dt)
        {
            ServerTick++;

            if (Phase == CloudMatchPhase.ReadyToAim)
            {
                TurnTimerRemaining -= dt;
                if (TurnTimerRemaining <= 0f)
                {
                    Console.WriteLine($"[ROOM {Room.RoomCode}] Turn timer expired for P{ActivePlayerIndex + 1}. Switching turns.");
                    PassTurn();
                }
            }
            else if (Phase == CloudMatchPhase.Rolling)
            {
                IntegratePhysics(dt);

                // Check if all marbles have come to rest
                bool anyMoving = Marbles[0].IsMoving || Marbles[1].IsMoving;
                if (!anyMoving)
                {
                    Phase = CloudMatchPhase.Evaluating;
                    EvaluateTurnOutcome();
                }
            }
        }

        private void IntegratePhysics(float dt)
        {
            for (int i = 0; i < 2; i++)
            {
                if (!Marbles[i].IsMoving || Marbles[i].IsRetired) continue;

                // Simple Euler integration with ground damping
                NetVector3 pos = Marbles[i].Position;
                NetVector3 vel = Marbles[i].Velocity;

                pos.x += vel.x * dt;
                pos.z += vel.z * dt;

                // Drag
                float speed = vel.Magnitude;
                float dragFactor = MathF.Max(0f, 1f - (LinearDamping * dt * 4f));
                vel.x *= dragFactor;
                vel.z *= dragFactor;

                // Boundary reflection (fairway edges)
                if (pos.x < FairwayMinX)
                {
                    pos.x = FairwayMinX;
                    vel.x = -vel.x * 0.75f;
                }
                else if (pos.x > FairwayMaxX)
                {
                    pos.x = FairwayMaxX;
                    vel.x = -vel.x * 0.75f;
                }

                if (pos.z < FairwayMinZ)
                {
                    pos.z = FairwayMinZ;
                    vel.z = -vel.z * 0.75f;
                }
                else if (pos.z > FairwayMaxZ)
                {
                    pos.z = FairwayMaxZ;
                    vel.z = -vel.z * 0.75f;
                }

                // Stop threshold check
                if (vel.SqrMagnitude < (StopThreshold * StopThreshold))
                {
                    vel = NetVector3.Zero;
                    Marbles[i].IsMoving = false;
                }

                Marbles[i].Position = pos;
                Marbles[i].Velocity = vel;
            }

            // Marble vs Marble elastic collision check
            float dist = NetVector3.Distance(Marbles[0].Position, Marbles[1].Position);
            if (dist < MarbleRadius * 2f && dist > 0.001f)
            {
                _hitOpponentThisTurn = true;
                // Simple 2D elastic collision response
                NetVector3 normal = new NetVector3(
                    (Marbles[1].Position.x - Marbles[0].Position.x) / dist,
                    0f,
                    (Marbles[1].Position.z - Marbles[0].Position.z) / dist);

                // Relative velocity along normal
                float kx = Marbles[0].Velocity.x - Marbles[1].Velocity.x;
                float kz = Marbles[0].Velocity.z - Marbles[1].Velocity.z;
                float p = 2f * (normal.x * kx + normal.z * kz) / 2f;

                Marbles[0].Velocity = new NetVector3(Marbles[0].Velocity.x - p * normal.x * 0.85f, 0f, Marbles[0].Velocity.z - p * normal.z * 0.85f);
                Marbles[1].Velocity = new NetVector3(Marbles[1].Velocity.x + p * normal.x * 0.85f, 0f, Marbles[1].Velocity.z + p * normal.z * 0.85f);

                Marbles[0].IsMoving = true;
                Marbles[1].IsMoving = true;
            }
        }

        private void EvaluateTurnOutcome()
        {
            int pIdx = ActivePlayerIndex;
            NetVector3 marblePos = Marbles[pIdx].Position;
            int targetPit = Players[pIdx].CurrentPit;

            NetVector3 pitTargetPos = targetPit switch
            {
                1 => Pit1Pos,
                2 => Pit2Pos,
                3 => Pit3Pos,
                _ => Pit3Pos
            };

            float distToPit = NetVector3.Distance(marblePos, pitTargetPos);
            if (distToPit <= PitCatchRadius)
            {
                // Sunk target pit!
                _pitConqueredThisTurn = true;
                Console.WriteLine($"[ROOM {Room.RoomCode}] ★ P{pIdx + 1} conquered Pit {targetPit}! ★");

                if (targetPit == 3)
                {
                    // Player has completed the course!
                    Players[pIdx].IsFinished = true;
                    Marbles[pIdx].IsRetired = true;
                    WinnerPlayerIndex = pIdx;
                    Phase = CloudMatchPhase.MatchCompleted;
                    Console.WriteLine($"[ROOM {Room.RoomCode}] ★★★ MATCH FINISHED! WINNER: P{pIdx + 1} ({Players[pIdx].Name}) ★★★");
                    return;
                }
                else
                {
                    // Advance to next pit
                    Players[pIdx].CurrentPit++;

                    // Advance tee position for next shot
                    NetVector3 nextTee = targetPit == 1 ? new NetVector3(0f, 0.25f, 4.5f) : new NetVector3(0f, 0.25f, 16.5f);
                    Marbles[pIdx].Position = nextTee;
                }
            }

            // Decide whether bonus shot is granted or turn passes
            bool getsBonusPlay = (_pitConqueredThisTurn || _hitOpponentThisTurn) && (_shotsTakenThisTurn < MaxShotsPerTurn);

            if (getsBonusPlay)
            {
                Console.WriteLine($"[ROOM {Room.RoomCode}] P{pIdx + 1} earned EXTRA PLAY ({_shotsTakenThisTurn}/{MaxShotsPerTurn})!");
                Phase = CloudMatchPhase.ReadyToAim;
                TurnTimerRemaining = NetworkProtocol.DefaultTurnDuration;
            }
            else
            {
                PassTurn();
            }
        }

        private void PassTurn()
        {
            _shotsTakenThisTurn = 0;
            _hitOpponentThisTurn = false;
            _pitConqueredThisTurn = false;

            // Switch to the other player if not finished
            int otherIdx = 1 - ActivePlayerIndex;
            if (!Players[otherIdx].IsFinished)
            {
                ActivePlayerIndex = otherIdx;
            }

            Phase = CloudMatchPhase.ReadyToAim;
            TurnTimerRemaining = NetworkProtocol.DefaultTurnDuration;
            Console.WriteLine($"[ROOM {Room.RoomCode}] Turn passed to P{ActivePlayerIndex + 1}");
        }

        public WorldSnapshotData CreateSnapshot()
        {
            return new WorldSnapshotData(
                ServerTick,
                DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds,
                Phase,
                ActivePlayerIndex,
                TurnTimerRemaining,
                Marbles[0],
                Marbles[1],
                Players[0],
                Players[1],
                WinnerPlayerIndex);
        }
    }
}
