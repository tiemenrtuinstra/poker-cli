#!/bin/bash
# Build Release Script for Poker CLI
# Creates self-contained executables for distribution

set -e

# Default values
RUNTIME="${1:-linux-x64}"
VERSION="${2:-1.2.7}"
OUTPUT_DIR="${3:-./release}"

echo -e "\033[36mBuilding Poker CLI v$VERSION for $RUNTIME...\033[0m"

# Validate runtime
VALID_RUNTIMES="win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64"
if [[ ! " $VALID_RUNTIMES " =~ " $RUNTIME " ]]; then
    echo -e "\033[31mInvalid runtime. Valid options: $VALID_RUNTIMES\033[0m"
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Build
echo -e "\033[33mRestoring dependencies...\033[0m"
dotnet restore TexasHoldem/TexasHoldem.csproj

echo -e "\033[33mPublishing self-contained executable...\033[0m"
dotnet publish TexasHoldem/TexasHoldem.csproj \
    --configuration Release \
    --runtime "$RUNTIME" \
    --output "$OUTPUT_DIR/$RUNTIME" \
    -p:Version="$VERSION" \
    -p:InformationalVersion="$VERSION"

# Determine output file name
if [[ "$RUNTIME" == win-* ]]; then
    EXT=".exe"
else
    EXT=""
fi

SOURCE_FILE="$OUTPUT_DIR/$RUNTIME/TexasHoldem$EXT"
DEST_FILE="$OUTPUT_DIR/poker-cli-$RUNTIME$EXT"

if [[ -f "$SOURCE_FILE" ]]; then
    mv "$SOURCE_FILE" "$DEST_FILE"
    chmod +x "$DEST_FILE"
    echo -e "\033[32mCreated: $DEST_FILE\033[0m"
else
    echo -e "\033[31mBuild output not found at: $SOURCE_FILE\033[0m"
    exit 1
fi

# Clean up intermediate files
rm -rf "$OUTPUT_DIR/$RUNTIME"

echo ""
echo -e "\033[32mBuild complete!\033[0m"
echo -e "\033[36mExecutable: $DEST_FILE\033[0m"
echo ""
echo -e "\033[33mTo build for other platforms:\033[0m"
echo "  ./build-release.sh linux-arm64"
echo "  ./build-release.sh osx-arm64"
echo "  ./build-release.sh win-x64"
