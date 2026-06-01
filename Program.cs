using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MediaFormatToolExe;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly TextBox _pathBox;
    private readonly TextBox _outputBox;
    private readonly ComboBox _presetBox;
    private readonly TextBox _resultBox;
    private readonly Button _convertButton;

    private static readonly string[] VideoExtensions =
    [
        ".mp4", ".mov", ".mkv", ".avi", ".wmv", ".m4v", ".flv", ".webm"
    ];

    public MainForm()
    {
        Text = "媒体格式查看与转换工具";
        Size = new Size(960, 680);
        MinimumSize = new Size(820, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(246, 248, 251);
        Font = new Font("Microsoft YaHei UI", 9);

        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(960, 78),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(17, 24, 39)
        };
        Controls.Add(header);

        header.Controls.Add(new Label
        {
            Text = "媒体格式查看与转换",
            Location = new Point(24, 14),
            Size = new Size(360, 28),
            Font = new Font("Microsoft YaHei UI", 15, FontStyle.Bold),
            ForeColor = Color.White
        });

        header.Controls.Add(new Label
        {
            Text = "查看视频/音频编码参数，转换常用音频格式",
            Location = new Point(26, 45),
            Size = new Size(420, 22),
            ForeColor = Color.FromArgb(209, 213, 219)
        });

        var panel = new Panel
        {
            Location = new Point(18, 94),
            Size = new Size(906, 158),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White
        };
        Controls.Add(panel);

        panel.Controls.Add(MakeLabel("文件或文件夹", 18, 18));
        _pathBox = MakeTextBox(112, 16, 550);
        _pathBox.Text = Environment.CurrentDirectory;
        panel.Controls.Add(_pathBox);

        var fileButton = MakeButton("选择文件", 676, 14, 96, Color.FromArgb(229, 231, 235), Color.FromArgb(31, 41, 55));
        fileButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        fileButton.Click += (_, _) => ChooseFile();
        panel.Controls.Add(fileButton);

        var folderButton = MakeButton("选择文件夹", 782, 14, 108, Color.FromArgb(229, 231, 235), Color.FromArgb(31, 41, 55));
        folderButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        folderButton.Click += (_, _) => ChooseFolder();
        panel.Controls.Add(folderButton);

        panel.Controls.Add(MakeLabel("输出目录", 18, 61));
        _outputBox = MakeTextBox(112, 59, 550);
        _outputBox.Text = Environment.CurrentDirectory;
        panel.Controls.Add(_outputBox);

        var outputButton = MakeButton("更改目录", 676, 57, 96, Color.FromArgb(229, 231, 235), Color.FromArgb(31, 41, 55));
        outputButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        outputButton.Click += (_, _) => ChooseOutputFolder();
        panel.Controls.Add(outputButton);

        panel.Controls.Add(MakeLabel("转换方式", 18, 105));
        _presetBox = new ComboBox
        {
            Location = new Point(112, 102),
            Size = new Size(460, 26),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _presetBox.Items.AddRange([
            "NVIDIA 快速压缩到约 200MB（MP4）",
            "NVIDIA 快速压缩到约 100MB（MP4）",
            "CPU 兼容压缩到约 200MB（MP4）",
            "CPU 兼容压缩到约 100MB（MP4）",
            "保留画面，音频转 AAC 44.1k 立体声（MP4）",
            "保留画面，音频转 AAC 48k 立体声（MP4）",
            "保留画面，音频转 PCM 16-bit（MOV）",
            "仅导出视频：去除音频（MP4）",
            "仅导出音频：MP3 192k",
            "仅导出音频：WAV PCM 16-bit",
            "仅导出音频：AAC M4A"
        ]);
        _presetBox.SelectedIndex = 0;
        panel.Controls.Add(_presetBox);

        var viewButton = MakeButton("查看所选", 588, 100, 98, Color.FromArgb(37, 99, 235), Color.White);
        viewButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        viewButton.Click += async (_, _) => await ViewSelectedAsync();
        panel.Controls.Add(viewButton);

        var folderViewButton = MakeButton("查看文件夹", 696, 100, 104, Color.FromArgb(29, 78, 216), Color.White);
        folderViewButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        folderViewButton.Click += async (_, _) => await ViewFolderAsync();
        panel.Controls.Add(folderViewButton);

        _convertButton = MakeButton("开始转换", 810, 100, 80, Color.FromArgb(5, 150, 105), Color.White);
        _convertButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _convertButton.Click += async (_, _) => await ConvertSelectedAsync();
        panel.Controls.Add(_convertButton);

        Controls.Add(new Label
        {
            Text = "结果",
            Location = new Point(22, 270),
            Size = new Size(100, 24),
            Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 41, 55)
        });

        var clearButton = MakeButton("清空结果", 810, 264, 108, Color.FromArgb(229, 231, 235), Color.FromArgb(31, 41, 55));
        clearButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        clearButton.Click += (_, _) => _resultBox.Clear();
        Controls.Add(clearButton);

        _resultBox = new TextBox
        {
            Location = new Point(18, 302),
            Size = new Size(906, 320),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 10),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(226, 232, 240),
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(_resultBox);

        if (!ToolExists("ffmpeg") || !ToolExists("ffprobe"))
        {
            MessageBox.Show("没有找到 ffmpeg 或 ffprobe。请把 ffmpeg.exe 和 ffprobe.exe 放在本工具同目录，或安装 ffmpeg 并加入 PATH。", "缺少依赖", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private Label MakeLabel(string text, int x, int y) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(95, 24),
        ForeColor = Color.FromArgb(31, 41, 55)
    };

    private TextBox MakeTextBox(int x, int y, int width) => new()
    {
        Location = new Point(x, y),
        Size = new Size(width, 25),
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
    };

    private Button MakeButton(string text, int x, int y, int width, Color back, Color fore)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = fore,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private void ChooseFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择媒体文件",
            Filter = "媒体文件|*.mp4;*.mov;*.mkv;*.avi;*.wmv;*.m4v;*.flv;*.webm;*.mp3;*.wav;*.aac;*.m4a;*.flac;*.ogg|所有文件|*.*"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        _pathBox.Text = dialog.FileName;
        _outputBox.Text = Path.GetDirectoryName(dialog.FileName) ?? Environment.CurrentDirectory;
    }

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择要查看的视频文件夹",
            SelectedPath = Directory.Exists(_pathBox.Text) ? _pathBox.Text : Environment.CurrentDirectory
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        _pathBox.Text = dialog.SelectedPath;
        _outputBox.Text = dialog.SelectedPath;
    }

    private void ChooseOutputFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择转换后的输出目录",
            SelectedPath = Directory.Exists(_outputBox.Text) ? _outputBox.Text : Environment.CurrentDirectory
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _outputBox.Text = dialog.SelectedPath;
        }
    }

    private async Task ViewSelectedAsync()
    {
        try
        {
            if (!File.Exists(_pathBox.Text))
            {
                MessageBox.Show("请先选择一个具体的媒体文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _resultBox.Text = await GetMediaInfoAsync(_pathBox.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "读取失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ViewFolderAsync()
    {
        try
        {
            if (!Directory.Exists(_pathBox.Text))
            {
                MessageBox.Show("请先选择一个文件夹。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var files = Directory.GetFiles(_pathBox.Text)
                .Where(path => VideoExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                .ToArray();
            if (files.Length == 0)
            {
                MessageBox.Show("这个文件夹里没有找到常见视频文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var builder = new StringBuilder();
            foreach (var file in files)
            {
                builder.AppendLine(await GetMediaInfoAsync(file));
                builder.AppendLine(new string('=', 90));
            }
            _resultBox.Text = builder.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "读取失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ConvertSelectedAsync()
    {
        try
        {
            if (!File.Exists(_pathBox.Text))
            {
                MessageBox.Show("转换前请先选择一个具体媒体文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Directory.Exists(_outputBox.Text))
            {
                MessageBox.Show("输出目录不存在，请重新选择。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var plan = await BuildConversionPlanAsync(_pathBox.Text, _outputBox.Text, _presetBox.SelectedIndex);
            if (File.Exists(plan.OutputPath))
            {
                var answer = MessageBox.Show($"输出文件已存在，是否覆盖？\n{plan.OutputPath}", "确认覆盖", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes) return;
            }

            _convertButton.Enabled = false;
            _resultBox.AppendText($"开始转换：{_pathBox.Text}{Environment.NewLine}");
            _resultBox.AppendText($"输出文件：{plan.OutputPath}{Environment.NewLine}");

            var result = await RunProcessAsync(FindTool("ffmpeg"), ["-y", "-hide_banner", "-i", _pathBox.Text, .. plan.Arguments, plan.OutputPath]);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(result.Output.Trim());
            }

            _resultBox.AppendText($"已完成：{plan.OutputPath}{Environment.NewLine}{Environment.NewLine}");
            _resultBox.AppendText($"转换后格式：{Environment.NewLine}");
            _resultBox.AppendText(await GetMediaInfoAsync(plan.OutputPath));
            MessageBox.Show($"已完成：{plan.OutputPath}", "转换完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "转换失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _convertButton.Enabled = true;
        }
    }

    private static async Task<ConversionPlan> BuildConversionPlanAsync(string inputPath, string outputFolder, int preset)
    {
        return preset switch
        {
            0 => await BuildCompressPlanAsync(inputPath, outputFolder, 200, useNvenc: true),
            1 => await BuildCompressPlanAsync(inputPath, outputFolder, 100, useNvenc: true),
            2 => await BuildCompressPlanAsync(inputPath, outputFolder, 200, useNvenc: false),
            3 => await BuildCompressPlanAsync(inputPath, outputFolder, 100, useNvenc: false),
            4 => new ConversionPlan(GetOutputPath(inputPath, "_aac441", ".mp4", outputFolder), ["-map", "0:V:0?", "-map", "0:a:0?", "-map_metadata", "-1", "-c:v", "copy", "-c:a", "aac", "-profile:a", "aac_low", "-ar", "44100", "-ac", "2", "-b:a", "128k", "-movflags", "+faststart"]),
            5 => new ConversionPlan(GetOutputPath(inputPath, "_aac48", ".mp4", outputFolder), ["-map", "0:V:0?", "-map", "0:a:0?", "-map_metadata", "-1", "-c:v", "copy", "-c:a", "aac", "-profile:a", "aac_low", "-ar", "48000", "-ac", "2", "-b:a", "160k", "-movflags", "+faststart"]),
            6 => new ConversionPlan(GetOutputPath(inputPath, "_pcm16", ".mov", outputFolder), ["-map", "0:V:0?", "-map", "0:a:0?", "-map_metadata", "-1", "-c:v", "copy", "-c:a", "pcm_s16le", "-ar", "48000", "-ac", "2"]),
            7 => new ConversionPlan(GetOutputPath(inputPath, "_video_only", ".mp4", outputFolder), ["-map", "0:V:0?", "-map_metadata", "-1", "-c:v", "copy", "-an", "-movflags", "+faststart"]),
            8 => new ConversionPlan(GetOutputPath(inputPath, "_audio", ".mp3", outputFolder), ["-vn", "-c:a", "libmp3lame", "-b:a", "192k"]),
            9 => new ConversionPlan(GetOutputPath(inputPath, "_audio", ".wav", outputFolder), ["-vn", "-c:a", "pcm_s16le", "-ar", "48000", "-ac", "2"]),
            _ => new ConversionPlan(GetOutputPath(inputPath, "_audio", ".m4a", outputFolder), ["-vn", "-c:a", "aac", "-profile:a", "aac_low", "-ar", "44100", "-ac", "2", "-b:a", "128k"])
        };
    }

    private static async Task<ConversionPlan> BuildCompressPlanAsync(string inputPath, string outputFolder, int targetMb, bool useNvenc)
    {
        var durationSeconds = await GetDurationSecondsAsync(inputPath);
        if (durationSeconds <= 0)
        {
            throw new InvalidOperationException("无法读取视频时长，不能按目标大小压缩。");
        }

        var totalKbps = (int)Math.Floor(targetMb * 8192d / durationSeconds);
        var audioKbps = totalKbps >= 900 ? 128 : Math.Max(64, totalKbps / 5);
        var videoKbps = Math.Max(250, totalKbps - audioKbps);
        var maxRateKbps = Math.Max(videoKbps + 100, (int)(videoKbps * 1.5));
        var bufferKbps = Math.Max(maxRateKbps * 2, videoKbps * 3);

        var suffix = useNvenc ? $"_nvenc_{targetMb}MB" : $"_compress_{targetMb}MB";
        var videoArgs = useNvenc
            ? new[]
            {
                "-c:v", "h264_nvenc",
                "-preset", "p4",
                "-rc", "vbr",
                "-b:v", $"{videoKbps}k",
                "-maxrate", $"{maxRateKbps}k",
                "-bufsize", $"{bufferKbps}k",
                "-pix_fmt", "yuv420p"
            }
            : new[]
            {
                "-c:v", "libx264",
                "-preset", "veryfast",
                "-b:v", $"{videoKbps}k",
                "-maxrate", $"{maxRateKbps}k",
                "-bufsize", $"{bufferKbps}k",
                "-pix_fmt", "yuv420p"
            };

        return new ConversionPlan(
            GetOutputPath(inputPath, suffix, ".mp4", outputFolder),
            [
                "-map", "0:V:0?",
                "-map", "0:a:0?",
                "-map_metadata", "-1",
                .. videoArgs,
                "-c:a", "aac",
                "-profile:a", "aac_low",
                "-b:a", $"{audioKbps}k",
                "-ac", "2",
                "-movflags", "+faststart"
            ]);
    }

    private static string GetOutputPath(string inputPath, string suffix, string extension, string outputFolder)
    {
        var baseName = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(outputFolder, $"{baseName}{suffix}{extension}");
    }

    private static async Task<string> GetMediaInfoAsync(string path)
    {
        var format = await RunProcessAsync(FindTool("ffprobe"), ["-v", "error", "-show_entries", "format=format_name,format_long_name,duration,size,bit_rate", "-of", "compact=p=1:nk=0", path]);
        var streams = await RunProcessAsync(FindTool("ffprobe"), ["-v", "error", "-show_entries", "stream=codec_type,codec_name,codec_long_name,profile,width,height,avg_frame_rate,bit_rate,sample_rate,channels,channel_layout", "-of", "compact=p=1:nk=0", path]);

        if (format.ExitCode != 0 || streams.ExitCode != 0)
        {
            throw new InvalidOperationException((format.Output + Environment.NewLine + streams.Output).Trim());
        }

        var formatMap = format.Output.SplitLines()
            .Where(line => line.StartsWith("format|", StringComparison.Ordinal))
            .Select(ParseCompactLine)
            .FirstOrDefault() ?? [];

        var builder = new StringBuilder();
        builder.AppendLine($"文件：{path}");
        builder.AppendLine($"容器：{GetValue(formatMap, "format_long_name", GetValue(formatMap, "format_name"))}");
        builder.AppendLine($"时长：{FormatSeconds(GetValue(formatMap, "duration"))}");
        builder.AppendLine($"大小：{FormatSize(GetValue(formatMap, "size"))}");
        builder.AppendLine($"总码率：{FormatKbps(GetValue(formatMap, "bit_rate"))}");

        foreach (var streamMap in streams.Output.SplitLines().Where(line => line.StartsWith("stream|", StringComparison.Ordinal)).Select(ParseCompactLine))
        {
            if (GetValue(streamMap, "codec_type") == "video")
            {
                builder.AppendLine();
                builder.AppendLine("[视频]");
                builder.AppendLine($"编码：{GetValue(streamMap, "codec_name")} ({GetValue(streamMap, "codec_long_name")})");
                builder.AppendLine($"分辨率：{GetValue(streamMap, "width")}x{GetValue(streamMap, "height")}");
                builder.AppendLine($"帧率：{FormatFps(GetValue(streamMap, "avg_frame_rate"))}");
                builder.AppendLine($"码率：{FormatKbps(GetValue(streamMap, "bit_rate"))}");
            }
            else if (GetValue(streamMap, "codec_type") == "audio")
            {
                builder.AppendLine();
                builder.AppendLine("[音频]");
                builder.AppendLine($"编码：{GetValue(streamMap, "codec_name")} ({GetValue(streamMap, "codec_long_name")})");
                builder.AppendLine($"Profile：{GetValue(streamMap, "profile")}");
                builder.AppendLine($"采样率：{FormatSampleRate(GetValue(streamMap, "sample_rate"))}");
                builder.AppendLine($"声道：{GetValue(streamMap, "channels")} ({GetValue(streamMap, "channel_layout")})");
                builder.AppendLine($"码率：{FormatKbps(GetValue(streamMap, "bit_rate"))}");
            }
        }

        return builder.ToString();
    }

    private static async Task<double> GetDurationSecondsAsync(string path)
    {
        var result = await RunProcessAsync(FindTool("ffprobe"), ["-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", path]);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.Output.Trim());
        }

        var text = result.Output.Trim();
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var duration) ? duration : 0;
    }

    private static Dictionary<string, string> ParseCompactLine(string line)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in line.Split('|'))
        {
            var index = part.IndexOf('=');
            if (index > 0) map[part[..index]] = part[(index + 1)..];
        }
        return map;
    }

    private static string GetValue(Dictionary<string, string> map, string key, string fallback = "-")
    {
        return map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static string FormatSeconds(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) ? $"{seconds:N2} 秒" : "-";
    }

    private static string FormatSize(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bytes) ? $"{bytes / 1024 / 1024:N2} MB" : "-";
    }

    private static string FormatKbps(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bitRate) ? $"{bitRate / 1000:N0} kbps" : "-";
    }

    private static string FormatSampleRate(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : $"{value} Hz";
    }

    private static string FormatFps(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0/0") return "-";
        var parts = value.Split('/');
        if (parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator)
            && denominator != 0)
        {
            return $"{numerator / denominator:N2} fps";
        }
        return value;
    }

    private static bool ToolExists(string tool)
    {
        try
        {
            return File.Exists(FindTool(tool));
        }
        catch
        {
            return false;
        }
    }

    private static string FindTool(string tool)
    {
        var exeName = tool.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? tool : $"{tool}.exe";
        var local = Path.Combine(AppContext.BaseDirectory, exeName);
        if (File.Exists(local)) return local;

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            var candidate = Path.Combine(dir.Trim(), exeName);
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException($"没有找到 {exeName}。请把 {exeName} 放在本工具同目录，或安装 ffmpeg 并加入 PATH。");
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, IReadOnlyList<string> arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var argument in arguments) psi.ArgumentList.Add(argument);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"无法启动：{fileName}");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new ProcessResult(process.ExitCode, (await stdout) + (await stderr));
    }

    private sealed record ConversionPlan(string OutputPath, string[] Arguments);

    private sealed record ProcessResult(int ExitCode, string Output);
}

internal static class StringExtensions
{
    public static IEnumerable<string> SplitLines(this string text)
    {
        return text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
    }
}
