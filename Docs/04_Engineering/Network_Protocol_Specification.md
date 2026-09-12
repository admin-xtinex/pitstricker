# Pit Striker — Network Protocol Specification (v1)

## 1. Packet Framing

All messages over the WebSocket transport follow binary packet framing:

```text
+-----------------------+----------------------------------+
| OpCode (1 Byte)       | Payload (Variable Length)        |
+-----------------------+----------------------------------+
```

Data types are encoded in little-endian order with zero boxing:
- `bool`: 1 byte (`0x00` or `0x01`).
- `int32` / `uint32`: 4 bytes little-endian.
- `float`: 4 bytes IEEE 754.
- `double`: 8 bytes IEEE 754.
- `string`: 4-byte length prefix followed by UTF-8 bytes.
- `NetVector3`: 3 consecutive `float`s (12 bytes total: `x`, `y`, `z`).

---

## 2. OpCode Directory

| OpCode | Hex | Name | Sender | Description |
| :--- | :--- | :--- | :--- | :--- |
| **1** | `0x01` | `ConnectRequest` | Client -> Server | Handshake containing Protocol Version (`int32`) and Player Name (`string`). |
| **2** | `0x02` | `ConnectResponse` | Server -> Client | Handshake response: Compatible (`bool`), SessionId (`string`), ReconnectToken (`string`). |
| **3** | `0x03` | `Ping` | Client -> Server | Heartbeat containing PingId (`uint32`). |
| **4** | `0x04` | `Pong` | Server -> Client | Heartbeat response containing original PingId (`uint32`). |
| **10** | `0x0A` | `CreateRoomRequest` | Client -> Server | Requests creation of a private match room. |
| **11** | `0x0B` | `CreateRoomResponse` | Server -> Client | Room Code (`string`, 6-chars) and PlayerIndex (`int32`, 0). |
| **12** | `0x0C` | `JoinRoomRequest` | Client -> Server | Join room with Room Code (`string`). |
| **13** | `0x0D` | `JoinRoomResponse` | Server -> Client | Success (`bool`), Room Code (`string`), PlayerIndex (`int32`). |
| **14** | `0x0E` | `QuickMatchRequest` | Client -> Server | Enters client into matchmaking queue. |
| **15** | `0x0F` | `MatchFound` | Server -> Client | Match found notification: Room Code, Player 1 Name, Player 2 Name. |
| **17** | `0x11` | `PlayerJoined` | Server -> Client | Opponent joined notification: PlayerIndex (`int32`), Name (`string`). |
| **19** | `0x13` | `LobbyCountdown` | Server -> Client | Pre-match countdown tick (`int32`, 3..1). |
| **20** | `0x14` | `MatchStarted` | Server -> Client | Match starting notification: Room Code, Player 1 Name, Player 2 Name. |
| **21** | `0x15` | `SubmitShotIntent` | Client -> Server | Active player shot input: Sequence (`uint32`), Timestamp (`double`), Direction (`NetVector3`), Force (`float`). |
| **22** | `0x16` | `ShotBroadcast` | Server -> Client | Shot broadcast to all players: PlayerIndex (`int32`), ShotIntent. |
| **24** | `0x18` | `WorldSnapshot` | Server -> Client | Full world state snapshot (see Section 3). |
| **26** | `0x1A` | `MatchCompleted` | Server -> Client | Match victory notification: Winner Player Index (`int32`). |
| **27** | `0x1B` | `RematchRequest` | Client -> Server | Requests a match rematch. |
| **28** | `0x1C` | `RematchConfirmed` | Server -> Client | Confirms both players accepted rematch. |
| **30** | `0x1E` | `ReconnectRequest` | Client -> Server | Reconnects to active match: ReconnectToken (`string`). |
| **31** | `0x1F` | `ReconnectResponse` | Server -> Client | Reconnection result: Success (`bool`), Room Code (`string`), PlayerIndex (`int32`). |
| **32** | `0x20` | `OpponentDisconnected` | Server -> Client | Opponent dropped: Disconnected Player Index (`int32`), Grace Period (`float`). |
| **33** | `0x21` | `OpponentReconnected` | Server -> Client | Opponent reconnected: Player Index (`int32`). |
| **34** | `0x22` | `MatchAbandoned` | Server -> Client | Match abandoned / forfeit: Winner Player Index (`int32`), Reason (`string`). |

---

## 3. High-Frequency State Snapshot Structure

The `WorldSnapshot` (`0x18`) payload size is approximately **60 bytes**:

```text
[uint32] ServerTick
[double] ServerTimestamp
[byte]   Phase (0: Waiting, 1: Countdown, 2: Toss, 3: ReadyToAim, 4: Rolling, 5: Evaluating, 6: Completed, 7: Abandoned)
[int32]  ActivePlayerIndex (0 or 1)
[float]  TurnTimerRemaining (seconds)
[MarbleState 0]:
  - [NetVector3] Position (12 bytes)
  - [NetVector3] Velocity (12 bytes)
  - [bool]       IsMoving (1 byte)
  - [bool]       IsRetired (1 byte)
[MarbleState 1]:
  - [NetVector3] Position (12 bytes)
  - [NetVector3] Velocity (12 bytes)
  - [bool]       IsMoving (1 byte)
  - [bool]       IsRetired (1 byte)
[PlayerData 0]:
  - [int32]  PlayerIndex
  - [string] Name
  - [int32]  TotalStrokes
  - [int32]  CurrentPit
  - [bool]   IsFinished
  - [bool]   IsConnected
[PlayerData 1]:
  - [int32]  PlayerIndex
  - [string] Name
  - [int32]  TotalStrokes
  - [int32]  CurrentPit
  - [bool]   IsFinished
  - [bool]   IsConnected
[int32]  WinnerPlayerIndex (-1 if not completed)
```
