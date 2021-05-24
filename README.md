# Unio

BLE-WebSocket gateway for [toio](https://toio.io/) and other apps (e.g. Unity) on Windows.

**The current communication protocol is provisional. It is subject to change in the near future.**

## About files and folders

* Unio and Unio.sln: Source codes of Unio itself and Visual Studio solution.
* UnityProjects\UnioForUniy: Unity project for Unio testing.

## How to run

1. Open `Unio.sln` with Visual Studio >= 2017.
2. Build and run.
3. Open `UnioForUnity` project with Unity >= 2019.
4. Open `UnioTest\Scenes\SampleScene`.
5. Play it.
6. Turn on the toio.
7. Focus on Unity's Game Window and press `Connect` button to connect.
8. Press `Motor Run` button to go forward.
9. And so on.

## Data basics

Data between Unio and the client is represented in JSON format like the following.
```
{"serial":NUMBER, "uuid":"UUID", "data":ByteArray}
```
Here,
* Number: Integer value. NUMBER >= 1. Unio will generate a corresponding serial number for each toio.
* UUID: UUID corresponding to the toio characteristic.
* BYTE_ARRAY: Array of byte values.

The following data sent from the client to Unio represents a request to start a connection toio.
```
{"serial":0, "uuid":"", "data":[]}
```

## toio communication specification

For more information about Characteristic UUID and data format used for communication with toio, please refer to this official page.
https://toio.github.io/toio-spec/docs/ble_communication_overview.html

## Note

UnioForUnity is including a binary of [websocket-sharp](https://github.com/sta/websocket-sharp).

## License

These codes are licensed under CC0.

[![CC0](http://i.creativecommons.org/p/zero/1.0/88x31.png "CC0")](http://creativecommons.org/publicdomain/zero/1.0/deed)