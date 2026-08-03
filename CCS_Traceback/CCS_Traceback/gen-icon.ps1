# 用 System.Drawing 生成 16/32/48/256 四尺寸的"C"字母图标(深蓝底白字),合并成多尺寸 .ico。
# 设计选择:PNG 嵌入(Vista+ ICO 格式),PNG 自带 alpha 通道,缩放无锯齿。
# 重新生成只需:  powershell -ExecutionPolicy Bypass -File gen-icon.ps1
[CmdletBinding()]
param(
    [string]$OutputPath = "app.ico",
    [int[]]$Sizes = @(16, 32, 48, 256),
    [string]$Text = "T"
)

Add-Type -AssemblyName System.Drawing

# 配色:深蓝底 + 白字
$bgColor = [System.Drawing.Color]::FromArgb(255, 30, 91, 168)
$fgColor = [System.Drawing.Color]::White

# 按尺寸配字号(粗略占 70% 边长)
function Get-FontSize([int]$s) {
    if ($s -le 16) { return 10 }
    if ($s -le 32) { return 22 }
    if ($s -le 48) { return 34 }
    return 180
}

# 给每尺寸渲染 PNG 字节流
$pngBlobs = @()
foreach ($size in $Sizes) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear($bgColor)

    $fontSize = Get-FontSize $size
    $font = New-Object System.Drawing.Font("Arial", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $brush = New-Object System.Drawing.SolidBrush($fgColor)

    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $rect = New-Object System.Drawing.RectangleF(0, 0, $size, $size)
    $g.DrawString($Text, $font, $brush, $rect, $sf)

    $g.Dispose()
    $font.Dispose()
    $brush.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $pngBlobs += ,@{ size = $size; bytes = $ms.ToArray() }
    $ms.Dispose()
}

# 写多尺寸 .ico 容器:ICONDIR + ICONDIRENTRY[] + PNG 数据
$fs = [System.IO.File]::Create($OutputPath)
$bw = New-Object System.IO.BinaryWriter($fs)

# ICONDIR (6 bytes)
$bw.Write([UInt16]0)                        # reserved
$bw.Write([UInt16]1)                        # type = 1 (ICO)
$bw.Write([UInt16]$pngBlobs.Count)          # image count

# ICONDIRENTRY (16 bytes each)
$headerSize = 6 + 16 * $pngBlobs.Count
$offset = $headerSize
foreach ($blob in $pngBlobs) {
    # 256 用 0 表示(字段是 byte,放不下 256)
    $w = if ($blob.size -ge 256) { 0 } else { $blob.size }
    $h = $w
    $bw.Write([byte]$w)                     # width (0 = 256)
    $bw.Write([byte]$h)                     # height
    $bw.Write([byte]0)                      # color count (0 = no palette)
    $bw.Write([byte]0)                      # reserved
    $bw.Write([UInt16]1)                    # color planes
    $bw.Write([UInt16]32)                   # bits per pixel
    $bw.Write([UInt32]$blob.bytes.Length)   # image data size
    $bw.Write([UInt32]$offset)              # offset from file start
    $offset += $blob.bytes.Length
}

# 各尺寸 PNG 数据
foreach ($blob in $pngBlobs) {
    $bw.Write($blob.bytes)
}

$bw.Dispose()
$fs.Dispose()

$total = (Get-Item $OutputPath).Length
Write-Host "OK: $OutputPath ($($pngBlobs.Count) sizes: $($Sizes -join ','), $total bytes)"