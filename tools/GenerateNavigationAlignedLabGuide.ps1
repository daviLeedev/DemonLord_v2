param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..'))
)

Add-Type -AssemblyName System.Drawing

$width = 1672
$height = 941
$orthoSize = 18.0
$aspect = $width / $height
$worldCenter = [double[]]@(0.0, 0.0, 2.5)
$mapOrigin = [double[]]@(-15.0, -14.0)
$mapSize = [double[]]@(30.0, 34.0)

$authoring = Join-Path $ProjectRoot 'Assets\_Project\Art\Maps\Authoring\WorldAdjustmentLab'
$mapFolder = Join-Path $ProjectRoot 'Assets\_Project\Art\Prototype\Maps'
New-Item -ItemType Directory -Path $authoring -Force | Out-Null
New-Item -ItemType Directory -Path $mapFolder -Force | Out-Null

$floors = @(
    @{ Name='Reception'; X=0.0; Z=0.0; W=12.0; D=10.0 },
    @{ Name='TaxOffice'; X=-10.0; Z=0.0; W=8.0; D=8.0 },
    @{ Name='Analysis'; X=10.0; Z=0.0; W=8.0; D=8.0 },
    @{ Name='Archive'; X=0.0; Z=9.0; W=10.0; D=8.0 },
    @{ Name='ArchiveAnnex'; X=0.0; Z=16.0; W=8.0; D=6.0 },
    @{ Name='Restricted'; X=0.0; Z=-9.0; W=10.0; D=8.0 }
)

$obstacles = @(
    @{ X=-6.0; Z=-3.0; W=0.35; D=4.0; H=3.0 }, @{ X=-6.0; Z=3.0; W=0.35; D=4.0; H=3.0 },
    @{ X=6.0; Z=-3.0; W=0.35; D=4.0; H=3.0 }, @{ X=6.0; Z=3.0; W=0.35; D=4.0; H=3.0 },
    @{ X=-3.5; Z=-5.0; W=5.0; D=0.35; H=3.0 }, @{ X=3.5; Z=-5.0; W=5.0; D=0.35; H=3.0 },
    @{ X=-3.5; Z=5.0; W=5.0; D=0.35; H=3.0 }, @{ X=3.5; Z=5.0; W=5.0; D=0.35; H=3.0 },
    @{ X=-14.0; Z=0.0; W=0.35; D=8.0; H=3.0 }, @{ X=-10.0; Z=4.0; W=8.0; D=0.35; H=3.0 }, @{ X=-10.0; Z=-4.0; W=8.0; D=0.35; H=3.0 },
    @{ X=14.0; Z=0.0; W=0.35; D=8.0; H=3.0 }, @{ X=10.0; Z=4.0; W=8.0; D=0.35; H=3.0 }, @{ X=10.0; Z=-4.0; W=8.0; D=0.35; H=3.0 },
    @{ X=-5.0; Z=9.0; W=0.35; D=8.0; H=3.0 }, @{ X=5.0; Z=9.0; W=0.35; D=8.0; H=3.0 },
    @{ X=-4.0; Z=16.0; W=0.35; D=6.0; H=3.0 }, @{ X=4.0; Z=16.0; W=0.35; D=6.0; H=3.0 },
    @{ X=-3.0; Z=13.0; W=4.0; D=0.35; H=3.0 }, @{ X=3.0; Z=13.0; W=4.0; D=0.35; H=3.0 }, @{ X=0.0; Z=19.0; W=8.0; D=0.35; H=3.0 },
    @{ X=-5.0; Z=-9.0; W=0.35; D=8.0; H=3.0 }, @{ X=5.0; Z=-9.0; W=0.35; D=8.0; H=3.0 }, @{ X=0.0; Z=-13.0; W=10.0; D=0.35; H=3.0 },
    @{ X=0.0; Z=2.8; W=4.3; D=0.85; H=1.3 }, @{ X=-10.4; Z=1.4; W=2.8; D=1.1; H=1.3 },
    @{ X=-12.8; Z=-1.6; W=0.55; D=3.8; H=2.5 }, @{ X=11.3; Z=1.6; W=2.4; D=1.2; H=1.6 },
    @{ X=9.1; Z=-1.3; W=1.2; D=1.2; H=1.8 }, @{ X=-3.5; Z=8.7; W=0.6; D=4.8; H=2.6 },
    @{ X=3.5; Z=8.7; W=0.6; D=4.8; H=2.6 }, @{ X=-2.5; Z=16.0; W=0.6; D=3.4; H=2.6 },
    @{ X=2.5; Z=16.0; W=0.6; D=3.4; H=2.6 }, @{ X=0.0; Z=-9.7; W=2.6; D=2.6; H=1.3 },
    @{ X=-9.7; Z=-0.8; W=1.2; D=0.9; H=1.0 }, @{ X=0.0; Z=10.25; W=1.1; D=1.1; H=1.1 },
    @{ X=0.0; Z=16.4; W=1.1; D=1.1; H=1.1 }
)

$doors = @(
    @{ X=-6.0; Z=0.0 }, @{ X=6.0; Z=0.0 }, @{ X=0.0; Z=5.0 }, @{ X=0.0; Z=13.0 }, @{ X=0.0; Z=-5.0 }
)
$spawn = @{ X=0.0; Z=-2.0 }

function Project-Point([double]$x, [double]$y, [double]$z) {
    $yaw = [Math]::PI / 4.0
    $pitch = 35.0 * [Math]::PI / 180.0
    $right = [double[]]@([Math]::Cos($yaw), 0.0, (-[Math]::Sin($yaw)))
    $up = [double[]]@(
        ([Math]::Sin($pitch) * [Math]::Sin($yaw)),
        [Math]::Cos($pitch),
        ([Math]::Sin($pitch) * [Math]::Cos($yaw))
    )
    $dx = $x - $worldCenter[0]
    $dy = $y - $worldCenter[1]
    $dz = $z - $worldCenter[2]
    $sx = $dx * $right[0] + $dy * $right[1] + $dz * $right[2]
    $sy = $dx * $up[0] + $dy * $up[1] + $dz * $up[2]
    return [System.Drawing.PointF]::new(
        [single](($sx / (2.0 * $orthoSize * $aspect) + 0.5) * $width),
        [single]((0.5 - $sy / (2.0 * $orthoSize)) * $height))
}

function Get-TopPolygon($box, [double]$y) {
    $x0 = $box.X - $box.W / 2.0; $x1 = $box.X + $box.W / 2.0
    $z0 = $box.Z - $box.D / 2.0; $z1 = $box.Z + $box.D / 2.0
    return [System.Drawing.PointF[]]@(
        (Project-Point $x0 $y $z0), (Project-Point $x1 $y $z0),
        (Project-Point $x1 $y $z1), (Project-Point $x0 $y $z1)
    )
}

function Draw-ProjectedBox($graphics, $box, $brush, $pen) {
    $top = Get-TopPolygon $box $box.H
    $bottom = Get-TopPolygon $box 0.0
    $graphics.FillPolygon($brush, $top)
    for ($i=0; $i -lt 4; $i++) {
        $j = ($i + 1) % 4
        $side = [System.Drawing.PointF[]]@($bottom[$i], $bottom[$j], $top[$j], $top[$i])
        $graphics.FillPolygon($brush, $side)
    }
    $graphics.DrawPolygon($pen, $top)
}

function Save-Projected([string]$path, [bool]$overlay) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    if ($overlay) { $graphics.Clear([System.Drawing.Color]::Transparent) }
    else { $graphics.Clear([System.Drawing.Color]::FromArgb(255, 7, 12, 20)) }

    $floorColor = if ($overlay) { [System.Drawing.Color]::FromArgb(42, 70, 213, 225) } else { [System.Drawing.Color]::FromArgb(255, 31, 184, 197) }
    $floorBrush = [System.Drawing.SolidBrush]::new($floorColor)
    $floorPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb($(if($overlay){90}else{255}), 122, 228, 239), $(if($overlay){3}else{5}))
    foreach ($floor in $floors) {
        $poly = Get-TopPolygon $floor 0.03
        $graphics.FillPolygon($floorBrush, $poly)
        $graphics.DrawPolygon($floorPen, $poly)
    }

    if (-not $overlay) {
        $obstacleBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 167, 34, 49))
        $obstaclePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 244, 103, 111), 2)
        foreach ($box in $obstacles) { Draw-ProjectedBox $graphics $box $obstacleBrush $obstaclePen }
        $obstacleBrush.Dispose(); $obstaclePen.Dispose()
    } else {
        $blockedBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(205, 24, 8, 14))
        $blockedPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(170, 116, 53, 61), 2)
        foreach ($box in $obstacles) {
            $expanded = @{
                X=$box.X; Z=$box.Z;
                W=($box.W + 0.7); D=($box.D + 0.7); H=0.08
            }
            $poly = Get-TopPolygon $expanded 0.09
            $graphics.FillPolygon($blockedBrush, $poly)
            $graphics.DrawPolygon($blockedPen, $poly)
        }
        $blockedBrush.Dispose(); $blockedPen.Dispose()
    }

    $doorBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb($(if($overlay){125}else{255}), 221, 166, 63))
    foreach ($door in $doors) {
        $box = @{ X=$door.X; Z=$door.Z; W=1.45; D=1.45; H=0.09 }
        $graphics.FillPolygon($doorBrush, (Get-TopPolygon $box 0.10))
    }
    $spawnBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb($(if($overlay){100}else{255}), 70, 129, 255))
    $spawnBox = @{ X=$spawn.X; Z=$spawn.Z; W=0.75; D=0.75; H=0.12 }
    $graphics.FillPolygon($spawnBrush, (Get-TopPolygon $spawnBox 0.13))

    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $spawnBrush.Dispose(); $doorBrush.Dispose(); $floorPen.Dispose(); $floorBrush.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}

function World-ToMapX([double]$x) { return [int](($x - $mapOrigin[0]) / $mapSize[0] * 1024.0) }
function World-ToMapY([double]$z) { return [int](1024.0 - (($z - $mapOrigin[1]) / $mapSize[1] * 1024.0)) }
function Draw-TopDownRect($graphics, $item, $brush, [int]$inset=0) {
    $left = World-ToMapX ($item.X - $item.W/2.0)
    $right = World-ToMapX ($item.X + $item.W/2.0)
    $top = World-ToMapY ($item.Z + $item.D/2.0)
    $bottom = World-ToMapY ($item.Z - $item.D/2.0)
    $graphics.FillRectangle($brush, $left+$inset, $top+$inset, [Math]::Max(1,$right-$left-2*$inset), [Math]::Max(1,$bottom-$top-2*$inset))
}

Save-Projected (Join-Path $authoring 'lab_projection_guide_v1.png') $false
Save-Projected (Join-Path $authoring 'lab_navigation_overlay_v1.png') $true

$mini = [System.Drawing.Bitmap]::new(1024, 1024, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$mg = [System.Drawing.Graphics]::FromImage($mini)
$mg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$mg.Clear([System.Drawing.Color]::FromArgb(255, 8, 14, 22))
$floorBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 43, 72, 88))
$obstacleBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 11, 19, 28))
$doorBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 190, 151, 74))
$spawnBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 102, 199, 232))
foreach ($floor in $floors) { Draw-TopDownRect $mg $floor $floorBrush 5 }
foreach ($box in $obstacles) {
    $expanded = @{
        X=$box.X; Z=$box.Z;
        W=($box.W + 0.7); D=($box.D + 0.7)
    }
    Draw-TopDownRect $mg $expanded $obstacleBrush 0
}
foreach ($door in $doors) { Draw-TopDownRect $mg @{X=$door.X;Z=$door.Z;W=1.4;D=1.4} $doorBrush 0 }
Draw-TopDownRect $mg @{X=$spawn.X;Z=$spawn.Z;W=0.6;D=0.6} $spawnBrush 0
$mini.Save((Join-Path $mapFolder 'lab_interior_map_v2.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$spawnBrush.Dispose(); $doorBrush.Dispose(); $obstacleBrush.Dispose(); $floorBrush.Dispose(); $mg.Dispose(); $mini.Dispose()

Write-Output "Generated navigation-aligned lab guide, overlay, and minimap."
