param([string]$Output = (Join-Path $PSScriptRoot '../src/Sukun.Windows/Assets/Brand'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, WindowsBase

# The filled WPF geometry is also used by the application header and SVG masters.
$geometryPath = Join-Path $PSScriptRoot '../src/Sukun.Windows/Resources/Brand.xaml'
[xml]$resource = Get-Content -LiteralPath $geometryPath -Raw
$master = [Windows.Media.Geometry]::Parse($resource.DocumentElement.FirstChild.InnerText)
New-Item -ItemType Directory -Force -Path $Output | Out-Null
function Brush([string]$Color) {
    $brush = [Windows.Media.SolidColorBrush]::new([Windows.Media.ColorConverter]::ConvertFromString($Color))
    $brush.Freeze()
    return $brush
}
function Write-Mark([int]$Size, [string]$Foreground, [string]$Background, [string]$Path) {
    $visual = [Windows.Media.DrawingVisual]::new()
    $drawing = $visual.RenderOpen()
    $drawing.PushTransform([Windows.Media.ScaleTransform]::new($Size/64.0,$Size/64.0))
    if ($Background) {
        $drawing.DrawRoundedRectangle((Brush $Background),$null,[Windows.Rect]::new(0,0,64,64),3,3)
    }
    $ink = Brush $Foreground
    if ($Size -le 32) {
        # Optical small-size variant: pixel-aligned bars, larger negative spaces.
        $drawing.PushTransform([Windows.Media.ScaleTransform]::new(4,4))
        if ($Background) { $xs=@(5,9); $width=2; $top=6; $height=6; $hat='M6,3.5 L8,1.8 10,3.5'; $weight=1.5 }
        else { $xs=@(4,9); $width=3; $top=7; $height=7; $hat='M6,4 L8,2 10,4'; $weight=1.75 }
        foreach ($x in $xs) { $drawing.DrawRoundedRectangle($ink,$null,[Windows.Rect]::new($x,$top,$width,$height),($width/2),($width/2)) }
        $pen = [Windows.Media.Pen]::new($ink,$weight)
        $pen.StartLineCap = 'Round'; $pen.EndLineCap = 'Round'; $pen.LineJoin = 'Round'
        $drawing.DrawGeometry($null,$pen,[Windows.Media.Geometry]::Parse($hat))
        $drawing.Pop()
    } else { $drawing.DrawGeometry($ink,$null,$master) }
    $drawing.Pop(); $drawing.Close()
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($Size,$Size,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.File]::Create($Path)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}
function Write-Ico([string]$Name,[string]$Foreground,[string]$Background) {
    $sizes = @(16,20,24,32,40,48,64,128,256)
    $frames = @()
    foreach ($size in $sizes) {
        $png = Join-Path $Output "$Name-$size.png"
        Write-Mark $size $Foreground $Background $png
        $frames += ,([IO.File]::ReadAllBytes($png))
    }
    $stream = [IO.MemoryStream]::new(); $writer = [IO.BinaryWriter]::new($stream)
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Length)
    $offset = 6 + 16*$sizes.Length
    for($i=0;$i -lt $sizes.Length;$i++) {
        $dimension = if($sizes[$i] -eq 256){0}else{$sizes[$i]}
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
        $offset += $frames[$i].Length
    }
    foreach($frame in $frames){$writer.Write([byte[]]$frame)}
    [IO.File]::WriteAllBytes((Join-Path $Output "$Name.ico"),$stream.ToArray())
    $writer.Dispose(); $stream.Dispose()
}
Write-Ico 'app' '#F6F5F1' '#5E8B7E'
Write-Ico 'sukun-tray-dark' '#F2F4F0' ''
Write-Ico 'sukun-tray-light' '#1E2421' ''
Write-Mark 512 '#F6F5F1' '#5E8B7E' (Join-Path $Output 'app-512.png')
Write-Mark 256 '#7FB09B' '#1D2421' (Join-Path $Output 'sukun-dark-256.png')
Write-Mark 256 '#5E8B7E' '#F6F5F1' (Join-Path $Output 'sukun-light-256.png')
Write-Host 'Generated pause + circumflex app/tray icons (including optically adjusted 16-32 px variants).'
