#!/bin/bash
set -e

# Poker CLI Installer for Linux and macOS
# Usage: curl -fsSL https://raw.githubusercontent.com/tiemenrtuinstra/poker-cli/main/install.sh | bash

REPO="tiemenrtuinstra/poker-cli"
INSTALL_DIR="${INSTALL_DIR:-$HOME/.local/bin}"
BINARY_NAME="poker-cli"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

info() { echo -e "${BLUE}[INFO]${NC} $1"; }
success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
error() { echo -e "${RED}[ERROR]${NC} $1"; exit 1; }

# Detect OS and architecture
detect_platform() {
    OS="$(uname -s)"
    ARCH="$(uname -m)"

    case "$OS" in
        Linux*)
            case "$ARCH" in
                x86_64) PLATFORM="linux-x64" ;;
                aarch64|arm64) PLATFORM="linux-arm64" ;;
                *) error "Unsupported architecture: $ARCH" ;;
            esac
            ;;
        Darwin*)
            case "$ARCH" in
                x86_64) PLATFORM="osx-x64" ;;
                arm64) PLATFORM="osx-arm64" ;;
                *) error "Unsupported architecture: $ARCH" ;;
            esac
            ;;
        *)
            error "Unsupported OS: $OS"
            ;;
    esac

    info "Detected platform: $PLATFORM"
}

# Get latest release version
get_latest_version() {
    info "Fetching latest version..."
    VERSION=$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest" | grep '"tag_name"' | sed -E 's/.*"([^"]+)".*/\1/')

    if [ -z "$VERSION" ]; then
        error "Could not determine latest version. Check https://github.com/$REPO/releases"
    fi

    info "Latest version: $VERSION"
}

# Download and install
install() {
    ARTIFACT="poker-cli-${PLATFORM}"
    DOWNLOAD_URL="https://github.com/$REPO/releases/download/$VERSION/$ARTIFACT"

    info "Downloading from: $DOWNLOAD_URL"

    # Create install directory if it doesn't exist
    mkdir -p "$INSTALL_DIR"

    # Download
    if command -v curl &> /dev/null; then
        curl -fsSL "$DOWNLOAD_URL" -o "$INSTALL_DIR/$BINARY_NAME"
    elif command -v wget &> /dev/null; then
        wget -q "$DOWNLOAD_URL" -O "$INSTALL_DIR/$BINARY_NAME"
    else
        error "Neither curl nor wget found. Please install one of them."
    fi

    # Make executable
    chmod +x "$INSTALL_DIR/$BINARY_NAME"

    success "Installed $BINARY_NAME to $INSTALL_DIR/$BINARY_NAME"
}

# Check if install dir is in PATH
check_path() {
    if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
        warn "$INSTALL_DIR is not in your PATH"
        echo ""
        echo "Add it to your shell profile:"
        echo ""
        echo "  For bash (~/.bashrc):"
        echo "    export PATH=\"\$HOME/.local/bin:\$PATH\""
        echo ""
        echo "  For zsh (~/.zshrc):"
        echo "    export PATH=\"\$HOME/.local/bin:\$PATH\""
        echo ""
        echo "Then restart your terminal or run: source ~/.bashrc (or ~/.zshrc)"
        echo ""
    fi
}

# Main
main() {
    echo ""
    echo "  ♠ ♥ ♦ ♣  Poker CLI Installer  ♣ ♦ ♥ ♠"
    echo ""

    detect_platform
    get_latest_version
    install
    check_path

    echo ""
    success "Installation complete!"
    echo ""
    echo "Run 'poker-cli' to start playing!"
    echo ""
}

main "$@"
