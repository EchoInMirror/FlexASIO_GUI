此分支对原项目做了以下修改:
- 修复中文乱码
- 汉化界面
- 将 PortAudio 替换为 NAudio \(仅支持获取 WASAPI 音频后端\)

by EIM

[下载链接](https://github.com/Tryanks/FlexASIO_GUI/releases/download/v0.34-modify/FlexASIO.GUIInstaller_EIMChanged.exe)

对应的 FlexASIO 版本: [FlexASIO v1.9](https://github.com/dechamps/FlexASIO/releases/download/flexasio-1.9/FlexASIO-1.9.exe)

---

This is a small GUI to make the configuration of https://github.com/dechamps/FlexASIO a bit quicker.

It should pick up your existing $Usersprofile/FlexASIO.toml file and read the basic parameters. Not all of them have been implemented yet...

To run, please make sure you have [.NET Desktop Runtime 6.x](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) installed.

v0.3 adds a registry key with the install path to HKEY_LOCAL_MACHINE\SOFTWARE\Fabrikat\FlexASIOGUI\Install\Installpath

It also makes most settings optional so that default settings are not overwritten.

![image](https://user-images.githubusercontent.com/6930367/118895016-a4746a80-b905-11eb-806c-7fd3fee4fcd1.png)

