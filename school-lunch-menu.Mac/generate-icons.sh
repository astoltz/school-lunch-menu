#!/bin/bash
# Generate macOS app icons from SVG source
# Requires: Inkscape or rsvg-convert (from librsvg)

set -e

SVG_SOURCE="../school-lunch-menu.Windows/Assets/logo.svg"
OUTPUT_DIR="SchoolLunchMenu/Resources/Assets.xcassets/AppIcon.appiconset"

# Check if source exists
if [ ! -f "$SVG_SOURCE" ]; then
    echo "Error: SVG source not found at $SVG_SOURCE"
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

# Icon sizes needed for macOS (size@scale)
# 16x16@1x, 16x16@2x, 32x32@1x, 32x32@2x, 128x128@1x, 128x128@2x,
# 256x256@1x, 256x256@2x, 512x512@1x, 512x512@2x

declare -A SIZES=(
    ["icon_16x16.png"]=16
    ["icon_16x16@2x.png"]=32
    ["icon_32x32.png"]=32
    ["icon_32x32@2x.png"]=64
    ["icon_128x128.png"]=128
    ["icon_128x128@2x.png"]=256
    ["icon_256x256.png"]=256
    ["icon_256x256@2x.png"]=512
    ["icon_512x512.png"]=512
    ["icon_512x512@2x.png"]=1024
)

# Try to use rsvg-convert (preferred) or fall back to sips with a temp PNG
if command -v rsvg-convert &> /dev/null; then
    echo "Using rsvg-convert for SVG rendering..."
    for filename in "${!SIZES[@]}"; do
        size=${SIZES[$filename]}
        echo "Generating $filename (${size}x${size})..."
        rsvg-convert -w "$size" -h "$size" "$SVG_SOURCE" -o "$OUTPUT_DIR/$filename"
    done
elif command -v inkscape &> /dev/null; then
    echo "Using Inkscape for SVG rendering..."
    for filename in "${!SIZES[@]}"; do
        size=${SIZES[$filename]}
        echo "Generating $filename (${size}x${size})..."
        inkscape "$SVG_SOURCE" -w "$size" -h "$size" -o "$OUTPUT_DIR/$filename" 2>/dev/null
    done
else
    echo "Warning: Neither rsvg-convert nor Inkscape found."
    echo "Please install librsvg (brew install librsvg) or Inkscape."
    echo ""
    echo "Alternatively, you can manually convert the SVG to PNG at these sizes:"
    for filename in "${!SIZES[@]}"; do
        size=${SIZES[$filename]}
        echo "  - $filename: ${size}x${size} pixels"
    done
    exit 1
fi

# Update Contents.json with the generated filenames
cat > "$OUTPUT_DIR/Contents.json" << 'EOF'
{
  "images" : [
    {
      "filename" : "icon_16x16.png",
      "idiom" : "mac",
      "scale" : "1x",
      "size" : "16x16"
    },
    {
      "filename" : "icon_16x16@2x.png",
      "idiom" : "mac",
      "scale" : "2x",
      "size" : "16x16"
    },
    {
      "filename" : "icon_32x32.png",
      "idiom" : "mac",
      "scale" : "1x",
      "size" : "32x32"
    },
    {
      "filename" : "icon_32x32@2x.png",
      "idiom" : "mac",
      "scale" : "2x",
      "size" : "32x32"
    },
    {
      "filename" : "icon_128x128.png",
      "idiom" : "mac",
      "scale" : "1x",
      "size" : "128x128"
    },
    {
      "filename" : "icon_128x128@2x.png",
      "idiom" : "mac",
      "scale" : "2x",
      "size" : "128x128"
    },
    {
      "filename" : "icon_256x256.png",
      "idiom" : "mac",
      "scale" : "1x",
      "size" : "256x256"
    },
    {
      "filename" : "icon_256x256@2x.png",
      "idiom" : "mac",
      "scale" : "2x",
      "size" : "256x256"
    },
    {
      "filename" : "icon_512x512.png",
      "idiom" : "mac",
      "scale" : "1x",
      "size" : "512x512"
    },
    {
      "filename" : "icon_512x512@2x.png",
      "idiom" : "mac",
      "scale" : "2x",
      "size" : "512x512"
    }
  ],
  "info" : {
    "author" : "xcode",
    "version" : 1
  }
}
EOF

echo ""
echo "Icon generation complete!"
echo "Icons saved to: $OUTPUT_DIR"
