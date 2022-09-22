# FlexASIO GUI 
> changed by [EIMSOUND](https://linktr.ee/EIMSOUND)

此分支对原项目做了以下修改:
- 修复中文乱码
- 汉化界面
- 将 PortAudio 替换为 NAudio \(仅支持获取 WASAPI 音频后端\)

![flex gui](https://user-images.githubusercontent.com/73160783/190864559-d8f4c796-50d5-4faa-8640-e8df348cb6c1.png)

## 下载

[下载链接](https://github.com/Tryanks/FlexASIO_GUI/releases/download/v0.34-modify/FlexASIO.GUIInstaller_EIMChanged.exe)

对应的 FlexASIO 版本: [FlexASIO v1.9](https://github.com/dechamps/FlexASIO/releases/download/flexasio-1.9/FlexASIO-1.9.exe)

## 配置说明

启用缓冲区大小: 一般配置为2的n次幂，cpu 性能不足建议 2048，cpu 性能过剩建议 128 或 256

设置输入/输出延迟: 输入/输出设备与 asio 之间的建议延迟，建议不要设置或设置为 0

独占模式: 是否允许应用独占设备

自动转换采样率: 是否允许 FlexASIO 自动转换音频的位深和采样率

保存到默认 FlexASIO.toml: 将当前配置保存到 FlexASIO GUI 每次打开的默认配置

另存为: 每次另存为到用户文件夹配置文件才会实际起作用，例如`C:\Users\Your Name\FlexASIO.toml`

[原版配置文档](https://github.com/dechamps/FlexASIO/blob/master/CONFIGURATION.md)

---

This is a small GUI to make the configuration of https://github.com/dechamps/FlexASIO a bit quicker.

It should pick up your existing $Usersprofile/FlexASIO.toml file and read the basic parameters. Not all of them have been implemented yet...

To run, please make sure you have [.NET Desktop Runtime 6.x](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) installed.

v0.3 adds a registry key with the install path to HKEY_LOCAL_MACHINE\SOFTWARE\Fabrikat\FlexASIOGUI\Install\Installpath

It also makes most settings optional so that default settings are not overwritten.
