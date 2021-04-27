# Unio

BLE - WebSocket gateway for toio and other apps (e.g. Unity) on Windows.

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
7. Focus on Unity's Game Window and press the C key to connect.
8. Press G key to go forward.

See `UnioTest\Scripts\UnioTest.cs` for other operations.

## Note

UnioForUnity is including a binary of [websocket-sharp](https://github.com/sta/websocket-sharp)

## License

These codes are licensed under CC0.

[![CC0](http://i.creativecommons.org/p/zero/1.0/88x31.png "CC0")](http://creativecommons.org/publicdomain/zero/1.0/deed.ja)