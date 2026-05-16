# MediaFormatKit

一个 Windows 桌面小工具，用来查看媒体格式并进行常用音视频转换。

## 功能

- 查看单个视频/音频的容器、视频编码、音频编码、采样率、声道、码率等信息
- 批量查看文件夹内视频格式
- 保留画面并转换音频为 AAC 或 PCM
- 仅导出视频并去除音频
- 仅导出音频为 MP3、WAV 或 M4A

## 使用

发布包中需要保留以下文件在同一个文件夹：

- `媒体格式查看与转换工具.exe`
- `ffmpeg.exe`
- `ffprobe.exe`
- FFmpeg 相关 `.dll` 文件

双击 `媒体格式查看与转换工具.exe` 即可使用。

## 构建

需要 .NET 9 SDK：

```powershell
dotnet publish .\MediaFormatKit.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=false
```

## 说明

本工具调用 FFmpeg/FFprobe 完成媒体信息读取与格式转换。
